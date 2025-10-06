using Godot;

/**
  Class to handle behavior of the junction Area2D.
*/
public partial class JunctionArea : Area2D
{
    private const string SquareName = "Square2D";
    private const int SquareSize = 16;
    private const int SquareZindex = 4; // Front of map
    private static readonly Color UnselectedColor = Colors.CadetBlue;
    private static readonly Color SelectedColor = Colors.MediumOrchid;

    // Display of a junction
    private readonly ColorRect _junctionRect = new();

    /**
        Constructor with mandatory properties

        @param position  middle position of the junction.
        @param name      Litteral name of junction.
        @param traceName Gpx file name which store this junction.
    */
    public JunctionArea(Vector2 position, string name, string traceName)
    {
        // -- Setup the junction square
        _junctionRect.Name = SquareName;
        _junctionRect.Color = UnselectedColor;
        _junctionRect.Size = new Vector2(SquareSize, SquareSize);
        _junctionRect.ZIndex = SquareZindex;
        AddChild(_junctionRect);
        // Add a collision shape
        RectangleShape2D rectangle = new() { Size = new Vector2(SquareSize, SquareSize) };
        CollisionShape2D collision = new()
        {
            Shape = rectangle,
            Position = new Vector2(SquareSize / 2, SquareSize / 2),
        };
        AddChild(collision);

        // -- Setup area2D
        Name = name;
        // trace name stored in description as it will be kept raw
        EditorDescription = traceName;
        Position = new Vector2(position.X - (SquareSize / 2), position.Y - (SquareSize / 2));
        // Collision with mouse cursor
        SetCollisionLayerValue(1, false);
        SetCollisionLayerValue(4, true);
    }

    [Signal]
    public delegate void TrailSelectionEventHandler();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Trigger actions when mouse go over/out a trail
        Connect("TrailSelection", new Callable(this, nameof(_on_trail_selection)));
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

        // Retrieve the rect
        ColorRect junctionRect = area.GetNodeOrNull<ColorRect>(SquareName);
        if (junctionRect == null)
            return;
        GD.Print($"junction selection {area.Name}");

        if (selected)
        {
            SetColor(junctionRect, SelectedColor);
            worldMap.m_selectedTrail = area.EditorDescription;
        }
        else
        {
            SetColor(junctionRect, UnselectedColor);
            worldMap.m_selectedTrail = null;
        }
    }

    /**
      Set body color of a junction ColorRect.

      @param junctionRect a Colorect
      @param color        a Color
    */
    private static void SetColor(ColorRect junctionRect, Color color)
    {
        junctionRect.Color = color;
    }
}
