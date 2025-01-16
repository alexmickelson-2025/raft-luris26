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
    //test to be random...
    // // 5. Verificar que el tiempo de elección es aleatorio entre 150ms y 300ms.
    [Fact]
    public void ElectionTimeoutIsRandomBetween150And300ms()
    {
        var node = new ServerNode(true);
        var timeouts = new List<int>();

        for (int i = 0; i < 100; i++)
        {
            int timeout = node.GetRandomElectionTimeout();
            timeouts.Add(timeout);
        }

        Assert.All(timeouts, t => Assert.InRange(t, 150, 300));
        Assert.Contains(timeouts, t => t < 200);
        Assert.Contains(timeouts, t => t > 250);
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
    // // 7. Verificar que AppendEntries reinicia el temporizador de elección.
    [Fact]
    public void AppendEntriesResetsElectionTimer()
    {
        var leader = new ServerNode(true);
        var follower = new ServerNode(false);

        follower.requestRPC(leader, "AppendEntries");

        Assert.Equal(leader, follower.GetCurrentLeader());
    }

    // // // 8. Verificar que un candidato se convierte en líder con la mayoría de los votos.
    // [Fact]
    // public void CandidateBecomesLeaderWithMajorityVotes()
    // {
    //     var neighbor1 = new ServerNode(true);
    //     var neighbor2 = new ServerNode(true);
    //     var neighbor3 = new ServerNode(false);

    //     var neighbors = new List<IServerNode> { neighbor1, neighbor2, neighbor3 };
    //     var candidate = new ServerNode(true, neighbors);

    //     Thread.Sleep(350);

    //     Assert.Equal(NodeState.Leader, candidate.State);
    // }

    // //9. CAndidato se convierte en lider con la mayoria de votos.
    // [Fact]
    // public void CandidateBecomesLeaderWithMajorityVotesDespiteUnresponsiveNode()
    // {
    //     // Arrange
    //     var neighbor1 = new ServerNode(true);
    //     var neighbor2 = new ServerNode(true);
    //     var unresponsiveNeighbor = new ServerNode(false);

    //     var neighbors = new List<IServerNode> { neighbor1, neighbor2, unresponsiveNeighbor };
    //     var candidate = new ServerNode(true, neighbors);

    //     // Act
    //     Thread.Sleep(3500);

    //     // Assert
    //     Assert.Equal(NodeState.Leader, candidate.State);
    // }

    // //10 un follower que aun no ha votado y es un earlier term a un respond de si

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

    // //10 esta soy yo comprobando un segundo voto/ this is me double checking 2 votes
    [Fact]
    public void FollowerDoesNotVoteTwiceInSameTerm()
    {
        // Arrange
        var follower = new ServerNode(true);
        follower.Term = 1;

        var candidate1 = new ServerNode(true);
        var candidate2 = new ServerNode(true);

        // Act
        bool firstVote = follower.RequestVote(candidate1, 1);
        bool secondVote = follower.RequestVote(candidate2, 1);

        // Assert
        Assert.True(firstVote);
        Assert.False(secondVote);
    }

    // //11
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
}
