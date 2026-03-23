using System.Security.Cryptography.X509Certificates;
using Godot;
using Godot.NativeInterop;
using Randonneur.Scripts;

namespace Randonneur
{
    public partial class HikerNpc : CharacterBody2D
    {
        /// <summary>
        /// Speed when NPC is inside a scene.
        /// </summary>
        public const float SpeedCoefficient = 100.0f;

        /// <summary>
        /// Speed when NPC is not in a scene.
        /// Number of pixel traveled horizontaly during one second.
        /// (slope is not considered)
        /// </summary>
        public const float PixelBySecond = 200.0f;

        /// <summary>
        /// Remaining of pixels before reaching the next route step.
        /// </summary>
        private float _distanceToNextSteps = 0.0f;

        /// <summary>
        /// Ref to the collision shape;
        /// </summary>
        private CollisionShape2D? _hikerCollisionShape;

        /// <summary>
        /// Access to waypoints.
        /// </summary>
        private Waypoints? _waypoints;

        private float _spriteHeight;

        /// <summary>
        /// Link to player instance.
        /// </summary>
        private Player? _player;

        /// <summary>
        /// Link to all the Scenes (trails) with gpx file name as the key and the level
        /// as the value.
        /// @see SceneManager
        /// </summary>
        private Dictionary<string, Level> _trailScenes = [];

        /// <summary>
        /// The route the NPC follows. Each string is a waypoint.
        /// </summary>
        private string[] _route { get; set; } = [""];

        private uint _nextWaypointIndex { get; set; } = 0;

        /// <summary>
        /// Walking speed, negative: go to the left, positive: go to the right.
        /// </summary>
        public int HikerSpeed { get; set; } = Global.PlayerSpeed;

        /// <summary>
        /// Flag to enable NPC moves.
        /// </summary>
        public bool Move { get; set; } = false;

        /// <summary>
        /// Current trail.
        /// </summary>
        public Level? CurrentLevel { get; set; }

        /// <summary>
        /// Flag up when this NPC is in the same scene as player.
        /// </summary>
        public bool InPlayerLevel { get; private set; } = false;

        /// <summary>
        /// Stores the last waypoint the NPC reached.
        /// </summary>
        public Waypoint? CurrentWaypoint;

        public override void _Ready()
        {
            // the NPC can climb all
            FloorBlockOnWall = false;
        }

        /// <summary>
        /// Initialize the NPC path and put it at start.
        /// </summary>
        /// <param name="trailScenes">Available trail scenes</param>
        /// <param name="traceName"></param>
        /// <param name="route">Route is an array of waypoint names (min 2 waypoints).</param>
        public void Init(Dictionary<string, Level> trailScenes, string traceName, string[] route)
        {
            // get current player level
            if (Player.Instance == null || Player.Instance.Level == null)
            {
                GD.PushError("Init(): a player instance is mandatory.");
                return;
            }
            _player = Player.Instance;

            if (route.Length < 2)
            {
                GD.PushError("Init(): Invalid route length.");
                return;
            }

            if (trailScenes.Count < 1)
            {
                GD.PushError("Init(): Invalid trail scenes length.");
                return;
            }

            _trailScenes = trailScenes;
            CurrentLevel = _trailScenes[Global.DefautlMapDirectory + traceName];
            _route = route;
            _nextWaypointIndex = 1;
            _hikerCollisionShape = GetNode<CollisionShape2D>("Collision");
            _spriteHeight = _hikerCollisionShape.Shape.GetRect().Size.Y;
            _waypoints = (Waypoints)Waypoints.Instance;
            CurrentWaypoint = _waypoints.GetWaypoint(_route[0]);
            if (CurrentWaypoint != null)
                MoveTo(CurrentWaypoint.LevelCoord[traceName], traceName);
            ChangeLevel(traceName, _nextWaypointIndex);
        }

