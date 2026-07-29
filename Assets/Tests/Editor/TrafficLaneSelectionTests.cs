using FriendOfOurs.Traffic;
using NUnit.Framework;
using Unity.Mathematics;
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

    [Test]
    public void FindClosestLaneIndexReturnsNearestLane()
    {
        var networkObject = new GameObject("Traffic Network");
        var container = networkObject.AddComponent<SplineContainer>();
        var nearSpline = new Spline();
        nearSpline.Add(new BezierKnot(new float3(0f, 0f, 0f)));
        nearSpline.Add(new BezierKnot(new float3(0f, 0f, 10f)));

        var farSpline = new Spline();
        farSpline.Add(new BezierKnot(new float3(20f, 0f, 0f)));
        farSpline.Add(new BezierKnot(new float3(20f, 0f, 10f)));

        container.Splines = new[] { nearSpline, farSpline };
        TrafficNetwork network = networkObject.AddComponent<TrafficNetwork>();
        network.SetLanesForTests(
            new TrafficLaneData("Near", 0, 8f),
            new TrafficLaneData("Far", 1, 8f));

        int closestLaneIndex = network.FindClosestLaneIndex(new Vector3(18f, 0f, 5f), sampleCount: 8);

        Assert.AreEqual(1, closestLaneIndex);

        Object.DestroyImmediate(networkObject);
    }

    [Test]
    public void ObstacleResponseStopsInsideStopDistance()
    {
        TrafficObstacleResponse response = TrafficObstacleResponse.Calculate(
            hasObstacle: true,
            obstacleDistance: 1.5f,
            stopDistance: 2f,
            slowDistance: 8f);

        Assert.AreEqual(0f, response.SpeedFactor);
        Assert.IsTrue(response.ShouldStop);
    }

    [Test]
    public void ObstacleResponseSlowsBetweenStopAndSlowDistance()
    {
        TrafficObstacleResponse response = TrafficObstacleResponse.Calculate(
            hasObstacle: true,
            obstacleDistance: 5f,
            stopDistance: 2f,
            slowDistance: 8f);

        Assert.Greater(response.SpeedFactor, 0f);
        Assert.Less(response.SpeedFactor, 1f);
        Assert.IsFalse(response.ShouldStop);
    }

    [Test]
    public void ObstacleFilterAcceptsNonVehicleColliderWhenVehicleOnlyModeIsDisabled()
    {
        var sensorObject = new GameObject("Sensor");
        var playerObject = new GameObject("Player");
        Collider playerCollider = playerObject.AddComponent<BoxCollider>();

        bool canUseHit = VehicleObstacleSensor.CanUseHit(
            playerCollider,
            sensorObject.transform,
            detectOnlyNpcCars: false);

        Assert.IsTrue(canUseHit);

        Object.DestroyImmediate(sensorObject);
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void BrakeTorqueUsesObstacleBrakeWhenSlowingForDetectedObstacle()
    {
        float brakeTorque = TrafficSteeringMath.GetBrakeTorque(
            currentSpeed: 7f,
            targetSpeed: 4f,
            hasObstacle: true,
            shouldStop: false,
            speedLimitBrakeTorque: 120f,
            obstacleBrakeTorque: 650f,
            stopBrakeTorque: 1600f);

        Assert.AreEqual(650f, brakeTorque);
    }
}
