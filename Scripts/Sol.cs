using System;
using System.Collections.Generic;
using Godot;
using static Godot.GD;

public partial class Sol : StaticBody2D
{
    private List<Vector2> TrailJunctions = new List<Vector2>();

    public Gpx? CurrentTrack;

    [Export]
    Player? Player;

    [Export]
    private string? GpxFile;

    // Generate a 2D polygon (list of Vector2) to test on a customize ground
    private Vector2[] generateGround(int length)
    {
        Vector2[] result = new Vector2[length];
        int x = -50;
        int y = 200;

        for (int i = 0; i < length - 2; ++i)
        {
            result[i].X = x;
            result[i].Y = y;
            x += 10;
            y += i % 2 == 0 ? 5 : -5;
        }
        result[length - 2].X = x;
        result[length - 2].Y = 300;

        result[length - 1].X = -50;
        result[length - 1].Y = 300;

        return result;
    }

    public override void _Draw()
    {
        foreach (Vector2 trailJunction in TrailJunctions)
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
            GD.PushWarning($"${nameof(generateGround)}: sanity checks failed");
            return;
        }

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
                    $"${nameof(generateGround)}: no track points in current gpx file ${gpxFile}"
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
                if (CurrentTrack.m_trackPoints[i].trailJunctionIndex != -1)
                {
                    TrailJunctions.Add(new Vector2(ground[i].X, ground[i].Y - 50));
                }
            }

            ground[solLength].X = CurrentTrack.maxX;
            ground[solLength].Y = Gpx.PIXEL_ELEVATION_MAX;
            ground[solLength + 1].X = 0.00f;
            ground[solLength + 1].Y = Gpx.PIXEL_ELEVATION_MAX;

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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (GpxFile != null)
            generateGround(GpxFile);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