        /// <summary>
        /// Change level by positioning NPC to the waypoint on the next trace.
        /// Use without parameters to reach the next destination on the route
        /// </summary>
        /// <param name="destTraceName">Optional - Destination trace.</param>
        /// <param name="routeIndex">Optional - Index of the next waypoint name.</param>
        private void ChangeLevel(string destTraceName = "", uint routeIndex = 0)
        {
            if (_waypoints == null || CurrentLevel == null)
            {
                GD.PushError("ChangeLevel(): sanity check failed");
                return;
            }

            // if level is not specified, get the next
            if (destTraceName == "" && CurrentWaypoint != null)
            {
                // loop indefinitively
                if (_nextWaypointIndex >= _route.Length)
                {
                    _nextWaypointIndex = 1;
                    CurrentWaypoint = _waypoints.GetWaypoint(_route[0])!;
                }

                ConnectedWaypoint? destWaypoint = _waypoints.GetConnectedWaypoint(
                    CurrentWaypoint.Name,
                    _route[_nextWaypointIndex]
                );

                if (destWaypoint is null)
                {
                    GD.PushError("ChangeLevel(): failed to retrieve destination");
                    return;
                }

                destTraceName = destWaypoint.TraceName;
                routeIndex = _nextWaypointIndex;

                // if NPC is at start of his trip and now the destination
                // is known, it's possible to move the NPC.
                if (_nextWaypointIndex == 1)
                {
                    MoveTo(CurrentWaypoint.LevelCoord[destTraceName], destTraceName);
                }
            }

            // another sanity checks ...
            Waypoint? targetWaypoint = _waypoints.GetWaypoint(_route[routeIndex]);
            if (targetWaypoint == null)
            {
                GD.PushError($"Failed to get waypoint {_route[routeIndex]} for hiker NPC");
                return;
            }

            if (CurrentWaypoint == null)
            {
                GD.PushError($"Failed to retrieve current waypoint");
                return;
            }

            // get direction
            HikerSpeed = Global.PlayerSpeed;
            if (
                CurrentWaypoint.LevelCoord[destTraceName].X
                > targetWaypoint.LevelCoord[destTraceName].X
            )
            {
                HikerSpeed = -Global.PlayerSpeed;
            }

            // settings for next level
            Move = true;
            string currentTraceName = CurrentLevel.TraceName;
            CurrentLevel = _trailScenes[Global.DefautlMapDirectory + destTraceName];
            _distanceToNextSteps = GetDistanceTo(routeIndex - 1, routeIndex);
            _nextWaypointIndex++;

            // level change?
            if (currentTraceName != destTraceName)
            {
                // put NPC at start
                MoveTo(CurrentWaypoint.LevelCoord[destTraceName], destTraceName);

                // In any case, it's no more handled by godot.
                RemoveFromLevel((TemplateLevel)GetParent());
            }
            CurrentWaypoint = targetWaypoint;

            GD.Print(
                $"Hiker NPC goes to {targetWaypoint.Name} in {destTraceName} and distance: {_distanceToNextSteps}"
            );
        }

        /// <summary>
        /// Retrieve the distance (in pixels) between the a route step and the next one.
        /// </summary>
        /// <param name="startIndex">Index of the start index.</param>
        /// <returns></returns>
        private float GetDistanceTo(uint startIndex, uint endIndex)
        {
            if (_waypoints == null || _route == null || startIndex < 0 || endIndex >= _route.Length)
            {
                GD.PushError("GetDistanceBetweenSteps(): sanity check failed.");
                return 0.0f;
            }

            float distance = 0.0f;
            string startWaypointName = _route[startIndex];

            Waypoint? startWaypoint = _waypoints.GetWaypoint(startWaypointName);
            ConnectedWaypoint? destWaypoint = _waypoints.GetConnectedWaypoint(
                _route[startIndex],
                _route[endIndex]
            );

            if (destWaypoint != null && startWaypoint != null)
            {
                string traceName = destWaypoint.TraceName;
                Waypoint? endWaypoint = destWaypoint.Waypoint;
                if (endWaypoint != null)
                {
                    distance = Mathf.Abs(
                        startWaypoint.LevelCoord[traceName].X - endWaypoint.LevelCoord[traceName].X
                    );
                }
            }

            return distance;
        }

        public override void _PhysicsProcess(double delta)
        {
            UpdatePosition(delta);
            MoveAndSlide();
        }

