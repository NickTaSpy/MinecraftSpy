using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace MinecraftSpy;

public sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly Settings _settings;
    private readonly DiscordClient _client;
    private readonly IDbContextFactory<DatabaseContext> _dbFactory;

    public BotService(
        ILogger<BotService> logger,
        IServiceProvider serviceProvider,
        Settings settings, AppSecrets secrets,
        IDbContextFactory<DatabaseContext> dbFactory)
    {
        _logger = logger;
        _settings = settings;
        _client = new(new()
        {
            Token = secrets.DiscordToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });
        _dbFactory = dbFactory;

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
            catch (OperationCanceledException)
            {
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

        var subsPerConnection = (await GetSubscriptions(ct)).ToArray();

        _logger.LogInformation("Calculated {connCount} connections to ping.", subsPerConnection.Length);

        await Task.WhenAll(subsPerConnection.Select(async connSubs =>
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            try
            {
                PingPayload? pingResult = null;

                try
                {
                    pingResult = await new MinecraftServerPing().Ping(connSubs.Key.Addresses, connSubs.Key.Port, _settings.ServerPingTimeout, ct);
                }
                catch (SocketException ex)
                {
                    _logger.LogInformation("Could not ping server. | Reason: {reason} | Addresses: {addresses} | Port: {port}",
                        ex.SocketErrorCode, string.Join(", ", connSubs.Key.Addresses.Select(x => x.ToString())), connSubs.Key.Port);
                }
                catch (OperationCanceledException)
                {
                }

                foreach (var sub in connSubs)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        var channel = await _client.GetChannelAsync(sub.ChannelID);
                        var message = await channel.GetMessageAsync(sub.MessageID);

                        if (pingResult is null)
                        {
                            await message.ModifyAsync("", Embeds.CreateErrorDiscordEmbed(sub.ServerAddress, sub.ServerPort));
                            continue;
                        }

                        await message.ModifyAsync("", Embeds.CreateDiscordEmbed(pingResult, sub.ServerAddress, sub.ServerPort));
                    }
                    catch (NotFoundException)
                    {
                        using (var db = await _dbFactory.CreateDbContextAsync(ct))
                        {
                            db.Subscriptions.Remove(sub);
                            await db.SaveChangesAsync(ct);
                        }

                        _logger.LogInformation("Removed subscription because the discord message could not be found. | Subscription: {subscription}",
                            JsonConvert.SerializeObject(connSubs));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }));

        _logger.LogInformation("Finished pinging minecraft servers.");
    }

    private async Task<IEnumerable<IGrouping<ConnectionAddress, Subscription>>> GetSubscriptions(CancellationToken ct)
    {
        Subscription[] subs;

        using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            subs = await db.Subscriptions.AsNoTracking().ToArrayAsync(ct);
        }

        await Task.WhenAll(subs.Select(async sub => sub.ResolvedServerAddresses = await Dns.GetHostAddressesAsync(sub.ServerAddress, ct)));

        return subs.GroupBy(x => new ConnectionAddress
        {
            Addresses = x.ResolvedServerAddresses!,
            Port = x.ServerPort
        }, ConnectionAddressComparer.Instance);
    }

    private async Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs args)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        var sub = await db.Subscriptions
            .Where(x => x.MessageID == args.Message.Id)
            .FirstOrDefaultAsync();

        if (sub is not null)
        {
            db.Subscriptions.Remove(sub);
            await db.SaveChangesAsync();
            _logger.LogInformation("Removed subscription because the discord message was deleted. | Subscription: {subscription}",
                JsonConvert.SerializeObject(sub));
        }
    }

    private async Task OnMessagesBulkDeleted(DiscordClient sender, MessageBulkDeleteEventArgs args)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        var messageIds = args.Messages.Select(x => x.Id).ToArray();

        var subs = await db.Subscriptions
            .Where(x => messageIds.Contains(x.MessageID))
            .ToListAsync();

        if (subs.Any())
        {
            db.Subscriptions.RemoveRange(subs);
            await db.SaveChangesAsync();
            _logger.LogInformation("Removed subscriptions because the discord messages were deleted. | Subscriptions: {subscriptions}",
                JsonConvert.SerializeObject(subs));
        }
    }
}
