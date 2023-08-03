using Newtonsoft.Json;

namespace MinecraftSpy.Test;

public class ServerPingTests
{
    private readonly MinecraftServerPing _sut = new();

    [Fact]
    public async Task TestPing()
    {
        var data = JsonConvert.DeserializeObject<TestData>(File.ReadAllText("testData.json"));

        var response = await _sut.Ping(data.IP, data.Port);

        Assert.NotNull(response);
        Assert.NotNull(response.Description);
    }
}