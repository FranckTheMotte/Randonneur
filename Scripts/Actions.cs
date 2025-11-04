using Godot;

public partial class Actions : CanvasLayer
{
    // Called when the node enters the scene tree for the first time.
    [Export]
    Player? _player;

    public override void _Ready()
    {
        // Sanity checks
        if (_player == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        _player.Move = true;
    }

    private void _on_check_button_toggled(bool isToggled)
    {
        // Sanity checks
        if (_player == null)
        {
            GD.PushWarning($"${nameof(_on_check_button_toggled)}: sanity checks failed");
            return;
        }

        /* Test to start or stop the auto-walk */
        _player.Move = isToggled;
    }
}
