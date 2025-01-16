using System.Data.Common;

namespace classlibrary;

public class ServerNode : IServerNode
{
    public string Id { get; set; }
    private bool _vote { get; set; }
    private List<IServerNode> _neighbors { get; set; }
    private bool _isLeader { get; set; }
    private Timer? _heartbeatTimer { get; set; }
    private Timer? _electionTimer { get; set; }
    private int _intervalHeartbeat;
    public NodeState State { get; set; }
    public ServerNode _currentLeader { get; set; }
    public int Term { get; set; }
    public int _votesReceived;
    private Random _random;
    private bool _hasVoted = false;
    public string? votedFor;

    public ServerNode()
    {
        _neighbors = new List<IServerNode>();
        _vote = false;
        _isLeader = false;
        _random = new Random();
        Id = Guid.NewGuid().ToString();
    }

    public ServerNode(bool vote, List<IServerNode> neighbors = null, int heartbeatInterval = 50, string id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        _vote = vote;
        _neighbors = neighbors ?? new List<IServerNode>();
        _isLeader = false;
        _intervalHeartbeat = heartbeatInterval;
        State = NodeState.Follower;
        Term = 0;
        _heartbeatTimer = null!;
        _electionTimer = null!;
        _currentLeader = null!;
        _random = new Random();

        StartElectionTimer();
    }

    public void requestRPC(ServerNode sender, string rpcType)
    {
        if (rpcType == "AppendEntries")
        {
            Term = sender.Term;
            State = NodeState.Follower;
            _currentLeader = sender;
            ResetElectionTimer();
            Console.WriteLine($"Node {Id} reset election timer on AppendEntries from {sender.Id}");
        }
    }


    void StartElectionTimer()
    {
        int timeout = GetRandomElectionTimeout();
        _electionTimer = new Timer(StartElection, null, timeout, Timeout.Infinite);
    }

    void ResetElectionTimer()
    {
        int timeout = GetRandomElectionTimeout();
        _electionTimer?.Change(timeout, Timeout.Infinite);
    }

    public void StartElection(object? state)
    {
        if (State == NodeState.Follower || State == NodeState.Candidate)
        {
            Term++;
            State = NodeState.Candidate;
            votedFor = Id;
            _votesReceived = 1;

            //testing
            foreach (var neighbor in _neighbors)
            {
                if (neighbor.RequestVote(this, Term))
                {
                    _votesReceived++;
                }
            }

            Thread.Sleep(500);
            int majority = (_neighbors.Count + 1) / 2;
            if (_votesReceived > majority)
            {
                BecomeLeader();
            }
        }
    }

    public bool RequestVote(ServerNode candidate, int term)
    {
        if (term > Term)
        {
            Term = term;
            votedFor = candidate.Id;
            _hasVoted = true;
            return true;
        }
        else if (term == Term && votedFor == null)
        {
            votedFor = candidate.Id;
            _hasVoted = true;
            return true;
        }
        return false;
    }

    public int GetRandomElectionTimeout()
    {
        return _random.Next(150, 301);
    }

    public void respondRPC()
    {
        Console.WriteLine("Received RPC");
    }

    public void BecomeLeader()
    {
        State = NodeState.Leader;
        _isLeader = true;
        Console.WriteLine($"Node {Id} became the Leader for term {Term}");
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
