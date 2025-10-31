using Randonneur;

namespace TestWaypoints;

// Default values
file static class Default
{
    // Generic waypoint constants
    public static readonly string TemplateName = "Simple";
    public static readonly string TemplateTraceName = "Trace";

    public static readonly float Elevation = 0.0f;
    public static readonly Godot.Vector2 GeographicCoord = new(1, 1);
    public static readonly Godot.Vector2 LevelCoord = new(10, 10);

    // traces
    public static readonly string TraceAName = Default.TemplateTraceName + "A";
    public static readonly string TraceBName = Default.TemplateTraceName + "B";
    public static readonly string TraceCName = Default.TemplateTraceName + "C";
    public static readonly string TraceDName = Default.TemplateTraceName + "D";
    public static readonly string TraceEName = Default.TemplateTraceName + "E";
}

public class SimpleTests
{
    Waypoint? SimpleWaypoint;

    // Simple constants
    static readonly string SimpleName = "Simple";
    static readonly string SimpleTraceName = "TraceA";

    // Level reference
    static readonly Godot.Vector2[] WaypointLevelCoord = [new(10, 10), new(100, 100)];
    static readonly Dictionary<string, int>[] WaypointLevelOrder =
    {
        new() { [Default.TraceAName] = 1 },
        new() { [Default.TraceAName] = 2 },
    };

    [SetUp]
    public void Setup()
    {
        // simple test
        SimpleWaypoint = new(SimpleName)
        {
            Elevation = Default.Elevation,
            GeographicCoord = Default.GeographicCoord,
            LevelCoord = Default.LevelCoord,
            TraceName = SimpleTraceName,
        };

        // a trace two waypoints
        Waypoint Waypoint1 = new(Default.TemplateName + "1")
        {
            Elevation = Default.Elevation,
            GeographicCoord = Default.GeographicCoord,
            LevelCoord = WaypointLevelCoord[0],
            LevelOrder = WaypointLevelOrder[0],
            TraceName = Default.TraceAName,
        };

        Waypoint Waypoint2 = new(Default.TemplateName + "2")
        {
            Elevation = Default.Elevation,
            GeographicCoord = Default.GeographicCoord,
            LevelCoord = WaypointLevelCoord[1],
            LevelOrder = WaypointLevelOrder[1],
            TraceName = Default.TraceAName,
        };
        Waypoints links = (Waypoints)Waypoints.Instance;
        links.Add(Waypoint1);
        links.Add(Waypoint2);
    }

    [TearDown]
    public void TearDown()
    {
        Waypoints.Reset();
    }

    /// <summary>
    /// Simple test of Waypoint class
    /// </summary>
    [Test]
    public void SimpleTest()
    {
        Assert.That(SimpleWaypoint, Is.Not.Null);
        Assert.That(SimpleWaypoint?.Elevation, Is.EqualTo(Default.Elevation));
        Assert.That(SimpleWaypoint?.GeographicCoord, Is.EqualTo(Default.GeographicCoord));
        Assert.That(SimpleWaypoint?.LevelCoord, Is.EqualTo(Default.LevelCoord));
        Assert.That(SimpleWaypoint?.Name, Is.EqualTo(SimpleName));
        Assert.That(SimpleWaypoint?.TraceName, Is.EqualTo(SimpleTraceName));
    }

