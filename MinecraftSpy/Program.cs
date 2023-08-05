using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinecraftSpy;
using Newtonsoft.Json;

await Host.CreateDefaultBuilder()
    .UseConsoleLifetime()
    .ConfigureServices((hostBuilder, services) =>
    {
        services.AddSingleton(hostBuilder.Configuration.GetSection("BotSettings").Get<Settings>() ?? new Settings());

        var secrets = JsonConvert.DeserializeObject<AppSecrets>(File.ReadAllText(Constants.FILE_SECRETS));

        if (secrets is null)
        {
            Console.WriteLine("Could not load " + Constants.FILE_SECRETS);
            Environment.Exit(0);
        }

        services.AddSingleton(secrets);

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

        if (string.IsNullOrEmpty(secrets.BotDb))
        {
            Console.WriteLine(secrets.BotDb + " hasn't been set.");
            Environment.Exit(0);
        }

        services.AddDbContextFactory<DatabaseContext>(options => options.UseMySql(secrets.BotDb, ServerVersion.AutoDetect(secrets.BotDb)));

        services.AddHostedService<BotService>();
    })
    .RunConsoleAsync();