        /// <summary>
        /// Update Hiker graphical position, it must be done manually if the node is not
        /// in a scene, otherwise Godot engine will handle it.
        /// </summary>
        /// <param name="delta">Time between two call (ms).</param>
        /// <param name="manual">Called manually or by godot engine.</param>
        public void UpdatePosition(double delta, bool manual = false)
        {
            if (CurrentLevel == null)
            {
                return;
            }
            Godot.Vector2 velocity = Velocity;

            // handle by godot engine
            if (manual == false)
            {
                // Add the gravity.
                if (!IsOnFloor())
                {
                    velocity += GetGravity() * (float)delta;
                }
                else
                {
                    velocity.X = Move ? HikerSpeed * SpeedCoefficient : 0;
                }

                _distanceToNextSteps -= ((float)(delta / Global.MS_IN_SECOND) * velocity.X);

                //  GD.Print($"HikerSPeed {HikerSpeed} distance {_distanceToNextSteps} move {Move}");

                // check with -1.0f because hiker starts at position 0.0f, unlike player that starts at front of
                // waypoint (to avoid the waypoint's collision box).
                if (
                    (HikerSpeed > 0 && Position.X > CurrentLevel.LimitX)
                    || (HikerSpeed < 0 && Position.X <= -1.0f)
                )
                {
                    // Don't fall
                    Move = false;
                    ChangeLevel();
                }
            }
            else
            {
                if (Move)
                {
                    float deltaX = (float)(delta / Global.MS_IN_SECOND) * PixelBySecond;
                    _distanceToNextSteps -= deltaX;
                }
            }

            // destination reached
            if (_distanceToNextSteps <= 0)
            {
                Move = false;
                ChangeLevel();
            }

            Velocity = velocity;
        }

        /// <summary>
        /// Add an hiker to a level.
        /// </summary>
        public void AddToLevel(TemplateLevel level)
        {
            if (InPlayerLevel == false)
            {
                _ = CallDeferred("AddToLevelAsync", level);
            }
        }

        /// <summary>
        /// Add an hiker to a level.
        /// </summary>
        private void AddToLevelAsync(TemplateLevel level)
        {
            if (GetParent() != null)
                return;

            level?.AddChild(this);
            InPlayerLevel = true;

            // update position to match the ground
            if (CurrentLevel != null && CurrentLevel.SolBody != null)
            {
                Vector2 position = CurrentLevel.SolBody.GetSolYAtX(
                    CurrentLevel.LimitX - _distanceToNextSteps
                );
                Position = new Vector2(position.X, position.Y - (_spriteHeight / 2 * Scale.Y));
            }
        }

        /// <summary>
        /// Remove an hiker from a level.
        /// </summary>
        public void RemoveFromLevel(TemplateLevel level)
        {
            if (InPlayerLevel == true)
            {
                _ = CallDeferred("RemoveFromLevelAsync", level);
            }
        }

        /// <summary>
        /// Remove an hiker to a level.
        /// </summary>
        private void RemoveFromLevelAsync(TemplateLevel level)
        {
            if (GetParent() == null)
                return;

            level?.RemoveChild(this);
            InPlayerLevel = false;
        }

        /// <summary>
        /// Move the hiker to a specific position. Y will be adapted to make the hiker
        /// bottom to the ground.
        /// </summary>
        /// <param name="position">Level's coordinates.</param>
        /// <param name="traceName">Level's trace name.</param>
        public void MoveTo(Vector2 position, string traceName)
        {
            // get current player level
            if (_player == null || _player.Level == null)
                return;

            TemplateLevel currentPlayerLevel = _player.Level;

            bool sameScene = currentPlayerLevel.CurrentTraceName == traceName;
            if (sameScene)
            {
                GD.Print($"Hiker and PLAYER are now in the same scene");
                AddToLevel(currentPlayerLevel);

                Position = new Vector2(position.X, position.Y - (_spriteHeight / 2 * Scale.Y));
            }

            GD.Print($"Hiker Moved to {Position}");

            // hike, baby hike
            Move = true;
        }
    } // HikerNpc
}
