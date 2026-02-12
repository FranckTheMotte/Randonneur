using Godot;

/// <summary>
/// Progress bar for grumbling status.
/// </summary>
public partial class GrumbleStatus : TextureProgressBar
{
    public override void _Ready()
    {
        Update();
    }

    /// <summary>
    /// Refresh the progress bar value with current player grumbling level.
    /// </summary>
    public void Update()
    {
        Player? player = Player.Instance;
        if (player != null)
        {
            Value = player.CurrentGrumblingLevel * 100 / player.MaxGrumblingLevel;
        }
    }
}
