using Godot;

public partial class Actions : CanvasLayer
{
    // Called when the node enters the scene tree for the first time.
    [Export]
    Player? player;

    public override void _Ready()
    {
        // Sanity checks
        if (player == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        player.Walk = 0;
    }

    private void _on_check_button_toggled(bool isToggled)
    {
        // Sanity checks
        if (player == null)
        {
            GD.PushWarning($"${nameof(_on_check_button_toggled)}: sanity checks failed");
            return;
        }

        /* Test to start or stop the auto-walk */
        if (isToggled)
        {
            player.Walk = 1;
        }
        else
        {
            player.Walk = 0;
        }
    }
}
