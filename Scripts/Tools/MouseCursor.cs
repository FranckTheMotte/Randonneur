using Godot;

/* As the detection with mouse cursor is easiest with an Area2D
This class is here to use area detection instead of classical
mouse surface detection.

*/
public partial class MouseCursor : Area2D
{
    public override void _Ready()
    {
        Connect("area_entered", new Callable(this, nameof(_on_area_entered)));
        Connect("area_exited", new Callable(this, nameof(_on_area_exited)));
        Monitorable = true;
        Monitoring = true;
    }

    private void _on_area_entered(Area2D area)
    {
        if (area.Name.ToString().StartsWith("trace"))
            area.EmitSignal(MapArea.SignalName.TrailSelection, area, true);
        else if (area.Name.ToString().StartsWith(MapJunctionArea.JunctionAreaFilterName))
            area.EmitSignal(MapJunctionArea.SignalName.TrailSelection, area, true);
    }

    private void _on_area_exited(Area2D area)
    {
        if (area.Name.ToString().StartsWith("trace"))
            area.EmitSignal(MapArea.SignalName.TrailSelection, area, false);
        else if (area.Name.ToString().StartsWith(MapJunctionArea.JunctionAreaFilterName))
            area.EmitSignal(MapJunctionArea.SignalName.TrailSelection, area, false);
    }

    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
    }
}
