using System;
using Godot;

public partial class WorldMap : Control
{
    // Godot path to MapRect (contains trail Line2D and CollisionShape2D)
    private const string MAPRECT_PATH = "BgMargin/BgNinePathRect/MapMargin/MapRect";

    // flag storing that middle mouse button is currently pressed
    private bool m_middleButton = false;

    // Container which store the map
    private MarginContainer m_margin;

    // local position of mouse cursor when clicking on middle button
    Vector2 m_dragPosition;

    // name of the current trail (node name of the Area2D)
    public string m_selectedTrail = null;

    // Wolrd map Singleton

    private static readonly Lazy<WorldMap> lazy = new Lazy<WorldMap>(() => new WorldMap());
    public static WorldMap Instance
    {
        get { return lazy.Value; }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        m_margin = GetNode<MarginContainer>("BgMargin");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (
                mouseEvent.ButtonIndex == MouseButton.Left
                && mouseEvent.Pressed
                && Instance.m_selectedTrail != null
            )
            {
                GD.Print($"Button pressed {m_selectedTrail} {Instance.m_selectedTrail}");
                Player player = Player.Instance;
                player.EmitSignal(Player.SignalName.TrailJunctionChoice, Instance.m_selectedTrail);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Middle)
            {
                GD.Print($"Middle {mouseEvent.Pressed}");
                m_middleButton = false;
                if (mouseEvent.Pressed)
                {
                    Vector2 mousePosition = GetLocalMousePosition();
                    if (m_margin.GetRect().HasPoint(mousePosition))
                    {
                        m_middleButton = true;
                        m_dragPosition = new Vector2(mousePosition.X, mousePosition.Y);
                        GD.Print($"decalage X {m_dragPosition.X} Y {m_dragPosition.Y}");
                    }
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (m_middleButton)
            {
                Position = GetGlobalMousePosition() - m_dragPosition;
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
        ColorRect mapRect = GetNode<ColorRect>(MAPRECT_PATH);
        foreach (var child in mapRect.GetChildren())
        {
            foreach (var subchild in child.GetChildren())
            {
                if (subchild.Name.ToString().StartsWith(MapGenerator.TRAIL_COLLISION_FILTER_NAME))
                {
                    CollisionShape2D collision = (CollisionShape2D)subchild;
                    collision.Disabled = value;
                }
            }
        }
    }
}
