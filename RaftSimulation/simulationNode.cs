using classlibrary;

public class SimulationNode : IServerNode
{
    public readonly ServerNode InnerNode;
    public SimulationNode(ServerNode node)
    {
        this.InnerNode = node;
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
