using System.Threading.Tasks;
using classlibrary;
using Microsoft.VisualBasic;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace test;

public class Log
{
    // //HomeWork2 1 part

    // //1 when a leader receives a client command the leader sends the log entry in the next appendentries RPC to all node
    [Fact]
    public async Task LeaderSendsLogEntryInNextAppendEntriesRPC()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        var follower2 = Substitute.For<IServerNode>();
        var neighbors = new List<IServerNode> { follower1, follower2 };

        var leader = new ServerNode(true, neighbors);
        leader.State = NodeState.Leader;
        leader.Term = 1;

        var logEntry = new LogEntry(command: "TestCommand", term: 1, index: 1);

        // Act
        await leader.ReceiveClientCommandAsync(logEntry);

        // Assert
        await follower1.Received(1)
            .AppendEntries(
                leader,
                leader.Term,
                Arg.Is<List<LogEntry>>(entries =>
                    entries.Count == 1 && entries[0].Command == "TestCommand"),
                    0,
                    0,
                    0
            );
        await follower2.Received(1).
            AppendEntries(
                leader,
                leader.Term,
                Arg.Is<List<LogEntry>>(entries =>
                    entries.Count == 1 && entries[0].Command == "TestCommand"),
                    0,
                    0,
                    0
            );
    }

    //2 when a leader receives a command from the client, it is appended to its log
    [Fact]
    public async Task LeaderAppendsCommandToLog()
    {
        // Arrange
        var leader = new ServerNode();
        leader.State = NodeState.Leader;

        var logEntry = new LogEntry(index: 1, term: leader.Term, command: "Set x = 10");

        // Act
        await leader.ReceiveClientCommandAsync(logEntry);

        // Assert
        Assert.Contains(logEntry, leader.Log);
        Assert.Equal(1, leader.Log.Count);
        Assert.Equal("Set x = 10", leader.Log[0].Command);
    }

    //3. when a node is new, its log is empty
    [Fact]
    public void NewNodeLogIsEmpty()
    {
        // Arrange
        var newNode = new ServerNode();

        // Assert
        Assert.NotNull(newNode.Log);
        Assert.Empty(newNode.Log);
    }

    //4. when a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one it its log
    public async Task LeaderInitializesNextIndexForFollowers()
    {
        // Arrange
        var leader = new ServerNode();
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");
        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        leader.SetNeighbors(new List<IServerNode> { follower1, follower2 });
        follower1.SetNeighbors(new List<IServerNode> { leader, follower2 });
        follower2.SetNeighbors(new List<IServerNode> { leader, follower1 });

        // Act
        await leader.BecomeLeaderAsync();

        // Assert
        int expectedNextIndex = leader.Log.Count + 1;
        Assert.Equal(expectedNextIndex, leader.NextIndex[follower1.Id]);
        Assert.Equal(expectedNextIndex, leader.NextIndex[follower2.Id]);
    }

    //5.leaders maintain an "nextIndex" for each follower that is the index of the next log entry the leader will send to that follower
    [Fact]
    public async Task LeaderMaintainsNextIndexForEachFollower()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };

        var leader = new ServerNode(true, neighbors);

        await leader.BecomeLeaderAsync();

        // Act
        var newLogEntry = new LogEntry(index: 1, term: leader.Term, command: "Set x = 10");
        leader.Log.Add(newLogEntry);

        await follower1.AppendEntries(leader, leader.Term, new List<LogEntry> { newLogEntry }, leader.CommitIndex, 0, 0);
        await leader.UpdateNextIndexAsync(follower1.Id, leader.Log.Count);

        // Assert
        Assert.Equal(leader.Log.Count, leader.NextIndex[follower1.Id]);
    }

    //6.
    [Fact]
    public async Task LeaderIncludesCommitIndexInAppendEntries()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");
        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };
        var leader = new ServerNode(true, neighbors);

        await leader.BecomeLeaderAsync();

        leader.Log.Add(new LogEntry(index: 1, term: leader.Term, command: "Set x = 10"));
        leader.Log.Add(new LogEntry(index: 1, term: leader.Term, command: "Set x = 20"));
        leader.CommitIndex = 2;

        await leader.SendAppendEntriesAsync();

        foreach (var follower in neighbors)
        {
            await follower.Received().AppendEntries(
                leader,
                leader.Term,
                Arg.Any<List<LogEntry>>(),
                leader.CommitIndex,
                0,
                0
            );
        }
    }
    //7 When a follower learns that a log entry is committed, it applies the entry to its local state machine
    [Fact]
    public async Task FollowerAppliesCommittedEntriesUponAppendEntries()
    {
        // Arrange
        var leader = Substitute.For<IServerNode>();
        var follower = new ServerNode();
        var logs = new List<LogEntry>
        {
            new LogEntry (index:1, leader.Term, command:"Command1"),
            new LogEntry (index:1, leader.Term, command:"Command2")
        };

        follower.Log = logs;
        follower.CommitIndex = 0;
        follower.LastApplied = 0;

        var leaderCommitIndex = 2;

        // Act
        await follower.AppendEntries(leader, term: 1, follower.Log, leaderCommitIndex, 0, 0);

        // Assert
        Assert.Equal(2, follower.CommitIndex);
        Assert.Equal(2, follower.LastApplied);
    }

    //8 when the leader has received a majority confirmation of a log, it commits it
    [Fact]
    public async Task LeaderCommitsLogAfterMajorityConfirmation()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };

        var leader = new ServerNode(true, neighbors);
        await leader.BecomeLeaderAsync();

        var logEntry = new LogEntry(index: 1, term: leader.Term, command: "Set x = 10");
        leader.Log.Add(logEntry);

        await leader.ReceiveConfirmationFromFollower(follower1.Id, 1);
        await leader.ReceiveConfirmationFromFollower(follower2.Id, 1);

        // Act
        await leader.ReceiveConfirmationFromFollower(follower1.Id, 1);

        // Assert
        Assert.Equal(1, leader.CommitIndex);
        Assert.Contains(logEntry, leader.Log);
    }

    //9. the leader commits logs by incrementing its committed log index
    [Fact]
    public async Task LeaderCommitsLogsByIncrementingCommitIndex()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };
        var leader = new ServerNode(true, neighbors);

        await leader.BecomeLeaderAsync();

        var logEntry1 = new LogEntry(index: 1, term: leader.Term, command: "Command1");
        var logEntry2 = new LogEntry(index: 2, term: leader.Term, command: "Command2");
        leader.Log.Add(logEntry1);
        leader.Log.Add(logEntry2);

        await leader.SendAppendEntriesAsync();

        await leader.ReceiveConfirmationFromFollower(follower1.Id, logEntry1.Index);
        await leader.ReceiveConfirmationFromFollower(follower2.Id, logEntry1.Index);

        // Assert
        Assert.Equal(1, leader.CommitIndex);

        // Act
        await leader.ReceiveConfirmationFromFollower(follower1.Id, logEntry2.Index);
        await leader.ReceiveConfirmationFromFollower(follower2.Id, logEntry2.Index);

        // Assert
        Assert.Equal(2, leader.CommitIndex);
    }

    //10. given a follower receives an appendentries with log(s) it will add those entries to its personal log
    [Fact]
    public async Task FollowerAddsEntriesToPersonalLogUponAppendEntries()
    {
        // Arrange
        var leader = Substitute.For<IServerNode>();
        var follower = new ServerNode();
        follower.Term = 1;

        var newEntries = new List<LogEntry>
        {
            new LogEntry(index: 1, term: 1, command: "Set x = 10"),
            new LogEntry(index: 2, term: 1, command: "Set y = 20")
        };
        // Act
        await follower.AppendEntries(leader, term: 1, logEntries: newEntries, leaderCommitIndex: 0, 0, 0);

        // Assert
        Assert.Equal(2, follower.Log.Count);
        Assert.Equal("Set x = 10", follower.Log[0].Command);
        Assert.Equal("Set y = 20", follower.Log[1].Command);
    }

    //11. a followers response to an appendentries includes the followers term number and log entry index
    [Fact]
    public async Task FollowerRespondsToAppendEntriesWithTermAndLastLogIndex()
    {
        // Arrange
        var follower = new ServerNode();
        follower.Term = 2;
        follower.Log.Add(new LogEntry(index: 1, term: 1, command: "Set x = 10"));
        follower.Log.Add(new LogEntry(index: 2, term: 2, command: "Set y = 20"));

        // Act
        var response = await follower.RespondToAppendEntriesAsync();

        // Assert
        Assert.Equal(2, response.Term);
        Assert.Equal(2, response.LastLogIndex);
    }

    //12. when a leader receives a majority responses from the clients after a log replication heartbeat, the leader sends a confirmation response to the client
    [Fact]
    public async Task LeaderSendsConfirmationToClientAfterMajorityResponses()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var follower3 = Substitute.For<IServerNode>();
        follower3.Id.Returns("Follower3");

        var neighbors = new List<IServerNode> { follower1, follower2, follower3 };
        var leader = new ServerNode(true, neighbors);
        await leader.BecomeLeaderAsync();

        var logEntry = new LogEntry(index: 1, term: leader.Term, command: "TestCommand");
        leader.Log.Add(logEntry);

        follower1.AppendEntries(Arg.Any<IServerNode>(), Arg.Any<int>(), Arg.Any<List<LogEntry>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                 .Returns(Task.FromResult(true));

        follower2.AppendEntries(Arg.Any<IServerNode>(), Arg.Any<int>(), Arg.Any<List<LogEntry>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                 .Returns(Task.FromResult(true));

        follower3.AppendEntries(Arg.Any<IServerNode>(), Arg.Any<int>(), Arg.Any<List<LogEntry>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                 .Returns(Task.FromResult(false));

        string clientResponse = string.Empty;
        void ClientCallback(string response) => clientResponse = response;

        // Act
        bool isConfirmed = await leader.ConfirmReplicationAsync(logEntry, ClientCallback);

        // Assert
        Assert.True(isConfirmed);
        Assert.Equal($"Log entry {logEntry.Index} comfirt.", clientResponse);

        await follower1.Received(1)
            .AppendEntries(
                leader,
                leader.Term,
                Arg.Is<List<LogEntry>>(entries =>
                    entries.Count == 1 && entries[0].Command == "TestCommand"),
                0,
                0,
                0
            );

        await follower2.Received(1)
            .AppendEntries(
                leader,
                leader.Term,
                Arg.Is<List<LogEntry>>(entries =>
                    entries.Count == 1 && entries[0].Command == "TestCommand"),
                0,
                0,
                0
            );

        await follower3.Received(1)
            .AppendEntries(
                leader,
                leader.Term,
                Arg.Is<List<LogEntry>>(entries =>
                    entries.Count == 1 && entries[0].Command == "TestCommand"),
                0,
                0,
                0
            );
    }

    //13 given a leader node, when a log is committed, it applies it to its internal state machine
    [Fact]
    public void LeaderAppliesCommittedLogsToStateMachine()
    {
        // Arrange
        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Log.Add(new LogEntry(index: 1, term: 1, command: "Set x = 10"));
        leader.Log.Add(new LogEntry(index: 2, term: 1, command: "Set y = 20"));
        leader.CommitIndex = 2;
        leader.LastApplied = 0;

        // Act
        leader.ApplyCommittedLogs();

        // Assert
        Assert.Equal(2, leader.LastApplied);
    }

    // 14 when a follower receives a valid heartbeat
    [Fact]
    public async Task FollowerRejectsHeartbeatOnLogMismatch()
    {
        // Arrange
        var leader = Substitute.For<IServerNode>();
        leader.Term.Returns(2);
        var follower = new ServerNode();
        follower.Term = 1;

        follower.Log.Add(new LogEntry(index: 1, term: 1, command: "Set x = 10"));
        follower.Log.Add(new LogEntry(index: 2, term: 1, command: "Set y = 20"));

        var leaderLogs = new List<LogEntry>
    {
        new LogEntry(index: 3, term: 2, command: "Set z = 30"),
    };
        int leaderCommitIndex = 3;
        int prevLogIndex = 2;
        int prevLogTerm = 2;

        // Act
        bool success = await follower.AppendEntries(leader, term: 2, leaderLogs, leaderCommitIndex, prevLogIndex, prevLogTerm);

        // Assert
        Assert.False(success);
        Assert.Equal(0, follower.CommitIndex);
        Assert.DoesNotContain(leaderLogs, entry => follower.Log.Contains(entry));
    }
    //in class
    [Fact]
    public async Task LeaderSendsAppendEntriesDuringElectionLoop()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var neighbors = new List<IServerNode> { follower1 };
        var leader = new ServerNode(true, neighbors);
        leader.SetNeighbors(neighbors);

        await leader.BecomeLeaderAsync();

        // Act
        await Task.Delay(400);

        // Assert
        await follower1.Received(2).AppendEntries(leader, 0, Arg.Any<List<LogEntry>>(), 0, 0, 0);
    }

    //15 When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes the new entries
    [Fact]
    public async Task LeaderRetriesAppendEntriesWhenFollowerRejects()
    {
        // Arrange
        var follower = Substitute.For<IServerNode>();
        follower.Id.Returns("Follower1");

        var leader = new ServerNode(true, new List<IServerNode> { follower });
        await leader.BecomeLeaderAsync();

        var logEntry1 = new LogEntry(index: 1, term: 1, command: "Command1");
        var logEntry2 = new LogEntry(index: 2, term: 1, command: "Command2");
        leader.Log.Add(logEntry1);
        leader.Log.Add(logEntry2);
        leader.NextIndex[follower.Id] = 3;

        follower.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(call =>
        {
            int prevLogIndex = call.ArgAt<int>(4);
            return prevLogIndex != 2;
        });

        // Act
        await leader.SendAppendEntriesAsync();

        // Assert
        await follower.Received().AppendEntries(
            leader,
            leader.Term,
            Arg.Any<List<LogEntry>>(),
            leader.CommitIndex,
            1,
            1
        );

        Assert.Equal(2, leader.NextIndex[follower.Id]);
    }

    //16. when a leader sends a heartbeat with a log, but does not receive responses from a majority of nodes, the entry is uncommitted
    [Fact]
    public async Task LeaderDoesNotCommitEntryWithoutMajorityResponses()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var follower3 = Substitute.For<IServerNode>();
        follower3.Id.Returns("Follower3");

        var neighbors = new List<IServerNode> { follower1, follower2, follower3 };
        var leader = new ServerNode(true, neighbors);

        await leader.BecomeLeaderAsync();

        var newLogEntry = new LogEntry(index: 1, term: leader.Term, command: "Set x = 10");
        leader.Log.Add(newLogEntry);

        follower1.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(true);

        follower2.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(false);

        follower3.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(false);

        // Act
        await leader.SendAppendEntriesAsync();

        // Assert
        Assert.NotEqual(1, leader.CommitIndex);
        Assert.Equal(0, leader.CommitIndex);
    }

    //17.if a leader does not response from a follower, the leader continues to send the log entries in subsequent heartbeats
    [Fact]
    public async Task LeaderContinuesSendingLogEntriesToUnresponsiveFollower()
    {
        // Arrange
        var follower = Substitute.For<IServerNode>();
        follower.Id.Returns("Follower1");

        var neighbors = new List<IServerNode> { follower };
        var leader = new ServerNode(true, neighbors);
        await leader.BecomeLeaderAsync();

        var logEntry1 = new LogEntry(index: 1, term: leader.Term, command: "Command1");
        var logEntry2 = new LogEntry(index: 2, term: leader.Term, command: "Command2");
        leader.Log.Add(logEntry1);
        leader.Log.Add(logEntry2);
        leader.NextIndex[follower.Id] = 1;

        follower.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(false);

        // Act
        await leader.SendAppendEntriesAsync();
        await leader.SendAppendEntriesAsync();

        // Assert
        await follower.Received().AppendEntries(
            leader,
            leader.Term,
            Arg.Is<List<LogEntry>>(entries =>
                entries.Count == 2 && entries[0].Command == "Command1" && entries[1].Command == "Command2"),
            leader.CommitIndex,
            0,
            0
        );
    }

    //18. if a leader cannot commit an entry, it does not send a response to the client
    [Fact]
    public async Task LeaderDoesNotSendResponseToClientIfEntryIsNotCommitted()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };
        var leader = new ServerNode(true, neighbors);
        await leader.BecomeLeaderAsync();

        var logEntry = new LogEntry(index: 1, term: leader.Term, command: "TestCommand");
        leader.Log.Add(logEntry);

        follower1.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(false);

        follower2.AppendEntries(
            Arg.Any<IServerNode>(),
            Arg.Any<int>(),
            Arg.Any<List<LogEntry>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>()
        ).Returns(false);

        string clientResponse = string.Empty;
        void ClientCallback(string response) => clientResponse = response;

        // Act
        bool isConfirmed = await leader.ConfirmReplicationAsync(logEntry, ClientCallback);

        // Assert
        Assert.False(isConfirmed);
        Assert.Equal(string.Empty, clientResponse);
    }

    //19. if a node receives an appendentries with a logs that are too far in the future from your local state, you should reject the appendentries
    [Fact]
    public async Task FollowerRejectsAppendEntriesWithFutureLogs()
    {
        // Arrange
        var leader = Substitute.For<IServerNode>();
        leader.Term.Returns(1);

        var follower = new ServerNode();
        follower.Term = 1;

        follower.Log.Add(new LogEntry(index: 1, term: 1, command: "Set x = 10"));

        var leaderLogs = new List<LogEntry>
        {
            new LogEntry(index: 5, term: 1, command: "Set y = 20"),
        };
        int leaderCommitIndex = 5;
        int prevLogIndex = 4;
        int prevLogTerm = 1;

        // Act
        bool success = await follower.AppendEntries(leader, term: 1, leaderLogs, leaderCommitIndex, prevLogIndex, prevLogTerm);

        // Assert
        Assert.False(success);
        Assert.Equal(1, follower.Log.Count);
        Assert.DoesNotContain(leaderLogs, entry => follower.Log.Contains(entry));
    }
    //20. if a node receives and appendentries with a term and index that do not match, you will reject the appendentry until you find a matching log 
    [Fact]
    public async Task FollowerRejectsAppendEntriesUntilMatchingLogIsFound()
    {
        // Arrange
        var leader = Substitute.For<IServerNode>();
        leader.Term.Returns(1);

        var follower = new ServerNode();
        follower.Term = 1;

        follower.Log.Add(new LogEntry(index: 1, term: 1, command: "Set x = 10"));
        follower.Log.Add(new LogEntry(index: 2, term: 1, command: "Set y = 20"));

        var leaderLogs = new List<LogEntry>
        {
            new LogEntry(index: 3, term: 2, command: "Set z = 30"),
        };
        int leaderCommitIndex = 3;
        int prevLogIndex = 2;
        int prevLogTerm = 2;

        // Act
        bool success = await follower.AppendEntries(leader, term: 1, leaderLogs, leaderCommitIndex, prevLogIndex, prevLogTerm);

        // Assert
        Assert.False(success);
        Assert.Equal(2, follower.Log.Count);
        Assert.DoesNotContain(leaderLogs, entry => follower.Log.Contains(entry));
    }
}
