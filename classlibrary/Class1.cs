using System.Data.Common;

namespace classlibrary;

public class ServerNode : IServerNode
{
    bool _vote { get; set; }
    List<IServerNode> _neighbors { get; set; }
    bool _isLeader { get; set; }
    private Timer _heartbeatTimer;
    private int _intervalHeartbeat;

    public ServerNode()
    {
        _neighbors = new List<IServerNode>();
        _vote = false;
        _isLeader = false;
    }

    public ServerNode(bool vote, List<IServerNode> neighbors = null, int heartbeatInterval = 50)
    {
        _vote = vote;
        _neighbors = neighbors ?? new List<IServerNode>();
        _isLeader = false;
        _intervalHeartbeat = heartbeatInterval;
    }

    public void ReceiveRPC()
    {
        throw new NotImplementedException();
    }

    public void SendRPC()
    {
        throw new NotImplementedException();
    }

    public bool ReturnVote()
    {
        return _vote;
    }

    public bool CountVotesInTheCluster()
    {
        int numberOfVotesToWin = _neighbors.Count() / 2;
        int trueVotes = 0;
        int falseVotes = 0;

        foreach (ServerNode neighbor in _neighbors)
        {
            if (trueVotes == numberOfVotesToWin || falseVotes == numberOfVotesToWin) break;

            if (neighbor.ReturnVote())
            {
                trueVotes++;
            }
            else
            {
                falseVotes++;
            }
        }

        return trueVotes >= falseVotes;
    }

    public void BecomeLeader()
    {
        _isLeader = true;
        _heartbeatTimer = new Timer(Append, null, 0, _intervalHeartbeat);
    }

    public void Append(object state)
    {
        if (_isLeader)
        {
            foreach (var neighbor in _neighbors)
            {
                neighbor.ReceiveRPC();
            }
        }
    }
}

public interface IServerNode
{
    void SendRPC();
    void Append(object state);

    void ReceiveRPC();
    //request
    //respond 

}