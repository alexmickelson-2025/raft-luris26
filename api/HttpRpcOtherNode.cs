using classlibrary;

public class HttpRpcOtherNode
{
    public int Id { get; }
    private HttpClient client = new();
    public string Url { get; }
    private readonly ILogger<HttpRpcOtherNode> _logger;
    public HttpRpcOtherNode(int id, string url, ILogger<HttpRpcOtherNode> logger)
    {
        Id = id;
        Url = url;
        _logger = logger;
    }

    public async Task AppendEntries(AppendEntriesData request)
    {
        try
        {
            await client.PostAsJsonAsync(Url + "/request/appendEntries", request);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
            throw new NotImplementedException();
        }
    }

    public async void ApplyCommand(ClientCommandData data)
    {
        await client.PostAsJsonAsync(Url + "/request/command", data);
    }

    public async Task RequestVoteAsync(VoteRequestData request)
    {
        try
        {
            await client.PostAsJsonAsync(Url + "/request/vote", request);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
    }

    public async void respondRPC(VoteResponseData response)
    {
        try
        {
            await client.PostAsJsonAsync(Url + "/response/vote", response);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
    }

    public async Task RespondToAppendEntriesAsync(RespondEntriesData response)
    {
        try
        {
            await client.PostAsJsonAsync(Url + "/response/appendEntries", response);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
    }

}
