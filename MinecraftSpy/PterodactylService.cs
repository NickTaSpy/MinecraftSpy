namespace MinecraftSpy;

public sealed class PterodactylService
{
    private readonly HttpClient _http;

    public PterodactylService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(Constants.HTTP_CLIENT_PTERODACTYL);
    }

    public async Task<string> GetServers()
    {
        return await _http.GetStringAsync("api/application/servers");
    }
}
