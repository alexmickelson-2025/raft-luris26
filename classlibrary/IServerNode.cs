namespace classlibrary;

public interface IServerNode
{
    public string Id { get; set; }
    public int Term { get; set; }
    public string CurrentLiderID { get; set; } //id
    public bool SimulationRunning { get; set; }
    public DateTime ElectionStartTime { get; set; }
    public TimeSpan ElectionTimeout { get; set; }
    public List<IServerNode> _neighbors { get; set; }
    public NodeState State { get; set; }
    public IServerNode InnerNode { get; set; }
    object CancellationTokenSource { get; }

    public IEnumerable<IServerNode> GetNeighbors() => _neighbors;
    public List<LogEntry> Log { get; set; }
    public Dictionary<string, int> NextIndex { get; set; }
    Task requestRPC(IServerNode sender, string rpcType); //sent
    Task AppendEntries(IServerNode leader, int term, List<LogEntry> logEntries);
    Task ReceiveClientCommandAsync(LogEntry command);
    void respondRPC(); //receive
    Task<bool> RequestVoteAsync(IServerNode candidate, int term);
    void SetNeighbors(List<IServerNode> neighbors);
    public Task UpdateNextIndexAsync(string followerId, int nextIndex);
    public IServerNode GetCurrentLeader();

    // Simulation control
    void StartSimulationLoop();
    void StopSimulationLoop();
}
