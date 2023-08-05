using DSharpPlus.Entities;

namespace MinecraftSpy;

public static class Embeds
{
    public static DiscordEmbed ToDiscordEmbed(this PingPayload ping, string address, short port)
    {
        var embedDescription = $"**{ping.Players?.Online}**/**{ping.Players?.Max}** players online";
        if (ping.Players?.Sample?.Count > 0)
        {
            embedDescription += $":\n{string.Join(',', ping.Players.Sample.Select(x => x.Name))}";
        }

        return new DiscordEmbedBuilder()
            .WithAuthor($"{address}:{port}")
            .WithTitle(ping.Description?.Text ?? "")
            .WithDescription(embedDescription)
            .WithColor(DiscordColor.DarkGreen)
            .WithTimestamp(DateTime.UtcNow)
            .WithThumbnail("https://www.minecraft.net/etc.clientlibs/minecraft/clientlibs/main/resources/favicon.ico")
            .Build();
    }
}
