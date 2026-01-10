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

        public void _on_quit_button_pressed()
        {
            Visible = false;
            // Re-activated the player to let collide with next waypoint collision shape
            if (Player.Instance != null)
            {
                Player.Instance.Move = true;
                Player.Instance.ForceJunction = true;
            }
        }
    }
}
