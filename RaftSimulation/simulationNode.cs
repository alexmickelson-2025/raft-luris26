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
    public ServerNode _currentLeader
    {
        get => ((IServerNode)InnerNode)._currentLeader;
        set => ((IServerNode)InnerNode)._currentLeader = value;
    }
    public int Term
    {
        get => ((IServerNode)InnerNode).Term;
        set => ((IServerNode)InnerNode).Term = value;
    }

    public void Append(object state)
    {
        ((IServerNode)InnerNode).Append(state);
    }

    public void requestRPC(ServerNode sender, string rpcType)
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
}
