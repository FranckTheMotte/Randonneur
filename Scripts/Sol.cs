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

    public Gpx? CurrentTrack;

    // Godot group of all wyapoints
    private const string _WaypointsGroup = "Waypoints";

    /// <summary>
    /// The maximum distance from the origin in the world the player can go.
    /// </summary>
    [Export]
    public float WorldLimitX { get; set; } = 10000.0f;

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

    /// <summary>
    /// Generate a solid ground from a gpx file.
    /// <param name="gpxFile">Full godot path to the gpx file.</param>
    /// </summary>
    public void generateGround(string gpxFile)
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
            Polygon2D sol = GetNode<Polygon2D>("ProfilElevation");

            /* Generate a profil from a gpx file */
            CurrentTrack = new Gpx();
            CurrentTrack.Load(gpxFile);

            if (CurrentTrack.TrackPoints == null)
            {
                GD.PushWarning(
                    $"{nameof(generateGround)}: no track points in current gpx file ${gpxFile}"
                );
                watch.Stop();
                return;
            }
            /* Add 2 points in order to display a solid ground */
            Vector2[] ground = new Vector2[CurrentTrack.TrackPoints.Length + 2];

            string traceName = Path.GetFileName(gpxFile);

            int solLength = CurrentTrack.TrackPoints.Length;
            for (int i = 0; i < solLength; i++)
            {
                // Put the display coord
                ground[i] = CurrentTrack.TrackPoints[i].Elevation;
                // Display a waypoint
                Waypoint? waypoint = CurrentTrack.TrackPoints[i].Waypoint;
                if (waypoint != null)
                {
                    // TODO: here only to display a graphic object for junction
                    waypoint.LevelCoord[traceName] = ground[i];
                    TrailJunction junction = new() { TraceName = traceName, Waypoint = waypoint };
                    _trailJunctions.Add(junction);

                    // Level order is the same as ground point index
                    Waypoint? TargetWaypoint = Waypoints.Instance.GetWaypoint(waypoint.Name);
                    if (TargetWaypoint != null)
                    {
                        TargetWaypoint.LevelOrder[traceName] = i;
                    }

                    Area2D junctionArea = new() { Position = ground[i], Name = traceName };
                    // Use the junction collision layer
                    junctionArea.SetCollisionLayerValue(1, false);
                    junctionArea.SetCollisionLayerValue(Global.SolJunctionLayer, true);
                    junctionArea.SetCollisionMaskValue(1, false);
                    junctionArea.SetCollisionMaskValue(Global.SolJunctionLayer, true);
                    RectangleShape2D rectangle = new() { Size = new Vector2(20, 20) };
                    CollisionShape2D junctionCollision = new() { Shape = rectangle };
                    junctionCollision.AddToGroup(_WaypointsGroup);
                    junctionArea.BodyEntered += delegate
                    {
                        JunctionHandler(junctionArea, junctionCollision, traceName, waypoint.Name);
                    };
                    junctionArea.AddChild(junctionCollision);
                    AddChild(junctionArea);
                }
            }

            ground[solLength].X = CurrentTrack.MaxX;
            ground[solLength].Y = Gpx.PixelElevationMax;
            ground[solLength + 1].X = 0.00f;
            ground[solLength + 1].Y = Gpx.PixelElevationMax;

            sol.Polygon = ground;
            solCollision.Polygon = sol.Polygon;

            /* player limit */
            WorldLimitX = CurrentTrack.MaxX;
            GD.Print($"world limit X : {WorldLimitX}");
        }
        /* TODO put default value if no Gpx is provided */
        watch.Stop();
        Print($"Ground creation Time: {watch.ElapsedMilliseconds} ms");
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
            return;

        /* When reparent the player to another level, it's put on 0,0, before moving to
        his real position.
        A unexpected collision can occurs, here we just test if the distance between
        the two collisionShapes is coherent.
        TODO: find a better fix */
        if (JunctionArea.Position.DistanceTo(Player.Instance.Position) > 30.0f)
        {
            GD.PushWarning("Junction is too far from player, skip.");
            return;
        }

        /* Reenable collisions of Waypoints group.
           Maybe there is a better method but as the collision can occurs several
           times when player go accross the shape, it is:
           - 1) disable when triggered
           - 2) reenable when next trigger occurs
        */
        foreach (
            CollisionShape2D collision in GetTree()
                .GetNodesInGroup(_WaypointsGroup)
                .Cast<CollisionShape2D>()
        )
        {
            collision.SetDeferred("disabled", false);
        }

        Player.Instance?.DisplayJunction(TrackName, Name);
        // avoid useless collisions.
        JunctionCollision.SetDeferred("disabled", true);
    }
}
