using System.Security;
using Godot;

/// <summary>
/// Actions panel to display and modify player's properties.
/// </summary>
public partial class Actions : CanvasLayer
{
    private SpinBox? _speed;

    public override void _Ready()
    {
        _speed = GetNode<SpinBox>("PlayerSettings/Speed");

        // Sanity checks
        if (Player.Instance == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        Player.Instance.Move = true;
        _speed.Value = Player.Instance.HikerSpeed;
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
        if (Player.Instance == null || _speed == null)
        {
            GD.PushWarning($"${nameof(_on_speed_value_changed)}: sanity checks failed");
            return;
        }

        GD.Print($"player speed {Speed} previous : {Player.Instance.HikerSpeed}");

        Player.Instance.HikerSpeed = (int)Speed;
        _speed.Value = Speed;
        Player.Instance.Move = true;
    }

    /// <summary>
    /// Update the value of speed Spinbox.
    /// </summary>
    /// <param name="HikerSpeed">New speed.</param>
    public void UpdateHikerSpeed(float HikerSpeed)
    {
        // Sanity checks
        if (_speed == null)
        {
            GD.PushWarning($"${nameof(UpdateHikerSpeed)}: sanity checks failed");
            return;
        }
        _speed.Value = HikerSpeed;
    }
}
