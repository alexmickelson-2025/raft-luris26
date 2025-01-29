using classlibrary;

public class SimulationNode : IServerNode
{
    public readonly ServerNode InnerNode;

    public SimulationNode(ServerNode node)
    {
        this.InnerNode = node;
    }

    public string Id
    {
        get => ((IServerNode)InnerNode).Id;
        set => ((IServerNode)InnerNode).Id = value;
    }
    public NodeState State
    {
        get => ((IServerNode)InnerNode).State;
        set => ((IServerNode)InnerNode).State = value;
    }
    public IServerNode _currentLeader
    {
        get => ((IServerNode)InnerNode).InnerNode;
        set => ((IServerNode)InnerNode).InnerNode = value;
    }
    public int Term
    {
        get => ((IServerNode)InnerNode).Term;
        set => ((IServerNode)InnerNode).Term = value;
    }
    public List<IServerNode> _neighbors
    {
        get => ((IServerNode)InnerNode)._neighbors;
        set => ((IServerNode)InnerNode)._neighbors = value;
    }
    public string CurrentLiderID
    {
        get => ((IServerNode)InnerNode).CurrentLiderID;
        set => ((IServerNode)InnerNode).CurrentLiderID = value;
    }
    public bool SimulationRunning
    {
        get => ((IServerNode)InnerNode).SimulationRunning;
        set => ((IServerNode)InnerNode).SimulationRunning = value;
    }

    public DateTime ElectionStartTime { get => ((IServerNode)InnerNode).ElectionStartTime; set => ((IServerNode)InnerNode).ElectionStartTime = value; }
    public TimeSpan ElectionTimeout { get => ((IServerNode)InnerNode).ElectionTimeout; set => ((IServerNode)InnerNode).ElectionTimeout = value; }
    public List<LogEntry> Log { get => ((IServerNode)InnerNode).Log; set => ((IServerNode)InnerNode).Log = value; }
    public int LastApplied { get => ((IServerNode)InnerNode).LastApplied; set => ((IServerNode)InnerNode).LastApplied = value; }
    public int CommitIndex { get => ((IServerNode)InnerNode).CommitIndex; set => ((IServerNode)InnerNode).CommitIndex = value; }
    public Dictionary<string, int> NextIndex { get => ((IServerNode)InnerNode).NextIndex; set => ((IServerNode)InnerNode).NextIndex = value; }
    public Dictionary<string, string> StateMachine { get => ((IServerNode)InnerNode).StateMachine; set => ((IServerNode)InnerNode).StateMachine = value; }
    IServerNode IServerNode.InnerNode
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public Task<bool> AppendEntries(IServerNode leader, int term, List<LogEntry> logEntries, int leaderCommitIndex, int prevLogIndex, int prevLogTerm)
    {
        return ((IServerNode)InnerNode).AppendEntries(leader, term, logEntries, leaderCommitIndex, prevLogIndex, prevLogTerm);
    }

    public void ApplyCommand(string key, string value)
    {
        ((IServerNode)InnerNode).ApplyCommand(key, value);
    }

    public void ApplyCommittedLogs()
    {
        ((IServerNode)InnerNode).ApplyCommittedLogs();
    }

    public void ApplyLogEntry(LogEntry entry)
    {
        ((IServerNode)InnerNode).ApplyLogEntry(entry);
    }

    public Task<bool> ConfirmReplicationAsync(LogEntry logEntry, Action<string> clientCallback)
    {
        return ((IServerNode)InnerNode).ConfirmReplicationAsync(logEntry, clientCallback);
    }

    public IServerNode GetCurrentLeader()
    {
        return ((IServerNode)InnerNode).GetCurrentLeader();
    }

    public Task ReceiveClientCommandAsync(List<LogEntry> logs, LogEntry command)
    {
        throw new NotImplementedException();
    }

    public Task ReceiveClientCommandAsync(LogEntry command)
    {
        return ((IServerNode)InnerNode).ReceiveClientCommandAsync(command);
    }

    public Task ReceiveConfirmationFromFollower(string followerId, int logIndex)
    {
        return ((IServerNode)InnerNode).ReceiveConfirmationFromFollower(followerId, logIndex);
    }

    public void requestRPC(ServerNode sender, string rpcType)
    {
        ((IServerNode)InnerNode).requestRPC(sender, rpcType);
    }

    public void requestRPC(IServerNode sender, string rpcType)
    {
        ((IServerNode)InnerNode).requestRPC(sender, rpcType);
    }

    public Task<bool> RequestVoteAsync(IServerNode candidate, int term)
    {
        return ((IServerNode)InnerNode).RequestVoteAsync(candidate, term);
    }

    public void respondRPC()
    {
        ((IServerNode)InnerNode).respondRPC();
    }

    public Task<(int Term, int LastLogIndex)> RespondToAppendEntriesAsync()
    {
        return ((IServerNode)InnerNode).RespondToAppendEntriesAsync();
    }

    public Task SendAppendEntriesAsync()
    {
        return ((IServerNode)InnerNode).SendAppendEntriesAsync();
    }

    public void SetNeighbors(List<IServerNode> neighbors)
    {
        ((IServerNode)InnerNode).SetNeighbors(neighbors);
    }

    public void StartSimulationLoop()
    {
        Console.WriteLine("starting election");
        InnerNode.SimulationRunning = true;
        ((IServerNode)InnerNode).StartSimulationLoop();
    }

    public void StopSimulationLoop()
    {
        Console.WriteLine("canceling election");
        InnerNode.SimulationRunning = false;
        ((IServerNode)InnerNode).StopSimulationLoop();
    }

    public Task UpdateNextIndexAsync(string followerId, int nextIndex)
    {
        return ((IServerNode)InnerNode).UpdateNextIndexAsync(followerId, nextIndex);
    }

    Task<bool> IServerNode.ReceiveClientCommandAsync(LogEntry command)
    {
        return ((IServerNode)InnerNode).ReceiveClientCommandAsync(command);
    }

    Task IServerNode.requestRPC(IServerNode sender, string rpcType)
    {
        return ((IServerNode)InnerNode).requestRPC(sender, rpcType);
    }
}
