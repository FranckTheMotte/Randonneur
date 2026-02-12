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

        /// <summary>
        /// State of game launch.
        /// </summary>
        Boolean _pictureGameLaunched = false;

        /// <summary>
        /// Where to display the game.
        /// </summary>
        private SubViewport? _gameContainer;

        public override void _Ready()
        {
            _gameContainer =
                GetNode<SubViewport>(
                    "BgMargin/BgNinePathRect/MapMargin/MapRect/GridContainer/ChoiceContainer/VBoxChoice/Control/SubViewportContainer/SubViewport"
                ) ?? throw new InvalidOperationException("Failed to find Game container.");
        }

        /// <summary>
        /// Normal button.
        /// </summary>
        public void _on_normal_button_pressed()
        {
            if (_gameContainer == null)
            {
                GD.PushError("_on_normal_button_pressed(): sanity check failed.");
                return;
            }

            // already launched?
            if (_pictureGameLaunched)
            {
                return;
            }

            // Load scene here (can take time)
            PackedScene aPackedScene = GD.Load<PackedScene>("res://Scenes/picture_game_box.tscn");
            _pictureGameScene = aPackedScene.Instantiate<PictureGame>();
            _gameContainer.AddChild(_pictureGameScene);
            _pictureGameLaunched = true;
        }

        /// <summary>
        /// Quit button. Hide the window, release sub ressources and re-enable player to
        /// the current level.
        /// </summary>
        public void _on_quit_button_pressed()
        {
            if (_gameContainer == null || _pictureGameScene == null)
            {
                GD.PushError("_on_quit_button_pressed(): sanity check failed.");
                return;
            }
            _gameContainer.RemoveChild(_pictureGameScene);
            _pictureGameScene.QueueFree();
            Visible = false;
            _pictureGameLaunched = false;
            Player.Instance?.BackToLevel();
        }
    }
}
