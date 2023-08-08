using DSharpPlus.Entities;

namespace MinecraftSpy;

public static class Embeds
{
    private const string THUMBNAIL_URL = "https://cdn.icon-icons.com/icons2/2699/PNG/512/minecraft_logo_icon_168974.png";

    public static DiscordEmbed CreateDiscordEmbed(PingPayload ping, string address, short port)
    {
        var embedDescription = $"**{ping.Players?.Online}**/**{ping.Players?.Max}** players online";
        if (ping.Players?.Sample?.Count > 0)
        {
            embedDescription += $":\n{string.Join(", ", ping.Players.Sample.Select(x => x.Name))}";
        }

        return new DiscordEmbedBuilder()
            .WithAuthor($"{address}:{port}", iconUrl: THUMBNAIL_URL)
            .WithTitle(ping.Description?.Text ?? "")
            .WithDescription(embedDescription)
            .WithColor(DiscordColor.DarkGreen)
            .WithTimestamp(DateTime.UtcNow)
            .Build();
    }

    public static DiscordEmbed CreateErrorDiscordEmbed(string address, short port)
    {
        return new DiscordEmbedBuilder()
            .WithAuthor($"{address}:{port}", iconUrl: THUMBNAIL_URL)
            .WithTitle("The server did not respond!")
            .WithDescription("The server is either not accepting connections or there's something wrong with the bot.")
            .WithColor(DiscordColor.Red)
            .WithTimestamp(DateTime.UtcNow)
            .Build();
    }
}
