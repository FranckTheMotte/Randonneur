using Godot;

public partial class MapArea : Area2D
{
    private Line2D? _trailLine;

    [Signal]
    public delegate void TrailSelectionEventHandler();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Trigger actions when mouse go over/out a trail
        // TODO : it's disabled but keep it.
        // Connect("TrailSelection", new Callable(this, nameof(_on_trail_selection)));
        InputPickable = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    /**
        Called with Signal "TrailSelection".

        @param area Properties of the un/selected area
        @param selected True if selected, False otherwise

    */
    private void _on_trail_selection(Area2D area, bool selected)
    {
        WorldMap worldMap = WorldMap.Instance;

        // Retrieve the line
        _trailLine = area.GetNode<Line2D>("TrailLine2D");
        GD.Print($"trail selection {area.Name}");

        if (selected)
        {
            setColorTrail(Colors.Red);
            worldMap.m_selectedTrail = area.EditorDescription;
        }
        else
        {
            setColorTrail(Colors.Orange);
            worldMap.m_selectedTrail = null;
        }
    }

    private void setColorTrail(Color color)
    {
        // Sanity checks
        if (_trailLine == null)
        {
            GD.PushWarning($"${nameof(setColorTrail)}: sanity checks failed");
            return;
        }

        _trailLine.DefaultColor = color;
    }
}
