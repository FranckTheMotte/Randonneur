using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using Godot.Collections;
using Randonneur;
using static Godot.GD;

/// <summary>
/// Store a junction by trace.
/// </summary>
public class TrailJunction
{
    /// <summary>
    /// Name of trace use to match correct properties from Waypoint.
    /// </summary>
    public string TraceName { get; set; } = "None";

    /// <summary>
    /// Waypoint to display a junction.
    /// </summary>
    public Waypoint? Waypoint;
}

public partial class Sol : StaticBody2D
{
    private List<TrailJunction> _trailJunctions = [];

    public Gpx? CurrentTrack { get; set; }

    // Godot group of all wyapoints
    private const string _WaypointsGroup = "Waypoints";

    PackedScene FlowerScene = GD.Load<PackedScene>("res://Scenes/flower.tscn");

    /// <summary>
    /// The maximum distance from the origin in the level the player can go.
    /// </summary>
    [Export]
    public float LevelLimitX { get; set; } = 10000.0f;

    public override void _Draw()
    {
        foreach (TrailJunction junction in _trailJunctions)
        {
            Waypoint? waypoint = junction.Waypoint;
            if (waypoint != null)
            {
                DrawCircle(waypoint.LevelCoord[junction.TraceName], 10.0f, Colors.Blue);
                Font defaultFont = ThemeDB.FallbackFont;
                int defaultFontSize = ThemeDB.FallbackFontSize;
                DrawString(
                    defaultFont,
                    waypoint.LevelCoord[junction.TraceName],
                    waypoint.Name,
                    modulate: new Color(200, 0, 0)
                );
            }
        }
        base._Draw();
    }

    private void AddFlower(Vector2 Position)
    {
        // put a flower
        Area2D flowerScene = (Area2D)FlowerScene.Instantiate();
        flowerScene.Position = new Vector2(Position.X, Position.Y - 5);
        AddChild(flowerScene);
    }

