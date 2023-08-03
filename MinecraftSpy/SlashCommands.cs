using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MinecraftSpy;

public class SlashCommands : ApplicationCommandModule
{
    private readonly ILogger<SlashCommands> _logger;

    public SlashCommands(ILogger<SlashCommands> logger)
    {
        _logger = logger;
    }

    //[SlashCommand("test", "A slash command made to test the DSharpPlusSlashCommands library!")]
    //public async Task TestCommand(InteractionContext ctx)
    //{
    //    await ctx.CreateResponseAsync(JsonConvert.SerializeObject(res));
    //}
}
