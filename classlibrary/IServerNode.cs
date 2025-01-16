namespace classlibrary;

public interface IServerNode
{
    public string Id { get; set; }
    public NodeState State { get; set; }
    public ServerNode _currentLeader { get; set; }
    public int Term { get; set; }
    void requestRPC(ServerNode sender, string rpcType); //sent
    void Append(object state);

    void respondRPC(); //receive
    bool RequestVote(ServerNode candidate, int term);
    
}
