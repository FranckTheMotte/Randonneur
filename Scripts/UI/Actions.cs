using System.Security;
using Godot;

public partial class Actions : CanvasLayer
{
    public override void _Ready()
    {
        // Sanity checks
        if (Player.Instance == null)
        {
            GD.PushWarning($"${nameof(_on_check_button_toggled)}: sanity checks failed");
            return;
        }

        Player.Instance.Move = true;
        SpinBox speed = GetNode<SpinBox>("PlayerSettings/Speed");
        speed.Value = Player.Instance.Walk;
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

    /// <summary>
    /// Signal to modify the player speed
    /// </summary>
    private void _on_speed_value_changed(float Speed)
    {
        // Sanity checks
        if (Player.Instance == null)
        {
            GD.PushWarning($"${nameof(_on_check_button_toggled)}: sanity checks failed");
            return;
        }

        // retrieve the direction
        int direction = Player.Instance.Walk >= 0 ? 1 : -1;
        Player.Instance.Walk = (int)Speed * direction;
        Player.Instance.Move = true;
    }
}
