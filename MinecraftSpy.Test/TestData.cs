using Newtonsoft.Json;

namespace MinecraftSpy.Test;

public class TestData
{
    [JsonProperty("IP")]
    public string IP { get; set; }

    [JsonProperty("Port")]
    public short Port { get; set; }
}
