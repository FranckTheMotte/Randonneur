using Randonneur;

namespace TestWaypoints;

public class Tests
{
    Waypoint? SimpleWaypoint;

    static readonly float SimpleElevation = 0.0f;
    static readonly Godot.Vector2 SimpleGeographicCoord = new(1, 1);
    static readonly string SimpleTraceName = "TraceA";

    [SetUp]
    public void Setup()
    {
        SimpleWaypoint = new("Test")
        {
            Elevation = SimpleElevation,
            GeographicCoord = SimpleGeographicCoord,
            TraceName = SimpleTraceName,
        };
    }

    /// <summary>
    /// Simple test of Waypoint class
    /// </summary>
    [Test]
    public void SimpleTest()
    {
        Assert.That(SimpleWaypoint, Is.Not.Null);
        Assert.That(SimpleWaypoint?.Elevation, Is.EqualTo(SimpleElevation));
        Assert.That(SimpleWaypoint?.GeographicCoord, Is.EqualTo(SimpleGeographicCoord));
        Assert.That(SimpleWaypoint?.TraceName, Is.EqualTo(SimpleTraceName));
    }
}