    /// <summary>
    /// Test a trace with two waypoints
    /// </summary>
    [Test]
    public void TraceWith2Waypoints()
    {
        Dictionary<string, WaypointsLinks>? links = Waypoints.Instance.Links;
        Assert.That(links, Is.Not.Null);
        Assert.That(links!.Count, Is.EqualTo(2));

        int i = 1;
        foreach (KeyValuePair<string, WaypointsLinks> link in links)
        {
            Waypoint currentWaypoint = link.Value.Waypoint;
            Assert.That(currentWaypoint?.Elevation, Is.EqualTo(Default.Elevation));
            Assert.That(currentWaypoint?.GeographicCoord, Is.EqualTo(Default.GeographicCoord));
            Assert.That(currentWaypoint?.LevelCoord, Is.EqualTo(WaypointLevelCoord[i - 1]));
            Assert.That(currentWaypoint?.LevelOrder, Is.EqualTo(WaypointLevelOrder[i - 1]));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(Default.TemplateName + i));
            Assert.That(currentWaypoint?.TraceName, Is.EqualTo(Default.TemplateTraceName + "A"));

            // the waypoint must be connected
            foreach (
                KeyValuePair<
                    string,
                    ConnectedWaypoint
                > connectedWaypoint in link.Value.ConnectedWaypoints
            )
            {
                Waypoint destWaypoint = connectedWaypoint.Value.Waypoint;
                Assert.That(destWaypoint.Elevation, Is.EqualTo(Default.Elevation));
                Assert.That(destWaypoint.GeographicCoord, Is.EqualTo(Default.GeographicCoord));
                switch (i)
                {
                    case 1:
                        Assert.That(destWaypoint.LevelCoord, Is.EqualTo(WaypointLevelCoord[1]));
                        Assert.That(destWaypoint.LevelOrder, Is.EqualTo(WaypointLevelOrder[1]));
                        Assert.That(destWaypoint.Name, Is.EqualTo(Default.TemplateName + 2));
                        break;
                    case 2:
                        Assert.That(destWaypoint.LevelCoord, Is.EqualTo(WaypointLevelCoord[0]));
                        Assert.That(destWaypoint.LevelOrder, Is.EqualTo(WaypointLevelOrder[0]));
                        Assert.That(destWaypoint.Name, Is.EqualTo(Default.TemplateName + 1));
                        break;
                }
                Assert.That(
                    connectedWaypoint.Value.TraceName,
                    Is.EqualTo(Default.TemplateTraceName + "A")
                );
                Assert.That(destWaypoint.TraceName, Is.EqualTo(Default.TemplateTraceName + "A"));
            }

            i++;
        }
    }
}

/// <summary>
/// Describe 2 traces with 2 waypoints each to form a :
///   X-----X-----X
/// </summary>
[TestFixture]
public class TwoTraces
{
    static readonly Waypoint A1 = new("WPT1")
    {
        TraceName = Default.TraceAName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceAName] = 1 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint A2 = new("WPT2")
    {
        TraceName = Default.TraceAName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceAName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint B1 = new("WPT2")
    {
        TraceName = Default.TraceBName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceBName] = 1 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint B2 = new("WPT3")
    {
        TraceName = Default.TraceBName,
        Elevation = Default.Elevation,
        GeographicCoord = new(100, 100),
        LevelOrder = new Dictionary<string, int> { [Default.TraceBName] = 2 },
        LevelCoord = Default.LevelCoord,
    };

    readonly Waypoint[] TestedWaypoints = [A1, A2, B1, B2];
    readonly Waypoint[][] LinkedWaypoints =
    [
        [A2],
        [A1, B2],
        [B1],
    ];
    readonly int[] NbConnectedWaypoints = [1, 2, 0, 1];

    [SetUp]
    public void Setup()
    {
        Waypoints links = (Waypoints)Waypoints.Instance;
        links.Add(A1);
        links.Add(A2);
        links.Add(B1);
        links.Add(B2);
    }

    [TearDown]
    public void TearDown()
    {
        Waypoints.Reset();
    }

    /// <summary>
    /// Test 2 traces with 3 waypoints (one common waypoint)
    /// </summary>
    [Test]
    public void Test2TracesWithWaypoints()
    {
        Dictionary<string, WaypointsLinks>? links = Waypoints.Instance.Links;
        Assert.That(links, Is.Not.Null);
        Assert.That(links!.Count, Is.EqualTo(3));

        int i = 0;
        foreach (KeyValuePair<string, WaypointsLinks> link in links)
        {
            Waypoint currentWaypoint = link.Value.Waypoint;

            // B1 must be skipped
            if (NbConnectedWaypoints[i] == 0)
            {
                i++;
                continue;
            }
            Assert.That(currentWaypoint?.Elevation, Is.EqualTo(TestedWaypoints[i].Elevation));
            Assert.That(
                currentWaypoint?.GeographicCoord,
                Is.EqualTo(TestedWaypoints[i].GeographicCoord)
            );
            Assert.That(currentWaypoint?.LevelCoord, Is.EqualTo(TestedWaypoints[i].LevelCoord));
            Assert.That(currentWaypoint?.LevelOrder, Is.EqualTo(TestedWaypoints[i].LevelOrder));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(TestedWaypoints[i].Name));
            Assert.That(currentWaypoint?.TraceName, Is.EqualTo(TestedWaypoints[i].TraceName));

            // the waypoint must be connected
            Assert.That(link.Value.ConnectedWaypoints.Count, Is.EqualTo(NbConnectedWaypoints[i]));
            int j = 0;
            foreach (
                KeyValuePair<
                    string,
                    ConnectedWaypoint
                > connectedWaypoint in link.Value.ConnectedWaypoints
            )
            {
                Waypoint destWaypoint = connectedWaypoint.Value.Waypoint;
                Assert.That(destWaypoint.Elevation, Is.EqualTo(LinkedWaypoints[i][j].Elevation));
                Assert.That(
                    destWaypoint.GeographicCoord,
                    Is.EqualTo(LinkedWaypoints[i][j].GeographicCoord)
                );
                Assert.That(destWaypoint.LevelCoord, Is.EqualTo(LinkedWaypoints[i][j].LevelCoord));
                Assert.That(destWaypoint.LevelOrder, Is.EqualTo(LinkedWaypoints[i][j].LevelOrder));
                Assert.That(destWaypoint.Name, Is.EqualTo(LinkedWaypoints[i][j].Name));
                Assert.That(destWaypoint.TraceName, Is.EqualTo(LinkedWaypoints[i][j].TraceName));
                j++;
            }

            i++;
        }
    }
}

