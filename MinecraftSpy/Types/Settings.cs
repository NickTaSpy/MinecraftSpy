namespace MinecraftSpy;

public class Settings
{
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(1);
}

public class AppSecrets
{
    public required string BotDb { get; set; }
    public required string DiscordToken { get; set; }
}