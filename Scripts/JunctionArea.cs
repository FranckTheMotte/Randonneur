using Godot;

/**
  Class to handle behavior of the junction Area2D.
*/
public partial class JunctionArea : Area2D
{
    private const string SQUARE_NAME = "Square2D";
    private const int SQUARE_SIZE = 16;
    private const int SQUARE_ZINDEX = 4; // Front of map
    private Color UNSELECTED_COLOR = Colors.CadetBlue;
    private Color SELECTED_COLOR = Colors.MediumOrchid;

    // Display of a junction
    private ColorRect JunctionRect = new();

    /**
        Constructor with mandatory properties

        @param position  middle position of the junction.
        @param name      Litteral name of junction.
        @param traceName Gpx file name which store this junction.
    */
    public JunctionArea(Vector2 position, string name, string traceName)
    {
        // -- Setup the junction square
        JunctionRect.Name = SQUARE_NAME;
        JunctionRect.Color = UNSELECTED_COLOR;
        JunctionRect.Size = new Vector2(SQUARE_SIZE, SQUARE_SIZE);
        JunctionRect.ZIndex = SQUARE_ZINDEX;
        AddChild(JunctionRect);
        // Add a collision shape
        RectangleShape2D rectangle = new() { Size = new Vector2(SQUARE_SIZE, SQUARE_SIZE) };
        CollisionShape2D collision = new()
        {
            Shape = rectangle,
            Position = new Vector2(SQUARE_SIZE / 2, SQUARE_SIZE / 2),
        };
        AddChild(collision);

        // -- Setup area2D
        Name = name;
        // trace name stored in description as it will be kept raw
        EditorDescription = traceName;
        Position = new Vector2(position.X - (SQUARE_SIZE / 2), position.Y - (SQUARE_SIZE / 2));
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
        ColorRect junctionRect = area.GetNode<ColorRect>(SQUARE_NAME);
        GD.Print($"junction selection {area.Name}");

        if (selected)
        {
            SetColor(junctionRect, SELECTED_COLOR);
            worldMap.m_selectedTrail = area.EditorDescription;
        }
        else
        {
            SetColor(junctionRect, UNSELECTED_COLOR);
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
