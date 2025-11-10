namespace Randonneur
{
    public class Junction(string Name) : GfxWaypoint(Name)
    {
        internal float Distance; // Distance from start (meter)
    }
}
