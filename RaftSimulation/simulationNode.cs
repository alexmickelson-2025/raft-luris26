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

    public object CancellationTokenSource => ((IServerNode)InnerNode).CancellationTokenSource;

    public DateTime ElectionStartTime { get => ((IServerNode)InnerNode).ElectionStartTime; set => ((IServerNode)InnerNode).ElectionStartTime = value; }
    public TimeSpan ElectionTimeout { get => ((IServerNode)InnerNode).ElectionTimeout; set => ((IServerNode)InnerNode).ElectionTimeout = value; }
    IServerNode IServerNode.InnerNode
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public void AppendEntries(ServerNode leader, int term, List<LogEntry> entries)
    {
        ((IServerNode)InnerNode).AppendEntries(leader, term, entries);
    }

    public Task AppendEntries(IServerNode leader, int term, List<LogEntry> entries)
    {
        return ((IServerNode)InnerNode).AppendEntries(leader, term, entries);
    }

    public IServerNode GetCurrentLeader()
    {
        return ((IServerNode)InnerNode).GetCurrentLeader();
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

    Task IServerNode.requestRPC(IServerNode sender, string rpcType)
    {
        return ((IServerNode)InnerNode).requestRPC(sender, rpcType);
    }
}
