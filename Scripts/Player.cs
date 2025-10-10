using System;
using Godot;

public partial class Player : CharacterBody2D
{
    /* Coefficient of speed */
    [Export]
    public int Walk = 1;

    [Export]
    public Sol? sol;

    [Signal]
    public delegate void TrailJunctionChoiceEventHandler();

    [Export]
    public TemplateLevel? level;

    public const float Speed = 100.0f;
    public Godot.Vector2 worldLimit;

    public static Player? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        // the player can climb all
        FloorBlockOnWall = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        Godot.Vector2 velocity = Velocity;

        if (this.Position.X >= worldLimit.X)
        {
            // Don't fall
            Walk = 0;
        }

        // Add the gravity.
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }
        else
        {
            velocity.X = Walk * Speed;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void _on_trail_junction_choice(string gpxFile)
    {
        // Sanity checks
        if (level == null || sol == null)
            return;

        if (sol.GpxFile != gpxFile)
        {
            level.EmitSignal(TemplateLevel.SignalName.TrailJunctionChoiceDone, gpxFile);
        }
        // TODO comment faire passer à travers la collison de la junction?
        SetCollisionLayerValue(5, true);
        SetCollisionMaskValue(5, true);
        Walk = 1;
    }

    /**
      Stop player and display the junction panel.

      @param TrackName Contains the name of the gpx file.
      @param Coord     Coordinate of the triggered waypoint.
    */
    internal void DisplayJunction(string Trace, Vector2 Coord)
    {
        if (level == null)
            return;
        Walk = 0;
        SetCollisionLayerValue(5, false);
        SetCollisionMaskValue(5, false);
        level.JunctionChoice(Trace, Coord);
    }
}
