using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinecraftSpy;

await Host.CreateDefaultBuilder()
    .UseConsoleLifetime()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new Settings
        {
            DiscordToken = Environment.GetEnvironmentVariable(Constants.ENV_DISCORD_TOKEN)
                ?? throw new Exception("Missing configuration " + Constants.ENV_DISCORD_TOKEN),
            MinecraftIP = Environment.GetEnvironmentVariable(Constants.ENV_MINECRAFT_IP)
                ?? throw new Exception("Missing configuration " + Constants.ENV_MINECRAFT_IP),
            MinecraftPort = short.Parse(Environment.GetEnvironmentVariable(Constants.ENV_MINECRAFT_PORT)
                ?? throw new Exception("Missing configuration " + Constants.ENV_MINECRAFT_PORT)),
            DiscordChannelID = ulong.Parse(Environment.GetEnvironmentVariable(Constants.ENV_DISCORD_CHANNEL_ID)
                ?? throw new Exception("Missing configuration " + Constants.ENV_DISCORD_CHANNEL_ID)),
            DiscordMessageID = ulong.Parse(Environment.GetEnvironmentVariable(Constants.ENV_DISCORD_MESSAGE_ID)
                ?? throw new Exception("Missing configuration " + Constants.ENV_DISCORD_MESSAGE_ID)),
        });

        //services.AddHttpClient(Constants.HTTP_CLIENT_PTERODACTYL)
        //    .ConfigureHttpClient(c =>
        //    {
        //        c.BaseAddress = new Uri(Environment.GetEnvironmentVariable(Constants.ENV_PTERODACTYL_ADDRESS) ??
        //            throw new Exception(Constants.ENV_PTERODACTYL_ADDRESS + " is not available."));
        //        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
        //            Environment.GetEnvironmentVariable(Constants.ENV_PTERODACTYL_KEY) ??
        //                throw new Exception(Constants.ENV_PTERODACTYL_KEY + " is not available."));
        //    });

        //services.AddSingleton<PterodactylService>();
        services.AddHostedService<BotService>();
    })
    .RunConsoleAsync();