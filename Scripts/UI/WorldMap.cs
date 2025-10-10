using System;
using Godot;

public partial class WorldMap : Control
{
    // Godot path to MapRect (contains trail Line2D and CollisionShape2D)
    private const string MapRectPath = "BgMargin/BgNinePathRect/MapMargin/MapRect";

    // flag storing that middle mouse button is currently pressed
    private bool _middleButton = false;

    // Container which store the map
    private MarginContainer? _margin;

    // local position of mouse cursor when clicking on middle button
    private Vector2 _dragPosition;

    // name of the current trail (node name of the Area2D)
    public string? m_selectedTrail = null;

    // Wolrd map Singleton

    private static readonly Lazy<WorldMap> lazy = new Lazy<WorldMap>(() => new WorldMap());
    public static WorldMap Instance
    {
        get { return lazy.Value; }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _margin = GetNode<MarginContainer>("BgMargin");
    }

    public override void _Input(InputEvent @event)
    {
        // Sanity checks
        if (Player.Instance == null || _margin == null)
        {
            GD.PushWarning($"${nameof(_Input)}: sanity checks failed");
            return;
        }

        if (@event is InputEventMouseButton mouseEvent)
        {
            if (
                mouseEvent.ButtonIndex == MouseButton.Left
                && mouseEvent.Pressed
                && Instance.m_selectedTrail != null
            )
            {
                GD.Print($"WorldMap Button pressed {Instance.m_selectedTrail}");
                Player.Instance.EmitSignal(
                    Player.SignalName.TrailJunctionChoice,
                    Instance.m_selectedTrail
                );
                Instance.m_selectedTrail = null;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Middle)
            {
                GD.Print($"Middle {mouseEvent.Pressed}");
                _middleButton = false;
                if (mouseEvent.Pressed)
                {
                    Vector2 mousePosition = GetLocalMousePosition();
                    if (_margin.GetRect().HasPoint(mousePosition))
                    {
                        _middleButton = true;
                        _dragPosition = new Vector2(mousePosition.X, mousePosition.Y);
                        GD.Print($"decalage X {_dragPosition.X} Y {_dragPosition.Y}");
                    }
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (_middleButton)
            {
                Position = GetGlobalMousePosition() - _dragPosition;
            }
        }
    }

    /**
        Hide and disable collision detection for the map

        @param value true to disable, false to enable
    */
    public void Disable(bool value)
    {
        // Hide or show
        Visible = !value;

        // enable or disable all trails collisions shapes
        ColorRect mapRect = GetNode<ColorRect>(MapRectPath);
        foreach (var child in mapRect.GetChildren())
        {
            foreach (var subchild in child.GetChildren())
            {
                // line collision shapes
                if (subchild.Name.ToString().StartsWith(MapGenerator.TrailCollisionFilterName))
                {
                    CollisionShape2D collision = (CollisionShape2D)subchild;
                    // put it as deferred, because it can be called during collision signal
                    collision.SetDeferred("disabled", value);
                }
                // junction collision shapes
                else if (subchild.Name.ToString().StartsWith(JunctionArea.JunctionArea2DFilterName))
                {
                    CollisionShape2D collision = subchild.GetNodeOrNull<CollisionShape2D>(
                        JunctionArea.JunctionCollisionFilterName
                    );
                    collision?.SetDeferred("disabled", value);
                    GD.Print($"collision Name {collision?.Name} {value}");
                }
            }
        }
    }
}
