using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using XmlGpx;
using static Godot.GD;

public partial class Sol : StaticBody2D
{
    private List<Vector2> _trailJunctions = [];

    public Gpx? CurrentTrack;

    [Export]
    Player? Player;

    [Export]
    public string? GpxFile;

    public override void _Draw()
    {
        foreach (Vector2 trailJunction in _trailJunctions)
        {
            DrawCircle(trailJunction, 10.0f, Colors.Blue);
        }
        base._Draw();
    }

    public void generateGround(string gpxFile)
    {
        // Sanity checks
        if (Player == null)
        {
            GD.PushWarning($"{nameof(generateGround)}: sanity checks failed");
            return;
        }

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

            if (CurrentTrack.m_trackPoints == null)
            {
                GD.PushWarning(
                    $"{nameof(generateGround)}: no track points in current gpx file ${gpxFile}"
                );
                watch.Stop();
                return;
            }
            /* Add 2 points in order to display a solid ground */
            Vector2[] ground = new Vector2[CurrentTrack.m_trackPoints.Length + 2];

            int solLength = CurrentTrack.m_trackPoints.Length;
            for (int i = 0; i < solLength; i++)
            {
                ground[i] = CurrentTrack.m_trackPoints[i].elevation;
                var Waypoint = CurrentTrack.m_trackPoints[i].Waypoint;
                if (Waypoint != null)
                {
                    // TODO: here only to display a graphic object for junction
                    Vector2 junctionPosition = ground[i];
                    _trailJunctions.Add(junctionPosition);

                    Area2D junctionArea = new() { Position = ground[i] };
                    junctionArea.Name = Path.GetFileName(gpxFile);
                    junctionArea.BodyEntered += delegate
                    {
                        JunctionHandler(Path.GetFileName(gpxFile), Waypoint.Coord);
                    };
                    // Use the junction collision layer
                    junctionArea.SetCollisionLayerValue(1, false);
                    junctionArea.SetCollisionLayerValue(5, true);
                    junctionArea.SetCollisionMaskValue(1, false);
                    junctionArea.SetCollisionMaskValue(5, true);
                    RectangleShape2D rectangle = new() { Size = new Vector2(20, 20) };
                    CollisionShape2D junctionCollision = new() { Shape = rectangle };
                    junctionArea.AddChild(junctionCollision);
                    AddChild(junctionArea);
                }
            }

            ground[solLength].X = CurrentTrack.maxX;
            ground[solLength].Y = Gpx.PixelElevationMax;
            ground[solLength + 1].X = 0.00f;
            ground[solLength + 1].Y = Gpx.PixelElevationMax;

            sol.Polygon = ground;
            solCollision.Polygon = sol.Polygon;

            /* player start position */
            Vector2 position = Player.Position;
            CollisionShape2D playerCollisionShape = Player.GetNode<CollisionShape2D>("Collision");

            position.X = 0.00f;
            /* Align player position with half of the collision shape size (don't forget the player rescaling) */
            position.Y =
                ground[0].Y - (playerCollisionShape.Shape.GetRect().Size.Y / 2 * Player.Scale.Y);
            Player.Position = position;

            /* player limit */
            Player.worldLimit = new Vector2(CurrentTrack.maxX, 0);
            GD.Print($"world limit X : {Player.worldLimit.X}");
        }
        /* TODO put default value if no Gpx is provided */
        watch.Stop();
        Print($"Ground creation Time: {watch.ElapsedMilliseconds} ms");
    }

    /**
      "BodyEntered" signal Handler.

      @param TrackName Contains the name of the gpx file.
      @param Coord     Coordinate of the triggered waypoint.
    */
    private void JunctionHandler(string TrackName, Vector2 Coord)
    {
        GpxWaypoint? Waypoint = CurrentTrack?.Waypoints.GetWaypoint(Coord);
        if (Waypoint is not null)
        {
            Player?.DisplayJunction(TrackName, Coord);
        }
    }
}
