using Godot;

namespace Randonneur
{
    /// <summary>
    /// Types of objects that can be placed in the landscape.
    /// </summary>
    public enum LandscapeObjectType
    {
        /// <summary>
        /// Tree vegetation areas with multiple tree instances.
        /// </summary>
        Trees,

        /// <summary>
        /// Rock formations including boulders and stone clusters.
        /// </summary>
        Rocks,

        // Future additions:
        // Flowers,
        // Hut,
        // Sheeps
        // Bears
        // ...
    }
}
