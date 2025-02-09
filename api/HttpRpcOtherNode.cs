using classlibrary;

public class HttpRpcOtherNode : IServerNode

{
    public int Id { get; }
    private HttpClient client = new();
    public string Url { get; }
    public List<string> Logs { get; private set; } = new();
    string IServerNode.Id { get; set; }
    public int Term { get; set; }
    public string CurrentLiderID { get; set; }
    public bool SimulationRunning { get; set; }
    public int LastApplied { get; set; }
    public DateTime ElectionStartTime { get; set; }
    public TimeSpan ElectionTimeout { get; set; }
    public List<IServerNode> _neighbors { get; set; }
    public NodeState State { get; set; }
    public IServerNode InnerNode { get; set; }
    public List<LogEntry> Log { get; set; }
    public int CommitIndex { get; set; }
    public Dictionary<string, int> NextIndex { get; set; }
    private Random _random;
    public Dictionary<string, string> StateMachine { get; set; }
    private Timer? _electionTimer { get; set; }
    private int _timeoutMultiplier = 1;
    public HttpRpcOtherNode()
    {
        _random = new Random();
    }
    public HttpRpcOtherNode(int id, string url)
    {
        Id = id;
        Url = url;
    }

    public async void ApplyCommand(ClientCommandData data)
    {
        await client.PostAsJsonAsync(Url + "/request/command", data);
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
    public async Task SendCommandAsync(string command)
    {
        try
        {
            Logs.Add($"Received command: {command}");
            await client.PostAsJsonAsync($"{Url}/request/command", command);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void ApplyLogEntry(LogEntry entry)
    {
        throw new NotImplementedException();
    }

    public void ApplyCommittedLogs()
    {
        Console.WriteLine($"Applying log entry");
    }

    public async Task ReceiveConfirmationFromFollower(string followerId, int logIndex)
    {
        if (!NextIndex.ContainsKey(followerId))
        {
            return;
        }

        if (logIndex >= NextIndex[followerId])
        {
            NextIndex[followerId] = logIndex + 1;
        }

        int majority = (_neighbors.Count + 1) / 2;
        int replicatedCount = _neighbors.Count(n => NextIndex[n.Id] > logIndex);

        if (replicatedCount >= majority)
        {
            CommitIndex = logIndex;
        }

        await Task.CompletedTask;
    }

    public Task<bool> ConfirmReplicationAsync(LogEntry logEntry, Action<string> clientCallback)
    {
        throw new NotImplementedException();
    }
    public async Task<NodeData?> GetNodeDataAsync(string nodeUrl)
    {
        try
        {
            return await client.GetFromJsonAsync<NodeData>($"{nodeUrl}/nodeData");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al obtener datos de {nodeUrl}: {ex.Message}");
            return null;
        }
    }

    public async Task requestRPC(IServerNode sender, string rpcType)
    {
        if (rpcType == "AppendEntries")
        {
            if (sender.Term >= Term)
            {
                Term = sender.Term;
            }

            if (sender.Term < Term)
            {
                return;
            }
            State = NodeState.Follower;
            ResetElectionTimer();
            var response = new VoteResponseData();
            sender.respondRPC(response);
        }
        throw new NotImplementedException();
    }
    void ResetElectionTimer()
    {
        int timeout = GetRandomElectionTimeout();
        _electionTimer?.Change(timeout, Timeout.Infinite);
    }
    public int GetRandomElectionTimeout()
    {
        return _random.Next(150, 301) * _timeoutMultiplier;
    }

    public Task<bool> ReceiveClientCommandAsync(LogEntry command)
    {
        throw new NotImplementedException();
    }

    public Task SendAppendEntriesAsync()
    {
        throw new NotImplementedException();
    }

    public void SetNeighbors(List<IServerNode> neighbors)
    {
        throw new NotImplementedException();
    }

    public Task UpdateNextIndexAsync(string followerId, int nextIndex)
    {
        throw new NotImplementedException();
    }

    public IServerNode GetCurrentLeader()
    {
        throw new NotImplementedException();
    }

    public void StartSimulationLoop()
    {
        throw new NotImplementedException();
    }

    public void StopSimulationLoop()
    {
        throw new NotImplementedException();
    }

    public async Task<(int Term, int LastLogIndex)> RespondToAppendEntriesAsync(RespondEntriesData response)
    {
        try
        {
            var httpResponse = await client.PostAsJsonAsync(Url + "/response/appendEntries", response);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return (-1, -1);
            }

            var result = await httpResponse.Content.ReadFromJsonAsync<(int Term, int LastLogIndex)>();
            return (-1, -1);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"Node {Url} is down.");
            return (-1, -1);
        }
    }

    public async Task<bool> AppendEntries(AppendEntriesData request)
    {
        try
        {
            var httpResponse = await client.PostAsJsonAsync(Url + "/request/appendEntries", request);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await httpResponse.Content.ReadFromJsonAsync<bool>();
            return result;
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"Node {Url} is down.");
            return false;
        }
    }

    public async Task<bool> RequestVoteAsync(VoteRequestData request)
    {
        try
        {
            var httpResponse = await client.PostAsJsonAsync(Url + "/request/vote", request);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await httpResponse.Content.ReadFromJsonAsync<bool>();
            return result;
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"Node {Url} is down.");
            return false;
        }
    }

}
