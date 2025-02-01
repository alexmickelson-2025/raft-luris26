namespace classlibrary;

public interface IServerNode
{
    public string Id { get; set; }
    public int Term { get; set; }
    public string CurrentLiderID { get; set; } //id
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
    Dictionary<string, string> StateMachine { get; set; }
    void ApplyLogEntry(LogEntry entry);
    void ApplyCommittedLogs();
    Task ReceiveConfirmationFromFollower(string followerId, int logIndex);
    Task<bool> ConfirmReplicationAsync(LogEntry logEntry, Action<string> clientCallback);
    Task<(int Term, int LastLogIndex)> RespondToAppendEntriesAsync();
    Task requestRPC(IServerNode sender, string rpcType); //sent
    Task<bool> AppendEntries(AppendEntriesData request);
    Task<bool> ReceiveClientCommandAsync(LogEntry command);
    Task SendAppendEntriesAsync();
    void respondRPC(); //receive
    Task<bool> RequestVoteAsync(VoteRequestData request);
    void SetNeighbors(List<IServerNode> neighbors);
    public Task UpdateNextIndexAsync(string followerId, int nextIndex);
    public IServerNode GetCurrentLeader();
    // Simulation control
    void StartSimulationLoop();
    void StopSimulationLoop();
    void ApplyCommand(ClientCommandData data);
}
