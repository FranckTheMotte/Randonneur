using Godot;
using Randonneur.Scripts;

namespace Randonneur
{
    public partial class HikerNpc : CharacterBody2D
    {
        /// <summary>
        /// consts.
        /// </summary>
        public const float SpeedCoefficient = 100.0f;

        /// <summary>
        /// Route of the NPC.
        /// </summary>
        private List<Waypoint> _routeWaypoints = [];

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

        public override void _Ready()
        {
            // the NPC can climb all
            FloorBlockOnWall = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Will be only called when this NPC is added to the current scene player.
        /// </remarks>
        /// <param name="delta"></param>
        public override void _PhysicsProcess(double delta)
        {
            UpdatePosition(delta);
            MoveAndSlide();
        }

        public void UpdatePosition(double delta)
        {
            if (CurrentLevel == null)
            {
                return;
            }
            Godot.Vector2 velocity = Velocity;

            // Add the gravity.
            if (!IsOnFloor())
            {
                velocity += GetGravity() * (float)delta;
            }

            if (this.Position.X >= CurrentLevel.LimitX || this.Position.X <= 0)
            {
                // Don't fall
                Move = false;
            }

            // Add the gravity.
            if (IsOnFloor())
            {
                velocity.X = Move ? HikerSpeed * SpeedCoefficient : 0;
            }

            Velocity = velocity;
        }

        /// <summary>
        /// Add an hiker to a level.
        /// </summary>
        public void AddToLevel(TemplateLevel level)
        {
            _ = CallDeferred("AddToLevelAsync", level);
        }

        /// <summary>
        /// Add an hiker to a level.
        /// </summary>
        private void AddToLevelAsync(TemplateLevel level)
        {
            level?.AddChild(this);
            InPlayerLevel = true;
        }

        /// <summary>
        /// Remove an hiker from a level.
        /// </summary>
        public void RemoveFromLevel(TemplateLevel level)
        {
            _ = CallDeferred("RemoveFromLevelAsync", level);
        }

        /// <summary>
        /// Remove an hiker to a level.
        /// </summary>
        private void RemoveFromLevelAsync(TemplateLevel level)
        {
            level?.RemoveChild(this);
            InPlayerLevel = false;
        }
    } // HikerNpc
}
