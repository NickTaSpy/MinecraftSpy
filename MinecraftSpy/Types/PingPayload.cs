using Newtonsoft.Json;

namespace MinecraftSpy;

public class PingPayload
{
    /// <summary>
    /// Protocol that the server is using and the given name
    /// </summary>
    [JsonProperty("version")]
    public VersionPayload? Version { get; set; }

    [JsonProperty("players")]
    public PlayersPayload? Players { get; set; }

    [JsonProperty("description")]
    public DescriptionPayload? Description { get; set; }

    /// <summary>
    /// Server icon, important to note that it's encoded in base 64
    /// </summary>
    [JsonProperty("favicon")]
    public string? Icon { get; set; }
}

public class VersionPayload
{
    [JsonProperty("protocol")]
    public int? Protocol { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }
}

public class PlayersPayload
{
    [JsonProperty("max")]
    public int? Max { get; set; }

    [JsonProperty("online")]
    public int? Online { get; set; }

    [JsonProperty("sample")]
    public List<Player>? Sample { get; set; }
}

public class Player
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }
}

public class DescriptionPayload
{
    [JsonProperty("text")]
    public string? Text { get; set; }
}