    /// <summary>
    /// Generate a solid ground from a gpx file.
    /// <param name="gpxFile">Full godot path to the gpx file.</param>
    /// </summary>
    public void GenerateGround(string gpxFile)
    {
        // reset
        _trailJunctions = [];

        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        if (Godot.FileAccess.FileExists(gpxFile))
        {
            CollisionPolygon2D solCollision = GetNode<CollisionPolygon2D>(
                "CollisionProfilElevation"
            );
            Polygon2D underground = GetNode<Polygon2D>("Underground");

            /* Generate a profil from a gpx file */
            CurrentTrack = new Gpx();
            CurrentTrack.Load(gpxFile);

            if (CurrentTrack.TrackPoints == null)
            {
                GD.PushWarning(
                    $"{nameof(GenerateGround)}: no track points in current gpx file ${gpxFile}"
                );
                watch.Stop();
                return;
            }
            /* Add :
              - 2 points to add a ground before and after the limit
              - 2 points to finish the polygon
              */
            Vector2[] ground = new Vector2[CurrentTrack.TrackPoints.Length + 2 + 2];

            // set a start
            ground[0].X = -1000;
            ground[0].Y = CurrentTrack.TrackPoints[0].LevelCoord.Y;
            string traceName = Path.GetFileName(gpxFile);

            int solLength = CurrentTrack.TrackPoints.Length;
            int gi = 1; // ground index starts after fake start
            for (int i = 0; i < solLength; i++, gi++)
            {
                // Put the display coord
                ground[gi] = CurrentTrack.TrackPoints[i].LevelCoord;
                // Display a waypoint
                Waypoint? waypoint = CurrentTrack.TrackPoints[i].Waypoint;
                if (waypoint != null)
                {
                    // TODO: here only to display a graphic object for junction
                    waypoint.LevelCoord[traceName] = ground[gi];
                    TrailJunction junction = new() { TraceName = traceName, Waypoint = waypoint };
                    _trailJunctions.Add(junction);

                    // Level order is the same as ground point index
                    Waypoint? TargetWaypoint = Waypoints.Instance.GetWaypoint(waypoint.Name);
                    if (TargetWaypoint != null)
                    {
                        TargetWaypoint.LevelOrder[traceName] = i;
                    }

                    Area2D junctionArea = new() { Position = ground[gi], Name = traceName };
                    // Use the junction collision layer
                    junctionArea.SetCollisionLayerValue(Global.PlayerLayer, false);
                    junctionArea.SetCollisionLayerValue(Global.TrailJunctionLayer, true);
                    junctionArea.SetCollisionMaskValue(Global.PlayerLayer, false);
                    junctionArea.SetCollisionMaskValue(Global.TrailJunctionLayer, true);
                    RectangleShape2D rectangle = new()
                    {
                        Size = new Vector2(
                            Global.JunctionCollisionShapeSize,
                            Global.JunctionCollisionShapeSize
                        ),
                    };
                    CollisionShape2D junctionCollision = new() { Shape = rectangle };
                    junctionCollision.AddToGroup(_WaypointsGroup);
                    junctionArea.BodyEntered += delegate
                    {
                        JunctionHandler(junctionArea, junctionCollision, traceName, waypoint.Name);
                    };
                    junctionArea.AddChild(junctionCollision);
                    GetParent().AddChild(junctionArea);
                }

                // add some flowers
                AddFlower(ground[gi]);
            }

            // set an end
            ground[solLength + 1].X = ground[solLength].X + 1000;
            ground[solLength + 1].Y = ground[solLength].Y;

            // close the ground polygon
            ground[solLength + 2].X = CurrentTrack.MaxX;
            ground[solLength + 2].Y = Gpx.PixelElevationMax;
            ground[solLength + 3].X = 0.00f;
            ground[solLength + 3].Y = Gpx.PixelElevationMax;

            underground.Polygon = ground;
            solCollision.Polygon = underground.Polygon;

            GenerateGrass(CurrentTrack.TrackPoints.Length, ground);

            /* player limit */
            LevelLimitX = CurrentTrack.MaxX;
            GD.Print($"world limit X : {LevelLimitX}");
        }
        /* TODO put default value if no Gpx is provided */
        watch.Stop();
        Print($"Ground creation Time: {watch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Add blades of grass of the surface of a ground.
    /// </summary>
    /// <param name="trackPointsCount">Number of trackpoints.</param>
    /// <param name="ground">Array of trackpoints.</param>
    private void GenerateGrass(int trackPointsCount, Vector2[] ground)
    {
        // Sanity checks
        if (ground == null || ground.Length < 3)
            throw new ArgumentException("Ground must have at least 3 points");

        Polygon2D underground = GetNode<Polygon2D>("Underground");
        // Duplicate the upper part of ground to define the walking line
        // (the last 2 points are ignored, they define the bottom of the poligon)
        Vector2[] surface = ground[..^2];

        Line2D walkingLine = new() { Points = surface };

        // it defines the widht and height of a blade of grass
        QuadMesh quad = new() { Size = new Vector2(4, 40) };

        // shader to apply texture
        ShaderMaterial material = new()
        {
            Shader = GD.Load<Shader>("res://Scripts/grass.gdshader"),
        };
        material.SetShaderParameter(
            "grass_texture",
            GD.Load<Texture2D>("res://Art/Background/brinherbe.png")
        );

        // TODO:
        // The grass counter must be linked with the length's trail and the
        // density of grass.
        int grassCount = trackPointsCount * 60;
        MultiMesh multiMesh = new()
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            InstanceCount = grassCount,
            Mesh = quad,
        };

        // Restrict to the track area
        Rect2 bounds = GetPolygonBounds(surface);

        // required to put randomly the blades of grass
        RandomNumberGenerator rng = new();
        rng.Randomize();

        int index = 0;
        float grassRotationStart = Mathf.Tau / 1.9f;
        float grassRotationEnd = Mathf.Tau / 2.1f;
        float minXBounds = bounds.Position.X;
        float maxXBounds = bounds.Position.X + bounds.Size.X;
        while (index < grassCount)
        {
            // Y will be modified
            Vector2 p = new(rng.RandfRange(minXBounds, maxXBounds), 0);

            // each point y must be attached to the walking line
            var (result, point) = AlignPointOnLine(p, walkingLine);
            if (result)
            {
                // set different size
                float scale = rng.RandfRange(0.3f, 0.7f);

                multiMesh.SetInstanceTransform2D(
                    index,
                    new Transform2D(
                        rng.RandfRange(grassRotationStart, grassRotationEnd),
                        Vector2.One * scale,
                        0,
                        point
                    )
                );
            }

            index++;
        }
        MultiMeshInstance2D multiMeshInstance = GetNode<MultiMeshInstance2D>("WalkingSurface");

        // don't forget to free an old instace of multiMesh
        multiMeshInstance.Multimesh?.Dispose();

        multiMeshInstance.Multimesh = multiMesh;
        multiMeshInstance.Material = material;

        walkingLine.QueueFree();
    }

    /// <summary>
    /// Retrieve the Y value on a line from X.
    /// </summary>
    /// <param name="line">An horizontal line (can be irregular).</param>
    /// <param name="x">A x enclosed in line bound</param>
    /// <returns></returns>
    private static float FindYOnLine(Line2D line, float x)
    {
        var points = line.Points;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];

            float minX = Mathf.Min(p1.X, p2.X);
            float maxX = Mathf.Max(p1.X, p2.X);

            if (x >= minX && x <= maxX)
            {
                if (Mathf.Abs(p2.X - p1.X) < 0.001f)
                    return (p1.Y + p2.Y) / 2f;

                float t = (x - p1.X) / (p2.X - p1.X);
                return Mathf.Lerp(p1.Y, p2.Y, t);
            }
        }

