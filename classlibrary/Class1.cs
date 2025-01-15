using System.Data.Common;

namespace classlibrary;

public class ServerNode : IServerNode
{
    bool _vote { get; set; }
    List<IServerNode> _neighbors { get; set; }
    bool _isLeader { get; set; }
    private Timer? _heartbeatTimer { get; set; }
    private Timer? _electionTimer { get; set; }
    private int _intervalHeartbeat;
    public NodeState State { get; set; }
    public ServerNode? _currentLeader { get; set; }
    public int Term { get; set; }

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
        Term = 0;
        _heartbeatTimer = null!;
        _electionTimer = null!;
        _currentLeader = null!;

        StartElectionTimer();
    }

    //sent
    public void requestRPC(ServerNode sender, string rpcType)
    {
        if (rpcType == "AppendEntries")
        {
            _currentLeader = sender;
            ResetElectionTimer();
        }
    }

    void StartElectionTimer()
    {
        _electionTimer = new Timer(StartElection, null, 300, Timeout.Infinite);
    }

    void ResetElectionTimer()
    {
        int timeout = GetRandomElectionTimeout();
        _electionTimer?.Change(timeout, Timeout.Infinite);
    }

    private void StartElection(object? state)
    {
        if (State == NodeState.Follower || State == NodeState.Candidate)
        {
            Term++;
            State = NodeState.Candidate;
        }
    }

    private int GetRandomElectionTimeout()
    {
        Random random = new Random();
        return random.Next(150, 301);
    }

    // receive
    public void respondRPC()
    {
        Console.WriteLine("doing something");
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
            if (trueVotes == numberOfVotesToWin || falseVotes == numberOfVotesToWin)
                break;

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
