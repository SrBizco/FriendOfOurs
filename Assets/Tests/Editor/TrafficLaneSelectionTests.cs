using FriendOfOurs.Traffic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Splines;

public sealed class TrafficLaneSelectionTests
{
    [Test]
    public void SelectNextLaneIndexReturnsOnlyAvailableConnection()
    {
        var connections = new[]
        {
            new TrafficLaneConnection(2, TrafficTurnType.Right, 1f)
        };

        int selected = TrafficLaneSelection.SelectNextLaneIndex(connections, laneCount: 3, roll: 0.5f);

        Assert.AreEqual(2, selected);
    }

    [Test]
    public void SelectNextLaneIndexReturnsMinusOneWhenConnectionTargetsMissingLane()
    {
        var connections = new[]
        {
            new TrafficLaneConnection(5, TrafficTurnType.Straight, 1f)
        };

        int selected = TrafficLaneSelection.SelectNextLaneIndex(connections, laneCount: 3, roll: 0.5f);

        Assert.AreEqual(-1, selected);
    }

    [Test]
    public void EnabledNetworkBecomesActiveNetwork()
    {
        var networkObject = new GameObject("Traffic Network");
        networkObject.AddComponent<SplineContainer>();

        TrafficNetwork network = networkObject.AddComponent<TrafficNetwork>();

        Assert.AreSame(network, TrafficNetwork.Active);

        Object.DestroyImmediate(networkObject);
        Assert.IsNull(TrafficNetwork.Active);
    }
}
