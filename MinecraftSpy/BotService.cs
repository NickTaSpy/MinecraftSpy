using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinecraftSpy;

public sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Settings _settings;
    private readonly DiscordClient _client;
    private readonly MinecraftServerPing _pinger = new();
    private readonly IServiceScope _serviceScope;
    private readonly DatabaseContext _dbContext;

    public BotService(ILogger<BotService> logger, IServiceProvider serviceProvider, Settings settings, AppSecrets secrets)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings;
        _client = new(new()
        {
            Token = secrets.DiscordToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });

        _serviceScope = _serviceProvider.CreateScope();
        _dbContext = _serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var slash = _client.UseSlashCommands(new SlashCommandsConfiguration { Services = serviceProvider });
        slash.RegisterCommands<SlashCommands>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync();
        _logger.LogInformation("Discord bot connected.");

        AddEvents();

        await BotLoop(stoppingToken);

        RemoveEvents();

        _logger.LogInformation("Discord bot disconnecting.");
        await _client.DisconnectAsync();

        _serviceScope?.Dispose();
    }

    private void AddEvents()
    {
        _client.MessageDeleted += OnMessageDeleted;
        _client.MessagesBulkDeleted += OnMessagesBulkDeleted;
    }

    private void RemoveEvents()
    {
        _client.MessageDeleted -= OnMessageDeleted;
        _client.MessagesBulkDeleted -= OnMessagesBulkDeleted;
    }

    private async Task BotLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_settings.UpdateInterval);

        while (await timer.WaitForNextTickAsync(ct) && !ct.IsCancellationRequested)
        {
            try
            {
                await UpdateSubscriptions(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating the subscriptions.");
            }
        }
    }

    private async Task UpdateSubscriptions(CancellationToken ct)
    {
        _logger.LogInformation("Starting pinging minecraft servers.");

        var subs = await _dbContext.Subscriptions.ToArrayAsync(ct);

        foreach (var sub in subs)
        {
            try
            {
                var channel = await _client.GetChannelAsync(sub.ChannelID);
                var message = await channel.GetMessageAsync(sub.MessageID);

                var pingResult = await _pinger.Ping(sub.ServerAddress, sub.ServerPort, ct);
                await message.ModifyAsync("", pingResult.ToDiscordEmbed(sub.ServerAddress, sub.ServerPort));
            }
            catch (NotFoundException)
            {
                _dbContext.Subscriptions.Remove(sub);
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Removed subscription because the discord message could not be found.");
            }
        }

        _logger.LogInformation("Finished pinging minecraft servers.");
    }

    private async Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs args)
    {
        var sub = await _dbContext.Subscriptions
            .Where(x => x.MessageID == args.Message.Id)
            .FirstOrDefaultAsync();

        if (sub is not null)
        {
            _dbContext.Subscriptions.Remove(sub);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Removed subscription because the discord message was deleted.");
        }
    }

    private async Task OnMessagesBulkDeleted(DiscordClient sender, MessageBulkDeleteEventArgs args)
    {
        var messageIds = args.Messages.Select(x => x.Id).ToArray();

        var subs = await _dbContext.Subscriptions
            .Where(x => messageIds.Contains(x.MessageID))
            .ToListAsync();

        if (subs.Any())
        {
            _dbContext.Subscriptions.RemoveRange(subs);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Removed subscriptions because the discord messages were deleted.");
        }
    }
}
