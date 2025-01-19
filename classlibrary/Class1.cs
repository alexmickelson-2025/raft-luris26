using System.Data.Common;
using System.Threading.Tasks;

namespace classlibrary;

public class ServerNode : IServerNode
{
    public string Id { get; set; }
    private bool _vote { get; set; }
    public string CurrentLiderID { get; set; }
    private bool _isLeader { get; set; }
    private bool _hasVoted = false;
    public string? votedFor;
    public int Term { get; set; }
    private int _intervalHeartbeat;
    public int _votesReceived;
    public bool SimulationRunning { get; set; }
    private Random _random;
    public List<IServerNode> _neighbors { get; set; }
    private Timer? _heartbeatTimer { get; set; }
    private Timer? _electionTimer { get; set; }
    public NodeState State { get; set; }
    public IServerNode _innerNode { get; set; }
    private int _timeoutMultiplier = 1;
    public IServerNode InnerNode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public object CancellationTokenSource => throw new NotImplementedException();

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
        _innerNode = null!;
        _random = new Random();

        StartElectionTimer();
    }

    public async Task requestRPC(IServerNode sender, string rpcType)
    {
        if (rpcType == "AppendEntries")
        {
            if (sender.Term >= Term)
            {
                Term = sender.Term;
            }

            if (sender.Term < Term)
            {
                return;
            }
            State = NodeState.Follower;
            _innerNode = sender;
            ResetElectionTimer();
            sender.respondRPC();
        }
    }

    public DateTime ElectionStartTime { get; set; }
    public TimeSpan ElectionTimeout { get; set; }

    public void StartElectionTimer()
    {
        ElectionStartTime = DateTime.Now;
        ElectionTimeout = TimeSpan.FromMilliseconds(GetRandomElectionTimeout());
        int timeout = GetRandomElectionTimeout();
        _electionTimer = new Timer(_ => StartElectionAsync(), null, timeout, Timeout.Infinite);
    }

    void ResetElectionTimer()
    {
        int timeout = GetRandomElectionTimeout();
        _electionTimer?.Change(timeout, Timeout.Infinite);
    }

    public async Task StartElectionAsync()
    {
        if (State == NodeState.Follower || State == NodeState.Candidate)
        {
            Term++;
            State = NodeState.Candidate;
            votedFor = Id;
            _votesReceived = 1;

            var voteTasks = _neighbors.Select(n => n.RequestVoteAsync(this, Term));
            var voteResults = await Task.WhenAll(voteTasks);

            _votesReceived += voteResults.Count(v => v);

            int majority = (_neighbors.Count / 2) + 1;
            if (_votesReceived >= majority)
            {
                await BecomeLeaderAsync();
            }
            else
            {
                StartElectionTimer();
            }
        }
    }

    public int GetRandomElectionTimeout()
    {
        return _random.Next(150, 301) * _timeoutMultiplier;
    }

    public void respondRPC()
    {
        Console.WriteLine("Received RPC");
    }

    public async Task BecomeLeaderAsync()
    {
        State = NodeState.Leader;
        _isLeader = true;

        // Send immediate heartbeat
        var heartbeatTasks = _neighbors.Select(neighbor =>
            neighbor.AppendEntries(this, Term, new List<LogEntry>())
        );

        await Task.WhenAll(heartbeatTasks);
    }

    public IServerNode GetCurrentLeader()
    {
        return _innerNode;
    }

    public void SetNeighbors(List<IServerNode> neighbors)
    {
        _neighbors.AddRange(neighbors);
    }

    public void StopSimulationLoop()
    {
        SimulationRunning = false;
    }

    public async Task<bool> RequestVoteAsync(IServerNode candidate, int term)
    {
        await Task.Delay(50);

        if (term > Term)
        {
            Term = term;
            votedFor = candidate.Id;
            _hasVoted = true;
            return true;
        }

        if (term == Term && string.IsNullOrEmpty(votedFor))
        {
            votedFor = candidate.Id;
            _hasVoted = true;
            return true;
        }

        return false;
    }

    public async Task AppendEntries(IServerNode leader, int term, List<LogEntry> entries)
    {
        await Task.Delay(10);
        if (term >= Term)
        {
            Term = term;
            _innerNode = leader;
            State = NodeState.Follower;
            ResetElectionTimer();
        }
    }

    void IServerNode.StartSimulationLoop()
    {
        SimulationRunning = true;
        StartElectionTimer();
    }

    public void SetTimeoutMultiplier(int multiplier)
    {
        _timeoutMultiplier = multiplier;
    }
}
