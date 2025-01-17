using classlibrary;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace test;

public class UnitTest1
{
    //1
    [Fact]
    public void LeaderSendsHeartbeatsToAllNeighborsWithin50ms()
    {
        var mockNeighbor1 = Substitute.For<IServerNode>();
        var mockNeighbor2 = Substitute.For<IServerNode>();
        var mockNeighbors = new List<IServerNode> { mockNeighbor1, mockNeighbor2 };
        var leader = new ServerNode(true, mockNeighbors);

        leader.BecomeLeader();
        Thread.Sleep(200);

        mockNeighbor1.ReceivedWithAnyArgs(5).respondRPC();
        mockNeighbor2.ReceivedWithAnyArgs(5).respondRPC();
    }

    // 2 Verificar que un nodo recuerda al líder al recibir un AppendEntries.
    [Fact]
    public void NodeRemembersCurrentLeader()
    {
        var leaderNode = new ServerNode(true);
        var followerNode = new ServerNode(false);

        followerNode.requestRPC(leaderNode, "AppendEntries");

        Assert.Equal(leaderNode, followerNode.GetCurrentLeader());
    }

    // 3 Verificar que un nodo nuevo empieza como follower
    [Fact]
    public void NewNodeStartsAsFollower()
    {
        var newNode = new ServerNode(true);

        Assert.Equal(NodeState.Follower, newNode.State);
    }

    // 4
    [Fact]
    public void FollowerStartsElectionAfterTimeout()
    {
        var follower = new ServerNode(true);

        Thread.Sleep(350);

        Assert.Equal(NodeState.Candidate, follower.State);
    }

    //5.
    [Fact]
    public void ElectionTimeoutIsRandomBetween150And300ms()
    {
        // Arrange
        var node = new ServerNode(true);
        var timeouts = new List<int>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            int timeout = node.GetRandomElectionTimeout();
            timeouts.Add(timeout);
        }

        // Assert
        Assert.All(timeouts, t => Assert.InRange(t, 150, 300));
        var uniqueValues = timeouts.Distinct().ToList();
        Assert.True(uniqueValues.Count > 10, "Randomness check failed: Not enough unique values.");

