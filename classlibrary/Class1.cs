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
    public List<LogEntry> Log { get; set; }
    private int _timeoutMultiplier = 1;
    public int CommitIndex { get; set; } = 0;
    public Dictionary<string, int> NextIndex { get; set; } = new();
    public IServerNode InnerNode { get; set; }
    public DateTime ElectionStartTime { get; set; }
    public TimeSpan ElectionTimeout { get; set; }
    public List<LogEntry> entries { get; set; }
    public int LastApplied { get; set; }
    public ServerNode()
    {
        _neighbors = new List<IServerNode>();
        _vote = false;
        _isLeader = false;
        _random = new Random();
        Id = Guid.NewGuid().ToString();
        Log = new List<LogEntry>();
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
        Log = new List<LogEntry>();

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

        NextIndex = new Dictionary<string, int>();
        int leaderLastLogIndex = Log.Count;

        foreach (var neighbor in _neighbors)
        {
            if (!string.IsNullOrEmpty(neighbor.Id))
            {
                NextIndex[neighbor.Id] = Log.Count + 1;
            }
        }
        var heartbeatTasks = _neighbors.Select(neighbor =>
            neighbor.AppendEntries(this, Term, new List<LogEntry>(), 0) // esto es lo que envio
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

    public async Task AppendEntries(IServerNode leader, int term, List<LogEntry> logEntries, int leaderCommitIndex)
    {
        await Task.Delay(10);
        if (term >= Term)
        {
            Term = term;
            foreach (var entry in logEntries)
            {
                if (entry.Index > Log.Count)
                {
                    Log.Add(entry);
                }
                else if (Log[entry.Index - 1].Term != entry.Term)
                {
                    Log.RemoveRange(entry.Index - 1, Log.Count - (entry.Index - 1));
                    Log.Add(entry);
                }
                if (leaderCommitIndex > CommitIndex)
                {
                    CommitIndex = Math.Min(leaderCommitIndex, Log.Count);

                    for (int i = LastApplied + 1; i <= CommitIndex; i++)
                    {
                        ApplyLogEntry(Log[i - 1]);
                        LastApplied = i;
                    }
                }
            }
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

    public async Task ReceiveClientCommandAsync(LogEntry command)
    {
        if (State != NodeState.Leader)
        {
            throw new InvalidOperationException("Only the leader can process client commands.");
        }
        Log.Add(command);
        var appendTasks = _neighbors.Select(neighbor =>
            neighbor.AppendEntries(this, Term, new List<LogEntry> { command }, 0)
        );
        await Task.WhenAll(appendTasks);
    }

    public async Task UpdateNextIndexAsync(string followerId, int nextIndex)
    {
        if (!NextIndex.ContainsKey(followerId))
        {
            throw new InvalidOperationException($"Follower {followerId} not found.");
        }

        NextIndex[followerId] = nextIndex;
        await Task.CompletedTask;
    }

    public async Task SendAppendEntriesAsync()
    {
        foreach (var neighbor in _neighbors)
        {
            var entriesToSend = Log.Skip(NextIndex[neighbor.Id] - 1).ToList();
            await neighbor.AppendEntries(this, Term, entriesToSend, CommitIndex);
        }
    }

    public void ApplyLogEntry(LogEntry entry)
    {
        Console.WriteLine($"Applying log entry");
    }

    public async Task ReceiveConfirmationFromFollower(string followerId, int index)
    {
        if (!NextIndex.ContainsKey(followerId))
        {
            return;
        }

        if (index >= NextIndex[followerId])
        {
            NextIndex[followerId] = index + 1;
        }

        int majority = (_neighbors.Count + 1) / 2;
        int replicatedCount = _neighbors.Count(n => NextIndex[n.Id] > index);

        if (replicatedCount >= majority)
        {
            CommitIndex = index;
        }

        await Task.CompletedTask;
    }
}
