using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinecraftSpy;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, retainedFileCountLimit: 15)
    .CreateLogger();

try
{
    Log.Information("Starting host");

    await Host.CreateDefaultBuilder()
        .UseConsoleLifetime()
        .ConfigureServices((hostBuilder, services) =>
        {
            services.AddSingleton(hostBuilder.Configuration.GetSection("BotSettings").Get<Settings>() ?? new Settings());

            var secrets = JsonConvert.DeserializeObject<AppSecrets>(File.ReadAllText(Constants.FILE_SECRETS))
                ?? throw new Exception("Could not load " + Constants.FILE_SECRETS);
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
                throw new Exception(secrets.BotDb + " hasn't been set.");
            }

            services.AddDbContextFactory<DatabaseContext>(options => options.UseMySql(secrets.BotDb, ServerVersion.AutoDetect(secrets.BotDb)));

            services.AddHostedService<BotService>();
        })
        .UseSerilog()
        .RunConsoleAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}