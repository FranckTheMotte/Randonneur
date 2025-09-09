using Godot;
using System;

namespace MouseSelector;

public partial class MouseArea2d : Area2D
{

    private MapArea m_mapArea;

    public override void _Ready()
    {
        Connect("area_entered", new Callable(this, nameof(_on_area_entered)));
        Connect("area_exited", new Callable(this, nameof(_on_area_exited)));
        Monitorable = true;
        Monitoring = true;
    }

    private void _on_area_entered(Area2D area)
    {
        switch (area.Name)
        {
            case "MapArea":
                m_mapArea.EmitSignal(MapArea.SignalName.TrailSelection, area, true);
                break;
        }
    }

    private void _on_area_exited(Area2D area)
    {
        switch (area.Name)
        {
            case "MapArea":
                m_mapArea.EmitSignal(MapArea.SignalName.TrailSelection, area, false);
                break;
        }
    }

    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
    }

    internal void setMapArea(MapArea mapArea)
    {
        m_mapArea = mapArea;
    }

}
