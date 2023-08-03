namespace MinecraftSpy;

public class Settings
{
    public required string DiscordToken { get; set; }
    public required string MinecraftIP { get; set; }
    public required short MinecraftPort { get; set; }
    public required ulong DiscordChannelID { get; set; }
    public required ulong DiscordMessageID { get; set; }
}
