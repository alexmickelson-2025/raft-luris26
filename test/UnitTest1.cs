using classlibrary;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace test;

public class UnitTest1
{
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

    // 2. Verificar que un nodo recuerda al líder al recibir un AppendEntries.
    [Fact]
    public void NodeRemembersCurrentLeader()
    {
        var leaderNode = new ServerNode(true);
        var followerNode = new ServerNode(false);

        followerNode.requestRPC(leaderNode, "AppendEntries");

        Assert.Equal(leaderNode, followerNode.GetCurrentLeader());
    }

    // 3. Verificar que un nodo nuevo empieza como Follower.
    [Fact]
    public void NewNodeStartsAsFollower()
    {
        var newNode = new ServerNode(true);

        Assert.Equal(NodeState.Follower, newNode.State);
    }

    //ojo
    // 4. Verificar que un Follower inicia una elección tras 300ms sin mensajes.
    [Fact]
    public void FollowerStartsElectionAfterTimeout()
    {
        var follower = new ServerNode(true);

        Thread.Sleep(350);

        Assert.Equal(NodeState.Leader, follower.State);
    }

    // 5. Verificar que el tiempo de elección es aleatorio entre 150ms y 300ms.
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

    // 6. Verificar que el término aumenta al iniciar una nueva elección.
    [Fact]
    public void ElectionIncrementsTerm()
    {
        var node = new ServerNode(true);

        int initialTerm = node.Term;
        Thread.Sleep(350);
        int newTerm = node.Term;

        Assert.Equal(initialTerm + 1, newTerm);
    }

    // 7. Verificar que AppendEntries reinicia el temporizador de elección.
    [Fact]
    public void AppendEntriesResetsElectionTimer()
    {
        var leader = new ServerNode(true);
        var follower = new ServerNode(false);

        follower.requestRPC(leader, "AppendEntries");

        Assert.Equal(leader, follower.GetCurrentLeader());
    }

    // 8. Verificar que un candidato se convierte en líder con la mayoría de los votos.
    [Fact]
    public void CandidateBecomesLeaderWithMajorityVotes()
    {
        var neighbor1 = new ServerNode(true);
        var neighbor2 = new ServerNode(true);
        var neighbor3 = new ServerNode(false);

        var neighbors = new List<IServerNode> { neighbor1, neighbor2, neighbor3 };
        var candidate = new ServerNode(true, neighbors);

        Thread.Sleep(350);

        Assert.Equal(NodeState.Leader, candidate.State);
    }

    //9
    [Fact]
    public void CandidateBecomesLeaderWithMajorityVotesDespiteUnresponsiveNode()
    {
        // Arrange
        var neighbor1 = new ServerNode(true);
        var neighbor2 = new ServerNode(true);
        var unresponsiveNeighbor = new ServerNode(false);

        var neighbors = new List<IServerNode> { neighbor1, neighbor2, unresponsiveNeighbor };
        var candidate = new ServerNode(true, neighbors);

        // Act
        Thread.Sleep(350);

        // Assert
        Assert.Equal(NodeState.Leader, candidate.State);
    }

}
