using Randonneur;

namespace TestWaypoints;

public class Tests
{
    Waypoint? SimpleWaypoint;

    // Default values
    static readonly float DefaultElevation = 0.0f;
    static readonly Godot.Vector2 DefaultGeographicCoord = new(1, 1);

    // Simple constants
    static readonly string SimpleName = "Simple";
    static readonly string SimpleTraceName = "TraceA";

    // Generic waypoint constants
    static readonly string TemplateName = "Simple";
    static readonly string TemplateTraceName = "Trace";

    [SetUp]
    public void Setup()
    {
        // simple test
        SimpleWaypoint = new(SimpleName)
        {
            Elevation = DefaultElevation,
            GeographicCoord = DefaultGeographicCoord,
            TraceName = SimpleTraceName,
        };

        // a trace two waypoints
        Waypoint Waypoint1 = new(TemplateName + "1")
        {
            Elevation = DefaultElevation,
            GeographicCoord = DefaultGeographicCoord,
            TraceName = TemplateTraceName + "A",
        };

        Waypoint Waypoint2 = new(TemplateName + "2")
        {
            Elevation = DefaultElevation,
            GeographicCoord = DefaultGeographicCoord,
            TraceName = TemplateTraceName + "A",
        };
        Waypoints links = Waypoints.Instance;
        links.Add(Waypoint1);
        links.Add(Waypoint2);
    }

    /// <summary>
    /// Simple test of Waypoint class
    /// </summary>
    [Test]
    public void SimpleTest()
    {
        Assert.That(SimpleWaypoint, Is.Not.Null);
        Assert.That(SimpleWaypoint?.Elevation, Is.EqualTo(DefaultElevation));
        Assert.That(SimpleWaypoint?.GeographicCoord, Is.EqualTo(DefaultGeographicCoord));
        Assert.That(SimpleWaypoint?.Name, Is.EqualTo(SimpleName));
        Assert.That(SimpleWaypoint?.TraceName, Is.EqualTo(SimpleTraceName));
    }

    /// <summary>
    /// Test a trace with two waypoints
    /// </summary>
    [Test]
    public void TraceWith2Waypoints()
    {
        Dictionary<string, WaypointsLinks> links = Waypoints.Instance.Links;
        Assert.That(links.Count, Is.EqualTo(2));

        int i = 1;
        foreach (KeyValuePair<string, WaypointsLinks> link in links)
        {
            Waypoint currentWaypoint = link.Value.Waypoint;
            Assert.That(currentWaypoint?.Elevation, Is.EqualTo(DefaultElevation));
            Assert.That(currentWaypoint?.GeographicCoord, Is.EqualTo(DefaultGeographicCoord));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(TemplateName + i));
            Assert.That(currentWaypoint?.TraceName, Is.EqualTo(TemplateTraceName + "A"));

            // the waypoint must be connected
            foreach (
                KeyValuePair<
                    string,
                    ConnectedWaypoint
                > connectedWaypoint in link.Value.ConnectedWaypoints
            )
            {
                Waypoint destWaypoint = connectedWaypoint.Value.Waypoint;
                Assert.That(destWaypoint.Elevation, Is.EqualTo(DefaultElevation));
                Assert.That(destWaypoint.GeographicCoord, Is.EqualTo(DefaultGeographicCoord));
                switch (i)
                {
                    case 1:
                        Assert.That(destWaypoint.Name, Is.EqualTo(TemplateName + 2));
                        break;
                    case 2:
                        Assert.That(destWaypoint.Name, Is.EqualTo(TemplateName + 1));
                        break;
                }
                Assert.That(connectedWaypoint.Value.TraceName, Is.EqualTo(TemplateTraceName + "A"));
                Assert.That(destWaypoint.TraceName, Is.EqualTo(TemplateTraceName + "A"));
            }

            i++;
        }
    }
}
