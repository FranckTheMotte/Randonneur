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
    public Level1? level;

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
        if (level == null)
            return;

        level.EmitSignal(Level1.SignalName.TrailJunctionChoiceDone, gpxFile);
        this.Position = new Godot.Vector2(this.Position.X + 1, this.Position.Y);
        Walk = 1;
    }

    internal void DisplayJunction(GpxWaypoint Waypoint)
    {
        if (level == null)
            return;
        Walk = 0;
        /* TODO
          - 0 is an hardcoded value just for a default status
          - rework this part to display the trail junction.
            Trail junction was based on a panel. It will be done with
            interactive world map.
        */
        level.EmitSignal(Level1.SignalName.TrailJunctionChoice, 0);
    }
}
