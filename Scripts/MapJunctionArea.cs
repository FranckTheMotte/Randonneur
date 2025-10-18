using Godot;

/**
 * <summary>
 * Class to handle behavior of the junction Area2D.
 * </summary>
 * <remarks>
 * This class is used to create a visual representation of a junction in the map.
 * </remarks>
 */
public partial class MapJunctionArea : Area2D
{
    public const string JunctionAreaFilterName = "junction";
    public const string JunctionCollisionFilterName = "junction";
    private const string SquareName = "Square2D";
    private const int SquareSize = 16;
    private const int SquareZindex = 4; // Front of map
    private static readonly Color UnselectedColor = Colors.CadetBlue;
    private static readonly Color SelectedColor = Colors.MediumOrchid;

    /// <summary>
    /// Gfx of player marker.
    /// </summary>
    public ColorRect? Landmark;

    [Signal]
    public delegate void TrailSelectionEventHandler();

    // Display of a junction
    private readonly ColorRect _junctionRect = new();

    public MapJunctionArea()
    {
        // player marker is initialize without position and invisible.
        Landmark = new()
        {
            Size = new Vector2(10, 10),
            Visible = false,
            ZIndex = 5,
        };

        AddChild(Landmark);
    }

    /// <summary>
    /// Setup the JunctionArea.
    /// </summary>
    /// <param name="position">Middle position of the junction.</param>
    /// <param name="name">Literal name of junction.</param>
    /// <param name="traceName">Gpx file name which store this junction.</param>
    public void Setup(Vector2 position, string name, string traceName)
    {
        // -- Setup the junction square
        ColorRect junctionRect = new()
        {
            Name = SquareName,
            Color = UnselectedColor,
            Size = new Vector2(SquareSize, SquareSize),
            ZIndex = SquareZindex,
        };
        AddChild(junctionRect);

        // -- Setup area2D
        Name = JunctionAreaFilterName + " " + name;
        // trace name stored in description as it will be kept raw
        SetMeta("TraceName", traceName);
        Position = new Vector2(position.X - (SquareSize / 2), position.Y - (SquareSize / 2));
        // Collision with mouse cursor
        SetCollisionLayerValue(1, false);
        SetCollisionLayerValue(5, true);
        SetCollisionMaskValue(1, false);
        SetCollisionMaskValue(5, true);

        // Add a collision shape
        RectangleShape2D rectangle = new() { Size = new Vector2(SquareSize, SquareSize) };
        CollisionShape2D collision = new()
        {
            Name = JunctionCollisionFilterName,
            Shape = rectangle,
            Position = new Vector2(SquareSize / 2, SquareSize / 2),
        };
        AddChild(collision);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Trigger actions when mouse go over/out a trail
        Connect("TrailSelection", new Callable(this, nameof(OnTrailSelection)));
        InputPickable = true;
    }

    /// <summary>
    /// Set the color of the junction square.
    /// </summary>
    /// <param name="color">The color to set.</param>
    public void SetColor(Color color)
    {
        // Retrieve the rect
        ColorRect? junctionRect = GetNodeOrNull<ColorRect>(SquareName);
        if (junctionRect == null)
            return;
        junctionRect.Color = color;
    }

    /**
        <summary>
        Called with Signal "TrailSelection".
        </summary>
        <param name="area">Properties of the un/selected area</param>
        <param name="selected">True if selected, False otherwise</param>
        <remarks>
        This method is called when the mouse cursor enters or exits a trail.
        It updates the visual color of the trail and the selected trail of the WorldMap.
        </remarks>
    */
    private void OnTrailSelection(Area2D area, bool selected)
    {
        WorldMap worldMap = WorldMap.Instance;

        // Retrieve the rect
        ColorRect? junctionRect = area.GetNodeOrNull<ColorRect>(SquareName);
        if (junctionRect == null)
            return;
        GD.Print($"junction selection {area.Name}");

        if (selected)
        {
            junctionRect.Color = SelectedColor;
            // Selection of a trail means that the junction is now the destination
            worldMap.SelectedTrail = (string)area.GetMeta("TraceName");
        }
        else
        {
            junctionRect.Color = UnselectedColor;
            worldMap.SelectedTrail = null;
        }
    }
}
