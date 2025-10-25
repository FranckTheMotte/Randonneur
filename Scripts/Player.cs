using System;
using Godot;
using Randonneur;

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

    public Waypoint? CurrentWaypoint = null;

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

    /// <summary>
    /// Called when the player has chosen a new trail.
    /// </summary>
    /// <param name="waypointName">The name of the waypoint chosen.</param>
    /// <remarks>
    /// This method is called when the player has chosen a new trail.
    /// It will enable the collision layer and mask of the junction
    /// and move the player forward (or backward if the player is moving
    /// backwards).
    /// </remarks>
    private void _on_trail_junction_choice(string waypointName)
    {
        // Sanity checks
        if (level == null || sol == null || CurrentWaypoint == null)
            return;

        // impossible to move on the same waypoint where the player is
        if (CurrentWaypoint.Name != waypointName)
        {
            level.EmitSignal(TemplateLevel.SignalName.TrailJunctionChoiceDone, waypointName);
        }
        // TODO comment faire passer à travers la collison de la junction?
        SetCollisionLayerValue(Global.SolJunctionLayer, true);
        SetCollisionMaskValue(Global.SolJunctionLayer, true);

        // move forward (default)
        Walk = 1;
        Waypoint? TargetWaypoint = Waypoints.Instance.GetWaypoint(waypointName);
        //CurrentWaypoint = Waypoints.Instance.GetWaypoint(CurrentWaypoint.Name);
        if (TargetWaypoint != null && CurrentWaypoint != null)
        {
            string traceName = TargetWaypoint.TraceName;
            // order of waypoints in the target trace determine the direction
            if (TargetWaypoint.LevelOrder[traceName] < CurrentWaypoint.LevelOrder[traceName])
            {
                Walk = -1;
            }
        }
        CurrentWaypoint = null;
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
        SetCollisionLayerValue(Global.SolJunctionLayer, false);
        SetCollisionMaskValue(Global.SolJunctionLayer, false);
        level.JunctionChoice(Trace, Coord);
    }
}
