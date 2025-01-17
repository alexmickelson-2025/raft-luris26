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
        get => ((IServerNode)InnerNode)._currentLeader;
        set => ((IServerNode)InnerNode)._currentLeader = value;
    }
    public int Term
    {
        get => ((IServerNode)InnerNode).Term;
        set => ((IServerNode)InnerNode).Term = value;
    }
    public List<IServerNode> _neighbors { get => ((IServerNode)InnerNode)._neighbors; set => ((IServerNode)InnerNode)._neighbors = value; }
    IServerNode IServerNode._currentLeader { get => ((IServerNode)InnerNode)._currentLeader; set => ((IServerNode)InnerNode)._currentLeader = value; }

    public void Append(object state)
    {
        ((IServerNode)InnerNode).Append(state);
    }

    public void AppendEntries(ServerNode leader, int term, List<LogEntry> entries)
    {
        ((IServerNode)InnerNode).AppendEntries(leader, term, entries);
    }

    public void requestRPC(ServerNode sender, string rpcType)
    {
        ((IServerNode)InnerNode).requestRPC(sender, rpcType);
    }

    public void requestRPC(IServerNode sender, string rpcType)
    {
        ((IServerNode)InnerNode).requestRPC(sender, rpcType);
    }

    public bool RequestVote(ServerNode candidate, int term)
    {
        return ((IServerNode)InnerNode).RequestVote(candidate, term);
    }

    public void respondRPC()
    {
        ((IServerNode)InnerNode).respondRPC();
    }

    public void SetNeighbors(List<IServerNode> neighbors)
    {
        ((IServerNode)InnerNode).SetNeighbors(neighbors);
    }
}
