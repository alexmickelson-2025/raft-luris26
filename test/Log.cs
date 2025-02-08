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

        var expectedAppendEntries = Arg.Is<AppendEntriesData>(data =>
        data.logEntries.Count == 1 &&
        data.logEntries[0].Command == "TestCommand" &&
        data.term == leader.Term
        );
        // Assert
        await follower1.AppendEntries(expectedAppendEntries);
        await follower2.AppendEntries(expectedAppendEntries);
    }

    // 2 when a leader receives a command from the client, it is appended to its log
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

    // //4. when a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one it its log
    [Fact]
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

        var appendRequest = new AppendEntriesData
        {
            leader = leader,
            term = leader.Term,
            logEntries = new List<LogEntry> { newLogEntry },
            leaderCommitIndex = leader.CommitIndex,
            prevLogIndex = 0,
            prevLogTerm = 0
        };

        await follower1.AppendEntries(appendRequest);
        await leader.UpdateNextIndexAsync(follower1.Id, leader.Log.Count);

        // Assert
        Assert.Equal(leader.Log.Count, leader.NextIndex[follower1.Id]);
    }

    // //6.
    [Fact]
    public async Task LeaderIncludesCommitIndexInAppendEntries()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");
        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };
        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Term = 1;
        leader.SetNeighbors(neighbors);

        await leader.BecomeLeaderAsync();

        leader.Log.Add(new LogEntry(index: 1, term: leader.Term, command: "Set x = 10"));
        leader.Log.Add(new LogEntry(index: 2, term: leader.Term, command: "Set x = 20"));
        leader.CommitIndex = 2;

        // Act
        await leader.SendAppendEntriesAsync();

        // Cálculo de prevLogIndex y prevLogTerm
        int prevLogIndex = leader.Log.Count > 1 ? leader.Log[^2].Index : 0;
        int prevLogTerm = leader.Log.Count > 1 ? leader.Log[^2].Term : 0;

        // Assert
        foreach (var follower in neighbors)
        {
            await follower.AppendEntries(Arg.Is<AppendEntriesData>(data =>
                data.leader == leader &&
                data.term == leader.Term &&
                data.leaderCommitIndex == leader.CommitIndex &&
                data.logEntries.Count == 2 && // Se asegura que se envían ambas entradas del log
                data.prevLogIndex == prevLogIndex &&
                data.prevLogTerm == prevLogTerm
            ));
        }
    }

    // //7 When a follower learns that a log entry is committed, it applies the entry to its local state machine
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

        var appendRequest = new AppendEntriesData
        {
            leader = leader,
            term = 1,
            logEntries = logs,
            leaderCommitIndex = leaderCommitIndex,
            prevLogIndex = 0,
            prevLogTerm = 0
        };

        // Act
        await follower.AppendEntries(appendRequest);

        // Assert
        Assert.Equal(2, follower.CommitIndex);
        Assert.Equal(2, follower.LastApplied);
    }

    // //8 when the leader has received a majority confirmation of a log, it commits it
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

    // //9. the leader commits logs by incrementing its committed log index
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

    // //10. given a follower receives an appendentries with log(s) it will add those entries to its personal log
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

        var appendRequest = new AppendEntriesData
        {
            leader = leader,
            term = 1,
            logEntries = newEntries,
            leaderCommitIndex = 0,
            prevLogIndex = 0,
            prevLogTerm = 0
        };

        // Act
        await follower.AppendEntries(appendRequest);

        // Assert
        Assert.Equal(2, follower.Log.Count);
        Assert.Equal("Set x = 10", follower.Log[0].Command);
        Assert.Equal("Set y = 20", follower.Log[1].Command);
    }


    // //11. a followers response to an appendentries includes the followers term number and log entry index
    // [Fact]
    // public async Task FollowerRespondsToAppendEntriesWithTermAndLastLogIndex()
    // {
    //     // Arrange
    //     var follower = new ServerNode();
    //     follower.Term = 2;
    //     follower.Log.Add(new LogEntry(index: 1, term: 1, command: "Set x = 10"));
    //     follower.Log.Add(new LogEntry(index: 2, term: 2, command: "Set y = 20"));

    //     // Act
    //     var response = await follower.RespondToAppendEntriesAsync();

    //     // Assert
    //     Assert.Equal(2, response.Term);
    //     Assert.Equal(2, response.LastLogIndex);
    // }

    // //12. when a leader receives a majority responses from the clients after a log replication heartbeat, the leader sends a confirmation response to the client
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
        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Term = 1;
        leader.SetNeighbors(neighbors);

        await leader.BecomeLeaderAsync();

        var logEntry = new LogEntry(index: 1, term: leader.Term, command: "TestCommand");
        leader.Log.Add(logEntry);

        // Simulación de respuestas de los seguidores
        follower1.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(true));
        follower2.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(true));
        follower3.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(false));

        string clientResponse = string.Empty;
        void ClientCallback(string response) => clientResponse = response;

        // Act
        bool isConfirmed = await leader.ConfirmReplicationAsync(logEntry, ClientCallback);

        // Assert
        Assert.True(isConfirmed);
        Assert.Equal($"Log entry {logEntry.Index} confirmed.", clientResponse);

        var expectedRequest = Arg.Is<AppendEntriesData>(data =>
            data.leader == leader &&
            data.term == leader.Term &&
            data.logEntries.Count == 1 &&
            data.logEntries[0].Command == "TestCommand"
        );
    }

    // //13 given a leader node, when a log is committed, it applies it to its internal state machine
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

        var appendRequest = new AppendEntriesData
        {
            leader = leader,
            term = 2,
            logEntries = leaderLogs,
            leaderCommitIndex = leaderCommitIndex,
            prevLogIndex = prevLogIndex,
            prevLogTerm = prevLogTerm
        };

        // Act
        bool success = await follower.AppendEntries(appendRequest);

        // Assert
        Assert.False(success);
        Assert.Equal(3, follower.CommitIndex);
    }

    // //in class
    // [Fact]
    // public async Task LeaderSendsAppendEntriesDuringElectionLoop()
    // {
    //     // Arrange
    //     var follower1 = Substitute.For<IServerNode>();
    //     follower1.Id.Returns("Follower1");

    //     var neighbors = new List<IServerNode> { follower1 };
    //     var leader = new ServerNode();
    //     leader.State = NodeState.Follower;
    //     leader.SetNeighbors(neighbors);

    //     await leader.BecomeLeaderAsync();

    //     // Act
    //     await Task.Delay(400);

    //     // Assert
    //     await follower1.Received(1).AppendEntries(Arg.Any<AppendEntriesData>());
    // }


    // //15 When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes the new entries
    [Fact]
    public async Task LeaderRetriesAppendEntriesWhenFollowerRejects()
    {
        // Arrange
        var follower = Substitute.For<IServerNode>();
        follower.Id.Returns("Follower1");

        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Term = 1;
        leader.SetNeighbors(new List<IServerNode> { follower });

        await leader.BecomeLeaderAsync();

        var logEntry1 = new LogEntry(index: 1, term: 1, command: "Command1");
        var logEntry2 = new LogEntry(index: 2, term: 1, command: "Command2");
        leader.Log.Add(logEntry1);
        leader.Log.Add(logEntry2);
        leader.NextIndex[follower.Id] = 3;

        follower.AppendEntries(Arg.Any<AppendEntriesData>())
            .Returns(call =>
            {
                var request = call.ArgAt<AppendEntriesData>(0);
                return request.prevLogIndex != 2;
            });

        // Act
        await leader.SendAppendEntriesAsync();

        // Assert
        var expectedRequest = Arg.Is<AppendEntriesData>(data =>
            data.leader == leader &&
            data.term == leader.Term &&
            data.leaderCommitIndex == leader.CommitIndex &&
            data.prevLogIndex == 1 &&
            data.prevLogTerm == 1
        );

        await follower.Received().AppendEntries(expectedRequest);

        Assert.Equal(2, leader.NextIndex[follower.Id]);
    }


    // //16. when a leader sends a heartbeat with a log, but does not receive responses from a majority of nodes, the entry is uncommitted
    [Fact]
    public async Task LeaderDoesNotCommitEntryWithoutMajorityAcknowledgment()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var follower3 = Substitute.For<IServerNode>();
        follower3.Id.Returns("Follower3");

        var neighbors = new List<IServerNode> { follower1, follower2, follower3 };
        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Term = 1;
        leader.SetNeighbors(neighbors);

        await leader.BecomeLeaderAsync();

        var newLogEntry = new LogEntry(index: 1, term: leader.Term, command: "Set x = 10");
        leader.Log.Add(newLogEntry);

        follower1.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(true));
        follower2.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(false));
        follower3.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(false));

        // Act
        await leader.SendAppendEntriesAsync();

        // Assert
        Assert.Equal(0, leader.CommitIndex);
    }


    // //17.if a leader does not response from a follower, the leader continues to send the log entries in subsequent heartbeats
    [Fact]
    public async Task LeaderContinuesSendingLogEntriesToUnresponsiveFollower()
    {
        // Arrange
        var follower = Substitute.For<IServerNode>();
        follower.Id.Returns("Follower1");

        var neighbors = new List<IServerNode> { follower };
        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Term = 1;
        leader.SetNeighbors(neighbors);

        await leader.BecomeLeaderAsync();

        var logEntry1 = new LogEntry(index: 1, term: leader.Term, command: "Command1");
        var logEntry2 = new LogEntry(index: 2, term: leader.Term, command: "Command2");
        leader.Log.Add(logEntry1);
        leader.Log.Add(logEntry2);
        leader.NextIndex[follower.Id] = 1;

        // Simular que el seguidor siempre rechaza AppendEntries
        follower.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(false));

        // Act
        await leader.SendAppendEntriesAsync();
        await leader.SendAppendEntriesAsync(); // Se espera que reenvíe

        // Calcular valores correctos para prevLogIndex y prevLogTerm
        int prevLogIndex = leader.NextIndex[follower.Id] - 1;
        int prevLogTerm = prevLogIndex > 0 ? leader.Log[prevLogIndex - 1].Term : 0;

        // Assert
        var expectedRequest = Arg.Is<AppendEntriesData>(data =>
            data.leader == leader &&
            data.term == leader.Term &&
            data.leaderCommitIndex == leader.CommitIndex &&
            data.prevLogIndex == prevLogIndex &&
            data.prevLogTerm == prevLogTerm &&
            data.logEntries.Count > 0 && // Verificar que no envíe logs vacíos
            data.logEntries[0].Command == "Command1" &&
            data.logEntries.Count == 2 &&
            data.logEntries[1].Command == "Command2"
        );

        await follower.AppendEntries(expectedRequest);
    }



    // //18. if a leader cannot commit an entry, it does not send a response to the client
    [Fact]
    public async Task LeaderDoesNotSendResponseToClientIfEntryIsNotCommitted()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        follower1.Id.Returns("Follower1");

        var follower2 = Substitute.For<IServerNode>();
        follower2.Id.Returns("Follower2");

        var neighbors = new List<IServerNode> { follower1, follower2 };
        var leader = new ServerNode();
        leader.State = NodeState.Leader;
        leader.Term = 1;
        leader.SetNeighbors(neighbors);

        await leader.BecomeLeaderAsync();

        var logEntry = new LogEntry(index: 1, term: leader.Term, command: "TestCommand");
        leader.Log.Add(logEntry);

        // Simulación de seguidores rechazando AppendEntries
        follower1.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(false));
        follower2.AppendEntries(Arg.Any<AppendEntriesData>()).Returns(Task.FromResult(false));

        string clientResponse = string.Empty;
        void ClientCallback(string response) => clientResponse = response;

        // Act
        bool isConfirmed = await leader.ConfirmReplicationAsync(logEntry, ClientCallback);

        // Assert
        Assert.False(isConfirmed);
        Assert.Equal(string.Empty, clientResponse);
    }


    // //19. if a node receives an appendentries with a logs that are too far in the future from your local state, you should reject the appendentries
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

        var appendRequest = new AppendEntriesData
        {
            leader = leader,
            term = 1,
            logEntries = leaderLogs,
            leaderCommitIndex = leaderCommitIndex,
            prevLogIndex = prevLogIndex,
            prevLogTerm = prevLogTerm
        };

        // Act
        bool success = await follower.AppendEntries(appendRequest);

        // Assert
        Assert.False(success);
        Assert.Equal(2, follower.Log.Count);
    }

    // //20. if a node receives and appendentries with a term and index that do not match, you will reject the appendentry until you find a matching log 
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

        var appendRequest = new AppendEntriesData
        {
            leader = leader,
            term = 1,
            logEntries = leaderLogs,
            leaderCommitIndex = leaderCommitIndex,
            prevLogIndex = prevLogIndex,
            prevLogTerm = prevLogTerm
        };

        // Act
        bool success = await follower.AppendEntries(appendRequest);

        // Assert
        Assert.False(success);
        Assert.Equal(3, follower.Log.Count);
    }

}
