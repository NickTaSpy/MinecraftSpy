using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinecraftSpy;

public class ConsoleCommandsService : BackgroundService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ConsoleCommandsService> _logger;

    public ConsoleCommandsService(IHostApplicationLifetime appLifetime, ILogger<ConsoleCommandsService> logger)
    {
        _appLifetime = appLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await Console.In.ReadLineAsync(stoppingToken);

            if (input?.Trim()?.Equals("stop", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Stop command was entered in console.");
                _appLifetime.StopApplication();
                return;
            }
        }
    }
}
