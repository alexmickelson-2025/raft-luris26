using System.Data.Common;

namespace classlibrary;

public class ServerNode : IServerNode
{
    bool _vote { get; set; }
    List<IServerNode> _neighbors { get; set; }
    bool _isLeader { get; set; }
    private Timer _heartbeatTimer;
    private int _intervalHeartbeat;
    public NodeState State { get; set; }
    public ServerNode _currentLeader { get; set; }

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
        State = NodeState.Follower;
    }

    public void requestRPC()
    {
        _currentLeader = this;
    }

    public void respondRPC()
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
                neighbor.respondRPC();
            }
        }
    }
    public ServerNode GetCurrentLeader()
    {
        return _currentLeader;
    }
}

public interface IServerNode
{
    void requestRPC(); //sent
    void Append(object state);

    void respondRPC(); //receive
}

public enum NodeState
{
    Follower,
    Candidate,
    Leader,
}
