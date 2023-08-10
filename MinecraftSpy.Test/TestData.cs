using Newtonsoft.Json;

namespace MinecraftSpy.Test;

public class TestData
{
    [JsonProperty("Address")]
    public string Address { get; set; }

    [JsonProperty("Port")]
    public short Port { get; set; }
}
