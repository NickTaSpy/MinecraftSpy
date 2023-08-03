using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MinecraftSpy;

public sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly Settings _settings;
    private readonly DiscordClient _client;

    public BotService(ILogger<BotService> logger, IServiceProvider serviceProvider, Settings settings)
    {
        _logger = logger;
        _settings = settings;
        _client = new(new()
        {
            Token = _settings.DiscordToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });

        var slash = _client.UseSlashCommands(new SlashCommandsConfiguration { Services = serviceProvider });
        slash.RegisterCommands<SlashCommands>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync();
        _logger.LogInformation("Discord bot connected.");

        await ChannelUpdate(stoppingToken);

        _logger.LogInformation("Discord bot disconnecting.");
        await _client.DisconnectAsync();
    }

    private async Task ChannelUpdate(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        var pinger = new MinecraftServerPing();
        var channel = await _client.GetChannelAsync(_settings.DiscordChannelID);
        var message = await channel.GetMessageAsync(_settings.DiscordMessageID);

        while (await timer.WaitForNextTickAsync(token) && !token.IsCancellationRequested)
        {
            _logger.LogInformation("Pinging minecraft server.");
            var pingResult = await pinger.Ping(_settings.MinecraftIP, _settings.MinecraftPort, token);
            await message.ModifyAsync(JsonConvert.SerializeObject(pingResult));
        }
    }
}