        // Hors limites
        return x < points[0].X ? points[0].Y : points[^1].Y;
    }

    /// <summary>
    /// Put a point on a Line2D if X is inside horizontal bounds of the line.
    /// Only Y will be modified.
    /// </summary>
    /// <param name="point">Point to align.</param>
    /// <param name="line">Line2D.</param>
    /// <returns>True is point can be aligned, False otherwise.
    /// The new point is returned.</returns>
    private static (bool result, Vector2 point) AlignPointOnLine(Vector2 point, Line2D line)
    {
        Vector2[] points = line.Points;

        // Need at least 2 points to form a line
        if (points.Length < 2)
            return (false, new Vector2());

        point.Y = FindYOnLine(line, point.X);

        return (true, point);
    }

    /// <summary>
    /// Return a Rect2 to delimiter the bounds of a polygon2D.
    /// </summary>
    /// <param name="polygon">Polygon defined by an array of 2D coordinates.</param>
    /// <returns>Rect2 with bounds of Polygon</returns>
    static Rect2 GetPolygonBounds(Vector2[] polygon)
    {
        if (polygon == null || polygon.Length == 0)
            return new Rect2();

        Vector2 min = polygon[0];
        Vector2 max = polygon[0];

        for (int i = 1; i < polygon.Length; i++)
        {
            Vector2 p = polygon[i];

            if (p.X < min.X)
                min.X = p.X;
            if (p.Y < min.Y)
                min.Y = p.Y;

            if (p.X > max.X)
                max.X = p.X;
            if (p.Y > max.Y)
                max.Y = p.Y;
        }

        return new Rect2(min, max - min);
    }

    /// <summary>
    /// "BodyEntered" signal Handler. Triggered when player enter in a level junction.
    /// </summary>
    /// <param name="JunctionArea">Area shape of the triggered junction.</param>
    /// <param name="JunctionCollision">Collision shape of the triggered junction.</param>
    /// <param name="TrackName">Contains the name of the gpx file.</param>
    /// <param name="Name">Name of the waypoint (key to retrieve waypoint).</param>
    private void JunctionHandler(
        Area2D JunctionArea,
        CollisionShape2D JunctionCollision,
        string TrackName,
        string Name
    )
    {
        if (Player.Instance == null)
        {
            GD.PushError("JunctionHandler: player is missing!");
            return;
        }

        Player.Instance?.DisplayJunction(TrackName, Name);
    }

    /// <summary>
    /// Retrieve the point colliding with the sol (defined by the CollisionPolygon2D).
    /// </summary>
    /// <param name="x">x-axis</param>
    /// <returns>A valid vector if found, a Zero vector otherwise.</returns>
    public Vector2 GetSolYAtX(float x)
    {
        var spaceState = GetWorld2D().DirectSpaceState;

        // assuming large bound
        Vector2 from = new(x, 0f);
        Vector2 to   = new(x, 100000f);

        var query = PhysicsRayQueryParameters2D.Create(from, to);

        query.CollisionMask = Global.GroundLayer;

        var result = spaceState.IntersectRay(query);

        if (result.Count == 0)
            return Vector2.Zero;

        // First hit position will be the ground
        return (Vector2)result["position"];
    }
}
