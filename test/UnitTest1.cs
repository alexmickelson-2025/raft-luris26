using System.Threading.Tasks;
using classlibrary;
using Microsoft.VisualBasic;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace test;

public class UnitTest1
{
    // 1
    [Fact]
    public async Task LeaderSendsHeartbeatsToAllNeighborsWithin50ms()
    {
        var mockNeighbor1 = Substitute.For<IServerNode>();
        var mockNeighbor2 = Substitute.For<IServerNode>();
        var mockNeighbors = new List<IServerNode> { mockNeighbor1, mockNeighbor2 };
        var leader = new ServerNode(true, mockNeighbors);

        await leader.BecomeLeaderAsync();
        Thread.Sleep(200);

        var response = new VoteResponseData();
        //
        mockNeighbor1.respondRPC(response);
        mockNeighbor2.respondRPC(response);//ReceivedWithAnyArgs
    }

    // 2
    [Fact]
    public async Task NodeRemembersCurrentLeader()
    {
        var leaderNode = Substitute.For<IServerNode>();
        var followerNode = new ServerNode(false);

        await followerNode.requestRPC(leaderNode, "AppendEntries");

        Assert.Equal(leaderNode, followerNode.GetCurrentLeader());
    }

    // // 3
    [Fact]
    public void NewNodeStartsAsFollower()
    {
        var newNode = new ServerNode(true);

        Assert.Equal(NodeState.Follower, newNode.State);
    }

    // // 4
    [Fact]
    public void FollowerStartsElectionAfterTimeout()
    {
        var follower = new ServerNode(true);

        Thread.Sleep(350);

        Assert.Equal(NodeState.Leader, follower.State);
    }

    // //5.
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

    // //6
    [Fact]
    public void ElectionIncrementsTerm()
    {
        var node = new ServerNode(true);

        int initialTerm = node.Term;
        Thread.Sleep(350);
        int newTerm = node.Term;

        Assert.Equal(initialTerm + 1, newTerm);
    }

    // // //test to random ...
    //7. dfghjkm
    [Fact]
    public async Task FollowerResetsElectionTimerOnAppendEntriesAsync()
    {
        // Arrange
        var follower = new ServerNode(true);
        var leader = Substitute.For<IServerNode>();
        leader.Term = 1;

        // Act
        await follower.requestRPC(leader, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Follower, follower.State);
        Assert.Equal(leader, follower.GetCurrentLeader());
    }

    // // //8
    [Fact]
    public async Task CandidateBecomesLeaderWithMajorityVotesAsync()
    {
        // Arrange
        var neighbor1 = Substitute.For<IServerNode>();
        var neighbor2 = Substitute.For<IServerNode>();
        var neighbor3 = Substitute.For<IServerNode>();

        neighbor1.RequestVoteAsync(Arg.Any<VoteRequestData>()).Returns(Task.FromResult(true));
        neighbor2.RequestVoteAsync(Arg.Any<VoteRequestData>()).Returns(Task.FromResult(true));
        neighbor3.RequestVoteAsync(Arg.Any<VoteRequestData>()).Returns(Task.FromResult(false));

        var neighbors = new List<IServerNode> { neighbor1, neighbor2, neighbor3 };
        var candidate = new ServerNode();
        candidate.State = NodeState.Follower;
        candidate.SetNeighbors(neighbors);

        // Act
        await candidate.StartElectionAsync();

        // Assert
        Assert.Equal(NodeState.Leader, candidate.State);
    }


    // // //9.
    [Fact]
    public async Task CandidateBecomesLeaderWithMajorityVotesDespiteUnresponsiveNode()
    {
        // Arrange
        var m1 = Substitute.For<IServerNode>();
        var m2 = Substitute.For<IServerNode>();
        var unresponsiveNode = Substitute.For<IServerNode>();

        m1.RequestVoteAsync(Arg.Any<VoteRequestData>()).Returns(Task.FromResult(true));
        m2.RequestVoteAsync(Arg.Any<VoteRequestData>()).Returns(Task.FromResult(true));
        unresponsiveNode.RequestVoteAsync(Arg.Any<VoteRequestData>()).Returns(Task.FromResult(false));

        var neighbors = new List<IServerNode> { m1, m2, unresponsiveNode };
        var candidate = new ServerNode();
        candidate.State = NodeState.Follower;
        candidate.SetNeighbors(neighbors);

        // Act
        await candidate.StartElectionAsync();

        // Assert
        Assert.Equal(NodeState.Leader, candidate.State);
    }

    //10
    [Fact]
    public async Task FollowerRespondYesToRequestVoteWithHigherTerm()
    {
        // Arrange
        var follower = new ServerNode();
        follower.Term = 1;

        var candidate = Substitute.For<IServerNode>();
        candidate.Id.Returns("candidate1");

        var voteRequest = new VoteRequestData
        {
            Candidate = candidate,
            term = 2
        };

        // Act
        bool voted = await follower.RequestVoteAsync(voteRequest);

        // Assert
        Assert.True(voted);
        Assert.Equal(2, follower.Term);
    }

    // // //11
    [Fact]
    public async Task CandidateVotesForItself()
    {
        // Arrange
        var candidate = new ServerNode();

        // Act
        await candidate.StartElectionAsync();

        // Assert
        Assert.Equal(candidate.Id, candidate.votedFor);
        Assert.Equal(1, candidate._votesReceived);
    }

