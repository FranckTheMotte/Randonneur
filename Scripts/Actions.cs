using Godot;

public partial class Actions : CanvasLayer
{
    public override void _Ready()
    {
        // Sanity checks
        if (Player.Instance != null)
        {
            Player.Instance.Move = true;
        }
    }

    private void _on_check_button_toggled(bool isToggled)
    {
        // Sanity checks
        if (Player.Instance == null)
        {
            GD.PushWarning($"${nameof(_on_check_button_toggled)}: sanity checks failed");
            return;
        }

        /* Test to start or stop the auto-walk */
        Player.Instance.Move = isToggled;
    }
}