/// <summary>
/// Describe 4 traces with 2 waypoints each to form a :
///   X-----X
///   |     |
///   X-----X
/// </summary>
public class FourTraces
{
    static readonly Waypoint A1 = new("WPT1")
    {
        TraceName = Default.TraceAName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceAName] = 1 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint A2 = new("WPT2")
    {
        TraceName = Default.TraceAName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceAName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint B1 = new("WPT2")
    {
        TraceName = Default.TraceBName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceBName] = 1 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint B2 = new("WPT3")
    {
        TraceName = Default.TraceBName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceBName] = 2 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint C1 = new("WPT3")
    {
        TraceName = Default.TraceCName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceCName] = 1 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint C2 = new("WPT4")
    {
        TraceName = Default.TraceCName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceCName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint D1 = new("WPT4")
    {
        TraceName = Default.TraceDName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceDName] = 1 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint D2 = new("WPT1")
    {
        TraceName = Default.TraceDName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceDName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    readonly Waypoint[] TestedWaypoints = [A1, A2, B2, C2];
    readonly Waypoint[][] LinkedWaypoints =
    [
        [A2, C2], // A1
        [A1, B2], // A2
        [A2, C2], // B2
        [B2, A1], // C2
    ];
    readonly int[] NbConnectedWaypoints = [2, 2, 2, 2];

    [SetUp]
    public void Setup()
    {
        Waypoints links = (Waypoints)Waypoints.Instance;
        links.Add(A1);
        links.Add(A2);
        links.Add(B1);
        links.Add(B2);
        links.Add(C1);
        links.Add(C2);
        links.Add(D1);
        links.Add(D2);
    }

    [TearDown]
    public void TearDown()
    {
        Waypoints.Reset();
    }

    /// <summary>
    /// Test 4 traces with 4 waypoints (one common waypoint between connected traces)
    /// </summary>
    [Test]
    public void Test4TracesWithWaypoints()
    {
        Dictionary<string, WaypointsLinks>? links = Waypoints.Instance.Links;
        Assert.That(links, Is.Not.Null);
        Assert.That(links!.Count, Is.EqualTo(4));
        Waypoints.Instance.DisplayLinks();
        int i = 0;
        foreach (KeyValuePair<string, WaypointsLinks> link in links)
        {
            Waypoint currentWaypoint = link.Value.Waypoint;

            // Overwritten waypoints (no connection) must be skipped
            /*if (NbConnectedWaypoints[i] == 0)
            {
                i++;
                continue;
            }*/
            Console.Write($"i {i} name {currentWaypoint?.Name} tested {TestedWaypoints[i].Name}\n");
            Assert.That(currentWaypoint?.Elevation, Is.EqualTo(TestedWaypoints[i].Elevation));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(TestedWaypoints[i].Name));
            Assert.That(
                currentWaypoint?.GeographicCoord,
                Is.EqualTo(TestedWaypoints[i].GeographicCoord)
            );
            Assert.That(currentWaypoint?.LevelCoord, Is.EqualTo(TestedWaypoints[i].LevelCoord));
            Assert.That(currentWaypoint?.LevelOrder, Is.EqualTo(TestedWaypoints[i].LevelOrder));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(TestedWaypoints[i].Name));
            Assert.That(currentWaypoint?.TraceName, Is.EqualTo(TestedWaypoints[i].TraceName));

            // the waypoint must be connected
            Assert.That(link.Value.ConnectedWaypoints.Count, Is.EqualTo(NbConnectedWaypoints[i]));
            int j = 0;
            foreach (
                KeyValuePair<
                    string,
                    ConnectedWaypoint
                > connectedWaypoint in link.Value.ConnectedWaypoints
            )
            {
                Waypoint destWaypoint = connectedWaypoint.Value.Waypoint;
                Assert.That(destWaypoint.Elevation, Is.EqualTo(LinkedWaypoints[i][j].Elevation));
                Assert.That(
                    destWaypoint.GeographicCoord,
                    Is.EqualTo(LinkedWaypoints[i][j].GeographicCoord)
                );
                Assert.That(destWaypoint.LevelCoord, Is.EqualTo(LinkedWaypoints[i][j].LevelCoord));
                Assert.That(destWaypoint.LevelOrder, Is.EqualTo(LinkedWaypoints[i][j].LevelOrder));
                Assert.That(destWaypoint.Name, Is.EqualTo(LinkedWaypoints[i][j].Name));
                Assert.That(destWaypoint.TraceName, Is.EqualTo(LinkedWaypoints[i][j].TraceName));
                j++;
            }

            i++;
        }
    }
}

/// <summary>
/// Describe 5 traces with 2 waypoints each to form a :
///   X-----X
///   |   / |
///   |  /  |
///   | /   |
///   X-----X
/// </summary>
public class FiveTraces
{
    static readonly Waypoint A1 = new("WPT1")
    {
        TraceName = Default.TraceAName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceAName] = 1 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint A2 = new("WPT2")
    {
        TraceName = Default.TraceAName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceAName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint B1 = new("WPT2")
    {
        TraceName = Default.TraceBName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceBName] = 1 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint B2 = new("WPT3")
    {
        TraceName = Default.TraceBName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceBName] = 2 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint C1 = new("WPT3")
    {
        TraceName = Default.TraceCName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceCName] = 1 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint C2 = new("WPT4")
    {
        TraceName = Default.TraceCName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceCName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint D1 = new("WPT4")
    {
        TraceName = Default.TraceDName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceDName] = 1 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint D2 = new("WPT1")
    {
        TraceName = Default.TraceDName,
        Elevation = Default.Elevation,
        GeographicCoord = new(0, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceDName] = 2 },
        LevelCoord = Default.LevelCoord,
    };
    static readonly Waypoint E1 = new("WPT2")
    {
        TraceName = Default.TraceEName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 0),
        LevelOrder = new Dictionary<string, int> { [Default.TraceEName] = 1 },
        LevelCoord = Default.LevelCoord,
    };

