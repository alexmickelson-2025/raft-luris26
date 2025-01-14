using classlibrary;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace test;

public class UnitTest1
{
    [Fact]
    public void LeaderSendsHeartbeatsToAllNeighbors()
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
        mockNeighbor1.ReceivedWithAnyArgs(4).ReceiveRPC();
        mockNeighbor2.ReceivedWithAnyArgs(4).ReceiveRPC();
    }
}