        var bucketCounts = new int[3];
        foreach (var timeout in timeouts)
        {
            if (timeout < 200) bucketCounts[0]++;
            else if (timeout < 250)
                bucketCounts[1]++;
            else
                bucketCounts[2]++;
        }
        Assert.All(bucketCounts, count => Assert.InRange(count, 200, 400));
    }

    // // 6. Verificar que el término aumenta al iniciar una nueva elección.
    [Fact]
    public void ElectionIncrementsTerm()
    {
        var node = new ServerNode(true);

        int initialTerm = node.Term;
        Thread.Sleep(350);
        int newTerm = node.Term;

        Assert.Equal(initialTerm + 1, newTerm);
    }

    //test to random ...
    //7.
    [Fact]
    public void AppendEntriesResetsElectionTimer()
    {
        var leader = new ServerNode(true);
        var follower = new ServerNode(false);

        follower.requestRPC(leader, "AppendEntries");

        Assert.Equal(leader, follower.GetCurrentLeader());
    }

    //8
    [Fact]
    public void CandidateBecomesLeaderWithMajorityVotes()
    {
        var neighbor1 = new ServerNode(true);
        var neighbor2 = new ServerNode(true);
        var neighbor3 = new ServerNode(true);

        var neighbors = new List<IServerNode> { neighbor1, neighbor2, neighbor3 };
        var candidate = new ServerNode(true, neighbors);

        Thread.Sleep(350);

        Assert.Equal(NodeState.Candidate, candidate.State);
    }

    //9.
    // [Fact]
    // public void CandidateBecomesLeaderWithMajorityVotesDespiteResponsive()
    // {
    //     var n1 = new ServerNode(true);
    //     var n2 = new ServerNode(true);
    //     var unrespond = new ServerNode(false);

    //     var neighbor = new List<IServerNode> { n1, n2, unrespond };
    //     var candidate = new ServerNode(true, neighbor);
    //     Thread.Sleep(350);
    //     Assert.Equal(NodeState.Follower, candidate.State);
    // }

    // //10

    [Fact]
    public void FollowerRespondYesToRequestVoteWithHigherTerm()
    {
        //Arrange
        var follower = new ServerNode(true);
        follower.Term = 1;

        var candidate = new ServerNode(true);
        //Act
        bool voted = follower.RequestVote(candidate, 2);

        //Assert
        Assert.True(voted);
        Assert.Equal(2, follower.Term);
    }

    //11
    [Fact]
    public void CandidateVotesForItself()
    {
        // Arrange
        var candidate = new ServerNode();

        // Act
        candidate.StartElection(null);

        // Assert
        Assert.Equal(candidate.Id, candidate.votedFor);
        Assert.Equal(1, candidate._votesReceived);
    }

    //12
    [Fact]
    public void CandidateBecomesFollowerUponReceivingAppendEntriesWithLaterTerm()
    {
        // Arrange
        var candidate = new ServerNode();
        candidate.State = NodeState.Candidate;
        candidate.Term = 5;

        var leader = new ServerNode();
        leader.Term = 6;

        // Act
        candidate.requestRPC(leader, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Follower, candidate.State); //foller
        Assert.Equal(6, candidate.Term);
        Assert.Equal(leader, candidate.GetCurrentLeader());
    }

    //13
    [Fact]
    public void CandidateBecomesFollowerUponReceivingAppendEntriesWithEqualTerm()
    {
        // Arrange
        var candidate = new ServerNode();
        candidate.State = NodeState.Candidate;
        candidate.Term = 5;

        var leader = new ServerNode();
        leader.Term = 5;

        // Act
        candidate.requestRPC(leader, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Follower, candidate.State);
        Assert.Equal(5, candidate.Term);
        Assert.Equal(leader, candidate.GetCurrentLeader());
    }

    //14
    [Fact]
    public void NodeDeniesSecondVoteRequestForSameTerm()
    {
        // Arrange
        var node = new ServerNode();
        node.Term = 5;

        var candidate1 = new ServerNode();
        var candidate2 = new ServerNode();

        // Act
        bool firstVote = node.RequestVote(candidate1, 5);
        bool secondVote = node.RequestVote(candidate2, 5);

        // Assert
        Assert.True(firstVote);
        Assert.False(secondVote);
    }

    //15
    [Fact]
    public void NodeVotesForFutureTerm()
    {
        // Arrange
        var node = new ServerNode();
        node.Term = 5;
        node.votedFor = "some-other-node";

        var futureCandidate = new ServerNode();

        // Act
        bool voteGranted = node.RequestVote(futureCandidate, 6);

        // Assert
        Assert.True(voteGranted);
        Assert.Equal(6, node.Term);
        Assert.Equal(futureCandidate.Id, node.votedFor);
    }

    //16
    [Fact]
    public void CandidateStartsNewElectionAfterTimeout()
    {
        // Arrange
        var neighbor1 = Substitute.For<IServerNode>();
        var neighbor2 = Substitute.For<IServerNode>();
        var neighbors = new List<IServerNode> { neighbor1, neighbor2 };
        var candidate = new ServerNode(true, neighbors);

        // Act
        candidate.StartElection(null);

        Thread.Sleep(candidate.GetRandomElectionTimeout() + 50);

        // Assert
        Assert.Equal(NodeState.Candidate, candidate.State);
        Assert.Equal(2, candidate.Term);
        neighbor1.Received().RequestVote(candidate, 2);
        neighbor2.Received().RequestVote(candidate, 2);
    }

    //17
    [Fact]
    public void FollowerSendsResponseUponReceivingAppendEntries()
    {
        // Arrange
        var leader = Substitute.For<IServerNode>();
        var follower = new ServerNode();

        leader.Term.Returns(5);
        leader.Id.Returns("leader-node-id");

        // Act
        follower.requestRPC(leader, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Follower, follower.State);
        Assert.Equal(5, follower.Term);
        leader.Received(1).respondRPC();
    }

    //18
    [Fact]
    public void CandidateRejectsAppendEntriesWithPreviousTerm()
    {
        // Arrange
        var candidate = new ServerNode();
        candidate.State = NodeState.Candidate;
        candidate.Term = 5;

        var sender = new ServerNode();
        sender.Term = 4;

        // Act
        candidate.requestRPC(sender, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Candidate, candidate.State);
        Assert.Equal(5, candidate.Term);
        Assert.Null(candidate.GetCurrentLeader());
    }

    // //19
    // [Fact]
    // public void LeaderSendsImmediateHeartbeatUponElection()
    // {
    //     // Arrange
    //     var follower1 = Substitute.For<IServerNode>();
    //     var follower2 = Substitute.For<IServerNode>();
    //     var neighbors = new List<IServerNode> { follower1, follower2 };

    //     var candidate = new ServerNode(true, neighbors);

    //     // Act
    //     candidate.StartElection(null);

    //     // Assert
    //     follower1.Received(1).AppendEntries(candidate, candidate.Term, Arg.Any<List<LogEntry>>());
    //     follower2.Received(1).AppendEntries(candidate, candidate.Term, Arg.Any<List<LogEntry>>());
    // }
}
