using System;
using Godot;
using Randonneur;

public partial class Player : CharacterBody2D
{
    /// <summary>
    /// Flag to enable player moves.
    /// </summary>
    public bool Move = false;

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
            Move = false;
        }

        // Add the gravity.
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }
        else
        {
            velocity.X = Move ? Walk * Speed : 0;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    /// <summary>
    /// Called when the player has chosen a new trail.
    /// </summary>
    /// <param name="destWaypointName">The name of the waypoint chosen.</param>
    /// <remarks>
    /// This method is called when the player has chosen a new trail.
    /// It will enable the collision layer and mask of the junction
    /// and move the player forward (or backward if the player is moving
    /// backwards).
    /// </remarks>
    private void _on_trail_junction_choice(string destWaypointName)
    {
        // Sanity checks
        if (level == null || sol == null || CurrentWaypoint == null)
            return;

        // impossible to move on the same waypoint where the player is
        if (CurrentWaypoint.Name != destWaypointName)
        {
            level.TrailJunctionChoiceDone(destWaypointName);
        }
        // TODO comment faire passer à travers la collison de la junction?
        SetCollisionLayerValue(Global.SolJunctionLayer, true);
        SetCollisionMaskValue(Global.SolJunctionLayer, true);

        // move forward (default)
        Move = true;
        Walk = 1;

        Waypoints waypoints = (Waypoints)Waypoints.Instance;
        if (
            waypoints.Links != null
            && waypoints.Links.TryGetValue(CurrentWaypoint.Name, out WaypointsLinks? links)
        )
        {
            string traceName = links.ConnectedWaypoints[destWaypointName].TraceName;
            Waypoint? TargetWaypoint = waypoints.GetWaypoint(destWaypointName);
            CurrentWaypoint = waypoints.GetWaypoint(CurrentWaypoint.Name);
            if (TargetWaypoint != null && CurrentWaypoint != null)
            {
                // order of waypoints in the target trace determine the direction
                if (TargetWaypoint.LevelOrder[traceName] < CurrentWaypoint.LevelOrder[traceName])
                {
                    Walk = -1;
                }
            }
        }
        CurrentWaypoint = null;
    }

    ///
    /// <summary>
    /// Stop player and display the junction panel.
    /// </summary>
    /// <param name="TrackName">Contains the name of the gpx file.</param>
    ///<param name="waypointName">Waypoint name.</param>
    internal void DisplayJunction(string trace, string waypointName)
    {
        if (level == null)
            return;
        Move = false;
        SetCollisionLayerValue(Global.SolJunctionLayer, false);
        SetCollisionMaskValue(Global.SolJunctionLayer, false);
        level.JunctionChoice(trace, waypointName);
    }

    /// <summary>
    /// Move the player to a new position.
    /// </summary>
    /// <param name="position"></param>
    public void MoveTo(Vector2 position)
    {
        Vector2 newPosition = new();
        /* default player start position */
        CollisionShape2D playerCollisionShape = GetNode<CollisionShape2D>("Collision");

        /*
            X: little shift player from the starting waypoint
            Y: Align player position with half of the collision shape size (don't forget the player rescaling)
        */
        newPosition = new Vector2(
            position.X + Walk * 30.0f,
            position.Y - (playerCollisionShape.Shape.GetRect().Size.Y / 2 * Scale.Y)
        );

        Position = newPosition;
    }
}