    // // //12
    [Fact]
    public async Task CandidateBecomesFollowerUponReceivingAppendEntriesWithLaterTerm()
    {
        // Arrange
        var candidate = Substitute.For<IServerNode>();
        candidate.State = NodeState.Candidate;
        candidate.Term = 5;
        candidate.GetCurrentLeader().Returns((IServerNode)null);

        var leader = Substitute.For<IServerNode>();
        leader.Term = 6;
        leader.Id.Returns("leader1");

        candidate.When(c => c.requestRPC(leader, "AppendEntries"))
                 .Do(_ =>
                 {
                     candidate.Term = leader.Term;
                     candidate.State = NodeState.Follower;
                     candidate.GetCurrentLeader().Returns(leader);
                 });

        // Act
        await candidate.requestRPC(leader, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Follower, candidate.State);
        Assert.Equal(6, candidate.Term);
        Assert.Equal(leader, candidate.GetCurrentLeader());
    }


    // // //13
    [Fact]
    public async Task CandidateBecomesFollowerUponReceivingAppendEntriesWithEqualTerm()
    {
        // Arrange
        var candidate = new ServerNode();
        candidate.State = NodeState.Candidate;
        candidate.Term = 5;

        var leader = new ServerNode();
        leader.Term = 5;

        // Act
        await candidate.requestRPC(leader, "AppendEntries");

        // Assert
        Assert.Equal(NodeState.Follower, candidate.State);
        Assert.Equal(5, candidate.Term);
        Assert.Equal(leader, candidate.GetCurrentLeader());
    }

    //14
    [Fact]
    public async Task NodeDeniesSecondVoteRequestForSameTerm()
    {
        // Arrange
        var node = new ServerNode();
        node.Term = 5;

        var candidate1 = new ServerNode();
        var candidate2 = new ServerNode();

        var voteRequest1 = new VoteRequestData { Candidate = candidate1, term = 5 };
        var voteRequest2 = new VoteRequestData { Candidate = candidate2, term = 5 };

        // Act
        bool firstVote = await node.RequestVoteAsync(voteRequest1);
        bool secondVote = await node.RequestVoteAsync(voteRequest2);

        // Assert
        Assert.True(firstVote);
        Assert.False(secondVote);
    }

    //15
    [Fact]
    public async Task NodeVotesForFutureTerm()
    {
        // Arrange
        var node = new ServerNode();
        node.Term = 5;

        var futureCandidate = new ServerNode();
        var voteRequest = new VoteRequestData { Candidate = futureCandidate, term = 6 };

        // Act
        bool voteGranted = await node.RequestVoteAsync(voteRequest);

        // Assert
        Assert.True(voteGranted);
        Assert.Equal(6, node.Term);
        Assert.Equal(futureCandidate.Id, node.votedFor);
    }


    // // //16
    [Fact]
    public async Task CandidateStartsNewElectionAfterTimeout()
    {
        // Arrange
        var neighbor1 = Substitute.For<IServerNode>();
        var neighbor2 = Substitute.For<IServerNode>();
        var neighbors = new List<IServerNode> { neighbor1, neighbor2 };

        var candidate = new ServerNode();
        candidate.State = NodeState.Follower;
        candidate.SetNeighbors(neighbors);

        int initialTerm = candidate.Term;

        // Act
        await candidate.StartElectionAsync();

        await Task.Delay(candidate.GetRandomElectionTimeout() + 50);

        // Assert
        Assert.Equal(NodeState.Candidate, candidate.State);
        Assert.True(candidate.Term > initialTerm);

        var expectedRequest = Arg.Is<VoteRequestData>(data => data.term == candidate.Term);

        await neighbor1.RequestVoteAsync(expectedRequest);
        await neighbor2.RequestVoteAsync(expectedRequest);//receive
    }


    // // //17
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
        var response = new VoteResponseData();
        leader.Received(1).respondRPC(response);
    }

    // // //18
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

    // // // //19
    [Fact]
    public async Task LeaderSendsImmediateHeartbeatUponElection()
    {
        // Arrange
        var follower1 = Substitute.For<IServerNode>();
        var follower2 = Substitute.For<IServerNode>();
        var neighbors = new List<IServerNode> { follower1, follower2 };

        var candidate = new ServerNode();
        candidate.State = NodeState.Follower;
        candidate.SetNeighbors(neighbors);

        // Act
        await candidate.StartElectionAsync();
        if (candidate.State == NodeState.Leader)
        {
            await candidate.SendAppendEntriesAsync();
        }

        // Assert
        await follower1.AppendEntries(Arg.Any<AppendEntriesData>());//receive
        await follower2.AppendEntries(Arg.Any<AppendEntriesData>());
    }


    // //1.a cluster of five nodes where no leader
    [Fact]
    public async Task NodeTimesOutAndInitiatesElection()
    {
        // Arrange
        var node1 = new ServerNode();
        var node2 = Substitute.For<IServerNode>();
        var node3 = Substitute.For<IServerNode>();
        var node4 = Substitute.For<IServerNode>();
        var node5 = Substitute.For<IServerNode>();

        node1.SetNeighbors(new List<IServerNode> { node2, node3, node4, node5 });
        node2.SetNeighbors(new List<IServerNode> { node1, node3, node4, node5 });
        node3.SetNeighbors(new List<IServerNode> { node1, node2, node4, node5 });
        node4.SetNeighbors(new List<IServerNode> { node1, node2, node3, node5 });
        node5.SetNeighbors(new List<IServerNode> { node1, node2, node3, node4 });

        // Act
        await Task.Delay(350);
        await node1.StartElectionAsync();

        // Assert
        Assert.Equal(NodeState.Candidate, node1.State);
        Assert.Equal(1, node1.Term);
    }
}
