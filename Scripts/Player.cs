using System;
using Godot;
using Randonneur;

public partial class Player : CharacterBody2D
{
    /// <summary>
    /// Flag to enable player moves.
    /// </summary>
    public bool Move { get; set; } = false;

    /// <summary>
    /// Walking speed, negative: go to the left, positive: go to the right.
    /// </summary>
    [Export]
    public int HikerSpeed { get; set; } = Global.PlayerSpeed;

    [Signal]
    public delegate void TrailJunctionChoiceEventHandler();

    // ref to the current level
    public TemplateLevel? Level { get; set; }

    public const float SpeedCoefficient = 100.0f;

    // Maximum limit of the level in X
    public Godot.Vector2 LevelLimitX { get; set; }

    public Waypoint? CurrentWaypoint { get; set; } = null;

    /// <summary>
    /// Traveled distance since last junction.
    /// </summary>
    private float _traveledDistance = 0.0f;

    /// <summary>
    /// Reference to debug actions panel
    /// </summary>
    private DebugTools? _actionsPanel = null;

    /// <summary>
    /// Force next waypoint to act as a junction.
    /// Introduce to force the map to display when exiting from a minigame.
    /// </summary>
    public bool ForceJunction { get; set; } = false;

    private static Player? _instance;
    public static Player? Instance { get; private set; } = _instance;

    /// <summary>
    /// Ref to the player sprite.
    /// </summary>
    private AnimatedSprite2D? PlayerSprite;

    /// <summary>
    /// Last visited waypoint.
    /// </summary>
    public String LastWaypointName { get; set; } = "NULL";

    /// <summary>
    /// Define the direction of movement.
    /// </summary>
    internal enum Direction
    {
        Forward,
        Backward,
    }

    /// <summary>
    /// Grumbling stats.
    /// </summary>
    public double CurrentGrumblingLevel { get; set; } = 100;
    public double MaxGrumblingLevel { get; private set; } = 100;

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public override void _Ready()
    {
        // Block player ressource free when scène change
        ProcessMode = ProcessModeEnum.Always;

        Instance = this;

        // the player can climb all
        FloorBlockOnWall = false;

        // ref to the sprite
        PlayerSprite = GetNode<AnimatedSprite2D>("TheSprite");

        // group for other item interaction
        AddToGroup(Global.PlayerGroup);
    }

    public override void _PhysicsProcess(double delta)
    {
        Godot.Vector2 velocity = Velocity;

        if (this.Position.X >= LevelLimitX.X || this.Position.X <= 0)
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
            velocity.X = Move ? HikerSpeed * SpeedCoefficient : 0;
            if (Move)
            {
                _traveledDistance += Math.Abs(HikerSpeed);
            }
        }

        // re-activate collision with junction when player is far enough
        // from last junction
        if (_traveledDistance > Global.JunctionCollisionShapeSize || ForceJunction)
        {
            SetCollisionLayerValue(Global.TrailJunctionLayer, true);
            SetCollisionMaskValue(Global.TrailJunctionLayer, true);
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
        if (Level == null || CurrentWaypoint == null)
            return;

        Waypoints waypoints = (Waypoints)Waypoints.Instance;
        Waypoint? targetWaypoint = waypoints.GetWaypoint(destWaypointName);

        if (targetWaypoint == null)
        {
            GD.PushWarning(
                $"Trail junction choice: destination waypoint not found {destWaypointName}"
            );
            return;
        }

        // impossible to move on the same waypoint where the player is
        if (CurrentWaypoint.Name != destWaypointName)
        {
            Level.TrailJunctionChoiceDone(destWaypointName);
        }

        // move forward (default)
        Move = true;
        Go(Direction.Forward);
        CurrentWaypoint.PlayerDirection = Global.PlayerSpeed;

        if (
            waypoints.Links != null
            && waypoints.Links.TryGetValue(CurrentWaypoint.Name, out WaypointsLinks? links)
        )
        {
            string traceName = links.ConnectedWaypoints[destWaypointName].TraceName;
            CurrentWaypoint = waypoints.GetWaypoint(CurrentWaypoint.Name);
            if (targetWaypoint != null && CurrentWaypoint != null)
            {
                // order of waypoints in the target trace determine the direction
                if (targetWaypoint.LevelOrder[traceName] < CurrentWaypoint.LevelOrder[traceName])
                {
                    Go(Direction.Backward);
                    CurrentWaypoint.PlayerDirection = -Global.PlayerSpeed;
                }
            }
        }
        CurrentWaypoint = null;
    }

    /// <summary>
    /// Move the player in the specified direction (sprite orientation and walk).
    /// </summary>
    /// <param name="direction">Direction to move the player.</param>
    internal void Go(Direction direction)
    {
        if (PlayerSprite == null)
        {
            GD.PushError("Sprite must exist.");
            return;
        }

        switch (direction)
        {
            case Direction.Forward:
                PlayerSprite.FlipH = false;
                HikerSpeed = Global.PlayerSpeed;
                break;
            case Direction.Backward:
                PlayerSprite.FlipH = true;
                HikerSpeed = -Global.PlayerSpeed;
                break;
        }

        // Update actions
        _actionsPanel?.UpdateHikerSpeed(HikerSpeed);
    }

    ///
    /// <summary>
    /// Stop player and display the junction panel.
    /// </summary>
    /// <param name="trace">Contains the name of the gpx file.</param>
    ///<param name="waypointName">Waypoint name.</param>
    internal void DisplayJunction(string trace, string waypointName)
    {
        if (Level == null)
            return;

        // Junction is not triggered if the player comes from this one
        // Added to prevent unexpected collision detection when leaving
        // the last waypoint.
        if (waypointName != LastWaypointName)
        {
            Move = false;
            SetCollisionLayerValue(Global.TrailJunctionLayer, false);
            SetCollisionMaskValue(Global.TrailJunctionLayer, false);
            Level.JunctionChoice(trace, waypointName);
            _traveledDistance = 0;
            LastWaypointName = waypointName;
        }
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
            position.X + (HikerSpeed > 0 ? 1 : -1) * 30.0f,
            position.Y - (playerCollisionShape.Shape.GetRect().Size.Y / 2 * Scale.Y)
        );

        Position = newPosition;
    }

    /// <summary>
    /// Give access to actions panel.
    /// </summary>
    /// <param name="ActionsPanel">Canvas to control actions.</param>
    public void SetActionsPanel(DebugTools ActionsPanel)
    {
        _actionsPanel = ActionsPanel;
    }

    /// <summary>
    /// Reactivate player in the current level.
    /// </summary>
    public void BackToLevel()
    {
        Move = true;
        // Re-activated the player to let collide with next waypoint collision shape
        ForceJunction = true;
        // hide possible minigame
        Level?.MGVisible(false);
    }
}
