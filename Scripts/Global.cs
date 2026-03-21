namespace Randonneur
{
    class Global
    {
        public const string DefautlMapDirectory = "res://data/Map1/";

        // Tag for waypoint metadata
        public const string MetaWaypointName = "WaypointName";

        // Name for nodes which describes the trail in the map
        public const string TrailLineName = "TrailLine2D";

        /// <summary>
        /// Collisions layer.
        /// </summary>
        public const int PlayerLayer = 1;

        public const int GroundLayer = 2;

        public const int NPCLayer = 3;

        public const int MapLayer = 4;

        public const int TrailJunctionLayer = 5;

        /// <summary>
        /// Player properties.
        /// </summary>
        public const int PlayerSpeed = 3;

        public const string PlayerGroup = "player";

        public const float JunctionCollisionShapeSize = 20.0f;

        public const int PictureGameMaxStars = 4;

        public const int PictureGameMaxNote = 10;

        // Zindex
        // TODO define layers
        public const int ZIndexUILayer1 = 1;

        // Time
        public const int MS_IN_SECOND = 1000;

        // Errors
        public const int SANITY_CHECK_ERROR = -1000;
    }
}
