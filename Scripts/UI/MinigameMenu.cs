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

        /// <summary>
        /// Reference to the Picture Game.
        /// </summary>
        internal PictureGame? _pictureGameScene;

        Camera2D? _camera;

        public override void _Ready()
        {
            _pictureGameScene =
                GetNode<PictureGame>(
                    "BgMargin/BgNinePathRect/MapMargin/MapRect/GridContainer/ChoiceContainer/VBoxChoice/PictureGame"
                ) ?? throw new InvalidOperationException("Failed to find Picture Game scene.");

            // desactivate the camera otherwise, it could conflict with others.
            Node2D cameraHUD = _pictureGameScene.GetNode<Node2D>("CameraHUD");
            _camera = cameraHUD.GetNode<Camera2D>("Camera2D");
            _camera.Enabled = false;
        }

        /// <summary>
        /// Normal button.
        /// </summary>
        public void _on_normal_button_pressed()
        {
            if (_pictureGameScene == null || _camera == null)
            {
                GD.PushError("_on_normal_button_pressed(): failure on ressources.");
                return;
            }

            _pictureGameScene.Visible = true;
            _camera.Enabled = true;
        }

        /// <summary>
        /// Quit button.
        /// </summary>
        public void _on_quit_button_pressed()
        {
            if (_pictureGameScene == null || _camera == null)
            {
                GD.PushError("_on_quit_button_pressed(): failure on ressources.");
                return;
            }

            _pictureGameScene.Visible = false;
            _camera.Enabled = false;
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
