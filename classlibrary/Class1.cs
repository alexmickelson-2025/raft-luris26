﻿using System.Data.Common;
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
    private System.Timers.Timer? _heartbeatTimer { get; set; }
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
    private const int MaxRetries = 3;
    private Dictionary<string, int> RetryCounts = new();
    private readonly Dictionary<string, string> _stateMachine = new();
    public Dictionary<string, string> StateMachine => _stateMachine;

    Dictionary<string, string> IServerNode.StateMachine
    {
        get => _stateMachine;
        set
        {
            _stateMachine.Clear();
            foreach (var kvp in value)
            {
                _stateMachine[kvp.Key] = kvp.Value;
            }
        }
    }

    public ServerNode()
    {
        _neighbors = new List<IServerNode>();
        _vote = false;
        _isLeader = false;
        _random = new Random();
        Id = Guid.NewGuid().ToString();
        Log = new List<LogEntry>();
        _stateMachine["1"] = "value1 test luris default";
        _stateMachine["2"] = "value2 test";
    }

    public ServerNode(
        bool vote,
        List<IServerNode> neighbors = null,
        int heartbeatInterval = 50,
        string id = null
    )
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

    public void ApplyCommand(ClientCommandData data)
    {
        _stateMachine[data.key] = data.value;
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
        if (State == NodeState.Leader)
        {
            return;
        }
        if (State == NodeState.Follower || State == NodeState.Candidate)
        {
            Term++;
            State = NodeState.Candidate;
            votedFor = Id;
            _votesReceived = 1;

            VoteRequestData data = new VoteRequestData { Candidate = this, term = Term };
            var voteTasks = _neighbors.Select(n => n.RequestVoteAsync(data));
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
                ResetElectionTimer();
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
        Console.WriteLine("el principio del become leader");

        State = NodeState.Leader;
        _isLeader = true;

        NextIndex = new Dictionary<string, int>();
        int leaderLastLogIndex = Log.Count;

        Console.WriteLine("antes del foreach");

        foreach (var neighbor in _neighbors)
        {
            if (!string.IsNullOrEmpty(neighbor.Id))
            {
                NextIndex[neighbor.Id] = Log.Count + 1;
            }
        }
        var log = new List<LogEntry>();
        AppendEntriesData data = new AppendEntriesData
        {
            leader = this,
            term = Term,
            logEntries = log,
            leaderCommitIndex = 0,
            prevLogIndex = 0,
            prevLogTerm = 0,
        };
        var heartbeatTasks = _neighbors.Select(neighbor =>
            neighbor.AppendEntries(data) // esto es lo que envio
        );

        Console.WriteLine("envio un heartbeat");
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

    public async Task<bool> RequestVoteAsync(VoteRequestData data)
    {
        await Task.Delay(50);

        if (data.term > Term)
        {
            Term = data.term;
            votedFor = data.Candidate.Id;
            _hasVoted = true;
            return true;
        }

        if (data.term == Term && string.IsNullOrEmpty(votedFor))
        {
            votedFor = data.Candidate.Id;
            _hasVoted = true;
            return true;
        }

        return false;
    }

    //
    public async Task<bool> AppendEntries(AppendEntriesData data)
    {
        await Task.Delay(10);

        if (data.term < Term)
        {
            return false;
        }

        if (data.prevLogIndex > 0)
        {
            if (data.prevLogIndex > Log.Count)
            {
                return false;
            }

            if (Log[data.prevLogIndex - 1].Term != data.prevLogTerm)
            {
                return false;
            }
        }

        if (data.term >= Term)
        {
            Term = data.term;
            _innerNode = data.leader;
            State = NodeState.Follower;
            ResetElectionTimer();
        }

        ProcessLogEntries(data.logEntries);
        UpdateCommitIndexAndApplyEntries(data.leaderCommitIndex);
        return true;
    }

    private void ProcessLogEntries(List<LogEntry> logEntries)
    {
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
        }
    }

    private void UpdateCommitIndexAndApplyEntries(int leaderCommitIndex)
    {
        if (leaderCommitIndex > CommitIndex)
        {
            CommitIndex = Math.Min(leaderCommitIndex, Log.Count);

            for (int i = LastApplied + 1; i <= CommitIndex; i++)
            {
                ApplyLogEntry(Log[i - 1]);
                LastApplied = i;
            }
        }
        ApplyCommittedLogs();
    }

    public void ApplyLogEntry(LogEntry entry)
    {
        Console.WriteLine($"Applying log entry");
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

    public async Task<bool> ReceiveClientCommandAsync(LogEntry command)
    {
        if (State != NodeState.Leader)
        {
            return false;
        }

        Log.Add(command);

        var appendTasks = _neighbors.Select(async neighbor =>
        {
            try
            {
                AppendEntriesData data = new AppendEntriesData
                {
                    leader = this,
                    logEntries = new List<LogEntry> { command },
                    leaderCommitIndex = CommitIndex,
                    prevLogIndex = Log.Count - 1,
                    prevLogTerm = Term,
                };
                return await neighbor.AppendEntries(data);
            }
            catch
            {
                return false;
            }
        });

        var results = await Task.WhenAll(appendTasks);
        int successfulReplications = results.Count(success => success);

        int majority = (_neighbors.Count / 2) + 1;
        if (successfulReplications >= majority)
        {
            CommitIndex = command.Index;
            ApplyLogEntry(command);
            return true;
        }

        return false;
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
            if (!RetryCounts.ContainsKey(neighbor.Id))
            {
                RetryCounts[neighbor.Id] = 0;
            }

            int prevLogIndex = NextIndex[neighbor.Id] - 1;
            int prevLogTerm = prevLogIndex > 0 ? Log[prevLogIndex - 1].Term : 0;

            var entriesToSend = Log.Skip(NextIndex[neighbor.Id] - 1).ToList();

            AppendEntriesData request = new AppendEntriesData
            {
                leader = this,
                term = Term,
                leaderCommitIndex = CommitIndex,
                prevLogIndex = prevLogIndex,
                prevLogTerm = prevLogTerm,
            };
            bool success = await neighbor.AppendEntries(request);

            if (!success)
            {
                RetryCounts[neighbor.Id]++;

                if (RetryCounts[neighbor.Id] > MaxRetries)
                {
                    Console.WriteLine($"Max retries exceeded for {neighbor.Id}. Skipping.");
                    continue;
                }

                Console.WriteLine(
                    $"AppendEntries rejected by {neighbor.Id}. Decrementing NextIndex and retrying."
                );
                NextIndex[neighbor.Id] = Math.Max(1, NextIndex[neighbor.Id] - 1);

                await SendAppendEntriesAsync();
            }
            else
            {
                RetryCounts[neighbor.Id] = 0;
            }
        }
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

    public async Task<(int Term, int LastLogIndex)> RespondToAppendEntriesAsync()
    {
        await Task.Delay(10);
        int lastLogIndex = Log.Count > 0 ? Log[^1].Index : 0;
        return (Term, lastLogIndex);
    }

    public async Task<bool> ConfirmReplicationAsync(LogEntry logEntry, Action<string> clientCallback)
    {
        int acknowledgements = 1;
        var replicationTasks = _neighbors.Select(async neighbor =>
        {
            try
            {
                AppendEntriesData data = new AppendEntriesData
                {
                    leader = this,
                    term = Term,
                    logEntries = new List<LogEntry> { logEntry },
                    leaderCommitIndex = NextIndex[neighbor.Id] - 1,
                    prevLogIndex = NextIndex[neighbor.Id] - 1,
                    prevLogTerm = Log.ElementAtOrDefault(NextIndex[neighbor.Id] - 2)?.Term ?? 0,
                };
                return await neighbor.AppendEntries(data);
            }
            catch
            {
                return false;
            }
        });

        var results = await Task.WhenAll(replicationTasks);
        acknowledgements += results.Count(success => success);

        int majority = (_neighbors.Count / 2) + 1;

        if (acknowledgements >= majority)
        {
            CommitIndex = logEntry.Index;
            clientCallback?.Invoke($"Log entry {logEntry.Index} comfirt.");
            return true;
        }

        return false;
    }
    public void ApplyCommittedLogs()
    {
        while (CommitIndex > LastApplied)
        {
            var entryToApply = Log[LastApplied];
            ApplyLogEntry(entryToApply);
            LastApplied++;
        }
    }
}
