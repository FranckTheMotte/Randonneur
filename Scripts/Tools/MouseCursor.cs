using Godot;
using System;
using System.Collections;

/* As the detection with mouse cursor is easiest with an Area2D
This class is here to use area detection instead of classical
mouse surface detection.

*/
public partial class MouseCursor : Area2D
{

    // list of trails on the map
    private Hashtable m_trailTable = new Hashtable();

    public override void _Ready()
    {
        Connect("area_entered", new Callable(this, nameof(_on_area_entered)));
        Connect("area_exited", new Callable(this, nameof(_on_area_exited)));
        Monitorable = true;
        Monitoring = true;
    }

    private void _on_area_entered(Area2D area)
    {
        MapArea mapArea = (MapArea)m_trailTable[area.Name];
        area.EmitSignal(MapArea.SignalName.TrailSelection, area, true);
    }

    private void _on_area_exited(Area2D area)
    {
        area.EmitSignal(MapArea.SignalName.TrailSelection, area, false);
    }

    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
    }

    internal void setTrails(Hashtable trails)
    {
        m_trailTable = trails;
    }
}
