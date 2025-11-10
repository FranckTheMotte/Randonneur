using Godot;

public partial class DestinationButton : Button
{
    public string? GpxFile;

    private void _on_pressed()
    {
        // Sanity checks
        if (GpxFile == null)
        {
            GD.PushWarning($"${nameof(_on_pressed)}: sanity checks failed");
            return;
        }

        GD.Print($"Button pressed {GpxFile}");
        if (Player.Instance is not null)
        {
            Player.Instance.EmitSignal(Player.SignalName.TrailJunctionChoice, GpxFile);
        }
    }
}
