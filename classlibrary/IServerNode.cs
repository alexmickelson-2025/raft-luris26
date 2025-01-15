namespace classlibrary;

public interface IServerNode
{
    // public string? Id { get; set; }
    void requestRPC(ServerNode sender, string rpcType); //sent
    void Append(object state);

    void respondRPC(); //receive
    bool RequestVote(ServerNode candidate, int term);
}
