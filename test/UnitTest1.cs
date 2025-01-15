using classlibrary;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace test;

public class UnitTest1
{
    // 1
    [Fact]
    public void LeaderSendsHeartbeatsToAllNeighborsWithin50ms()
    {
        // Arrange
        var mockNeighbor1 = Substitute.For<IServerNode>();
        var mockNeighbor2 = Substitute.For<IServerNode>();
        var mockNeighbors = new List<IServerNode> { mockNeighbor1, mockNeighbor2 };
        var leader = new ServerNode(true, mockNeighbors);

        // Act
        leader.BecomeLeader();
        for (int i = 0; i < 3; i++)
        {
            leader.Append(null);
        }

        // Assert
        mockNeighbor1.ReceivedWithAnyArgs(4).respondRPC();
        mockNeighbor2.ReceivedWithAnyArgs(4).respondRPC();
    }

    //2
    [Fact]
    public void NodeRemembersCurrentLeader()
    {
        // Arrange
        var leaderNode = new ServerNode(true);
        var followerNode = new ServerNode(false);

        // Act
        followerNode.requestRPC(leaderNode, "AppendEntries");

        // Assert
        Assert.Equal(leaderNode, followerNode.GetCurrentLeader());
    }

    //3
    [Fact]
    public void NewNodeStartsAsFollower()
    {
        // Arrange
        var newNode = new ServerNode(true);

        // Act
        var initialState = newNode.State;

        // Assert
        Assert.Equal(NodeState.Follower, initialState);
    }

    //4
    [Fact]
    public void FollowerStartsElectionAfterTimeout()
    {
        // Arrange
        var follower = new ServerNode(true);

        // Act
        Thread.Sleep(350);

        // Assert
        Assert.Equal(NodeState.Candidate, follower.State);
    }

    //6
    [Fact]
    public void ElectionIncrementsTerm()
    {
        // Arrange
        var node = new ServerNode(true);

        // Act
        int initialTerm = node.Term;
        Thread.Sleep(350);
        int newTerm = node.Term;

        // Assert
        Assert.True(newTerm > initialTerm, "Term should increment when an election starts.");
        Assert.Equal(initialTerm + 1, newTerm);
    }

    //7
    [Fact]
    public void AppendEntriesResetsElectionTimer()
    {
        // Arrange
        var mockNeighbor1 = Substitute.For<IServerNode>();
        var mockNeighbor2 = Substitute.For<IServerNode>();
        var mockNeighbors = new List<IServerNode> { mockNeighbor1, mockNeighbor2 };
        var server = new ServerNode(true, mockNeighbors);

        // Simulate leader RPC request
        var leader = new ServerNode(true);
        server.requestRPC(leader, "AppendEntries");

        // Act
        server.BecomeLeader();
        Thread.Sleep(300); // Adjust time to fit the timer interval

        // Assert
        mockNeighbor1.ReceivedWithAnyArgs().respondRPC();
        mockNeighbor2.ReceivedWithAnyArgs().respondRPC();
    }
}
