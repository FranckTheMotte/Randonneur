using Godot;

/// <summary>
/// Display a rectangle to debug sprite 2D projection from 3D env.
/// </summary>
public partial class DebugOverlay : Control
{
    /// <summary>
    /// The rect to draw.
    /// </summary>
    public Rect2 ScreenRect;

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        // A red rectangle
        DrawRect(ScreenRect, Colors.Red, false, 2);
    }
}
