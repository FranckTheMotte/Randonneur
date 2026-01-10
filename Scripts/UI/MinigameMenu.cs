using Godot;

namespace Randonneur
{
    /// <summary>
    /// This class describe and setup the minigame before launching it.
    /// It also add the possibility to skip it.
    /// </summary>
    public partial class MinigameMenu : Control
    {
        /// <summary>
        /// Minigame types.
        /// </summary>
        public enum MGType
        {
            Picture,
            Plants,
            None,
        }
    }
}
