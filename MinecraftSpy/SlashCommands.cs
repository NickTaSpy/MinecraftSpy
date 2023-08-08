using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Channels;

namespace MinecraftSpy;

public class SlashCommands : ApplicationCommandModule
{
    private readonly ILogger<SlashCommands> _logger;
    private readonly DatabaseContext _dbContext;

    public SlashCommands(ILogger<SlashCommands> logger, DatabaseContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [SlashCommand("subscribe", "Create a message that will get updates with information about a server")]
    public async Task Subscribe(
        InteractionContext ctx,
        [Option("address", "Server's address")] string address,
        [Option("port", "Server's port")] long port)
    {
        await ctx.DeferAsync();

        if (!await ValidateNewSubscription(ctx, address, port))
            return;

        var channelId = ctx.Channel.Id;

        if (!await HandleNewSubscriptionExists(ctx, channelId, address, port))
            return;

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Adding subscription. This message will now automatically get updated with server information."));

        var message = await ctx.GetOriginalResponseAsync();

        await _dbContext.Subscriptions.AddAsync(new Subscription
        {
            ChannelID = channelId,
            MessageID = message.Id,
            ServerAddress = address,
            ServerPort = (short)port,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = ctx.User.Id
        });

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created subscription. Address: {address}, Port: {port}, Channel: {channel}", address, port, ctx.Channel.Name);
    }

    private async Task<bool> ValidateNewSubscription(InteractionContext ctx, string address, long port)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid port! The range is [{IPEndPoint.MinPort}, {IPEndPoint.MaxPort}]"));

            _logger.LogInformation("Declined subscription for invalid port. Address: {address}, Port: {port}, Channel: {channel}", address, port, ctx.Channel.Name);
            return false;
        }

        return true;
    }

    private async Task<bool> HandleNewSubscriptionExists(InteractionContext ctx, ulong channelId, string address, long port)
    {
        var sub = await _dbContext.Subscriptions
            .Where(x => x.ChannelID == channelId && x.ServerAddress == address && x.ServerPort == port)
            .FirstOrDefaultAsync();

        if (sub is not null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This channel already has a subscription for this server. Delete it before subscribing again."));

            _logger.LogInformation("Declined duplicate subscription. Address: {address}, Port: {port}, Channel: {channel}", address, port, ctx.Channel.Name);
            return false;
        }

        return true;
    }
}
