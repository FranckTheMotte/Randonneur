namespace Randonneur
{
    class Global
    {
        public const string DefautlMapDirectory = "res://data/Map1/";

        // Tag for waypoint metadata
        public const string MetaWaypointName = "WaypointName";

        // Name for nodes which describes the trail in the map
        public const string TrailLineName = "TrailLine2D";

        public const int MapLayer = 4;

        public const int SolJunctionLayer = 5;

        public const int PlayerSpeed = 3;

        public const string PlayerGroup = "player";

        public const float JunctionCollisionShapeSize = 20.0f;

        // Errors
        public const int SANITY_CHECK_ERROR = -1000;
    }
}
