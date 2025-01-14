namespace classlibrary;

public interface IServerNode
{
    void requestRPC(); //sent
    void Append(object state);

    void respondRPC(); //receive
}