    static readonly Waypoint E2 = new("WPT4")
    {
        TraceName = Default.TraceEName,
        Elevation = Default.Elevation,
        GeographicCoord = new(10, 10),
        LevelOrder = new Dictionary<string, int> { [Default.TraceEName] = 2 },
        LevelCoord = Default.LevelCoord,
    };

    readonly Waypoint[] TestedWaypoints = [A1, A2, B2, C2];
    readonly Waypoint[][] LinkedWaypoints =
    [
        [A2, C2], // A1
        [A1, B2, C2], // A2
        [A2, C2], // B2
        [B2, A1, A2], // C2
    ];
    readonly int[] NbConnectedWaypoints = [2, 3, 2, 3];

    [SetUp]
    public void Setup()
    {
        Waypoints links = (Waypoints)Waypoints.Instance;
        links.Add(A1);
        links.Add(A2);
        links.Add(B1);
        links.Add(B2);
        links.Add(C1);
        links.Add(C2);
        links.Add(D1);
        links.Add(D2);
        links.Add(E1);
        links.Add(E2);
    }

    [TearDown]
    public void TearDown()
    {
        Waypoints.Reset();
    }

    /// <summary>
    /// Test 4 traces with 4 waypoints (one common waypoint between connected traces)
    /// </summary>
    [Test]
    public void Test4TracesWithWaypoints()
    {
        Dictionary<string, WaypointsLinks>? links = Waypoints.Instance.Links;
        Assert.That(links, Is.Not.Null);
        Assert.That(links!.Count, Is.EqualTo(4));
        Waypoints.Instance.DisplayLinks();
        int i = 0;
        foreach (KeyValuePair<string, WaypointsLinks> link in links)
        {
            Waypoint currentWaypoint = link.Value.Waypoint;

            // Overwritten waypoints (no connection) must be skipped
            /*if (NbConnectedWaypoints[i] == 0)
            {
                i++;
                continue;
            }*/
            Console.Write($"i {i} name {currentWaypoint?.Name} tested {TestedWaypoints[i].Name}\n");
            Assert.That(currentWaypoint?.Elevation, Is.EqualTo(TestedWaypoints[i].Elevation));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(TestedWaypoints[i].Name));
            Assert.That(
                currentWaypoint?.GeographicCoord,
                Is.EqualTo(TestedWaypoints[i].GeographicCoord)
            );
            Assert.That(currentWaypoint?.LevelCoord, Is.EqualTo(TestedWaypoints[i].LevelCoord));
            Assert.That(currentWaypoint?.LevelOrder, Is.EqualTo(TestedWaypoints[i].LevelOrder));
            Assert.That(currentWaypoint?.Name, Is.EqualTo(TestedWaypoints[i].Name));
            Assert.That(currentWaypoint?.TraceName, Is.EqualTo(TestedWaypoints[i].TraceName));

            // the waypoint must be connected
            Assert.That(link.Value.ConnectedWaypoints.Count, Is.EqualTo(NbConnectedWaypoints[i]));
            int j = 0;
            foreach (
                KeyValuePair<
                    string,
                    ConnectedWaypoint
                > connectedWaypoint in link.Value.ConnectedWaypoints
            )
            {
                Waypoint destWaypoint = connectedWaypoint.Value.Waypoint;
                Assert.That(destWaypoint.Elevation, Is.EqualTo(LinkedWaypoints[i][j].Elevation));
                Assert.That(
                    destWaypoint.GeographicCoord,
                    Is.EqualTo(LinkedWaypoints[i][j].GeographicCoord)
                );
                Assert.That(destWaypoint.LevelCoord, Is.EqualTo(LinkedWaypoints[i][j].LevelCoord));
                Assert.That(destWaypoint.LevelOrder, Is.EqualTo(LinkedWaypoints[i][j].LevelOrder));
                Assert.That(destWaypoint.Name, Is.EqualTo(LinkedWaypoints[i][j].Name));
                Assert.That(destWaypoint.TraceName, Is.EqualTo(LinkedWaypoints[i][j].TraceName));
                j++;
            }

            i++;
        }
    }
}
