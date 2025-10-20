using System;
using Godot;

public partial class WorldMap : Control
{
    /// <summary>
    /// Godot path to MapRect (contains trail Line2D and CollisionShape2D)
    /// </summary>
    private const string MapRectPath = "BgMargin/BgNinePathRect/MapMargin/MapRect";

    /// <summary>
    /// Storing that middle mouse button is currently pressed.
    /// </summary>
    private bool _middleButton = false;

    /// <summary>
    /// Container which store the map.
    /// </summary>
    private MarginContainer? _margin;

    /// <summary>
    /// local position of mouse cursor when clicking on middle button.
    /// </summary>
    private Vector2 _dragPosition;

    /// <summary>
    /// name of the current trail (node name of the Area2D).
    /// </summary>
    public string? SelectedTrail = null;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Name = "worldMapControl";
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
                && SelectedTrail != null)
            {
                // Left mouse button to select a junction destination
                GD.Print($"WorldMap Button pressed {SelectedTrail}");
                Player.Instance.EmitSignal(
                    Player.SignalName.TrailJunctionChoice,
                    SelectedTrail
                );
                SelectedTrail = null;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Middle)
            {
                // Middle mouse button to move the map window
                GD.Print($"Middle {mouseEvent.Pressed}");
                _middleButton = false;
                if (mouseEvent.Pressed)
                {
                    Vector2 mousePosition = GetLocalMousePosition();
                    if (_margin.GetRect().HasPoint(mousePosition))
                    {
                        _middleButton = true;
                        _dragPosition = new Vector2(mousePosition.X, mousePosition.Y);
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
     * <summary>
     * Hide and disable collision detection for the map
     * </summary>
     * <param name="value">true to disable, false to enable</param>
     */
    public void CollisionStatus(bool value)
    {
        // Hide or show the map
        Visible = value;

        // Enable or disable all trails collisions shapes
        ColorRect mapRect = GetNode<ColorRect>(MapRectPath);
        foreach (var child in mapRect.GetChildren())
        {
            foreach (var subchild in child.GetChildren())
            {
                // Line collision shapes
                if (subchild.Name.ToString().StartsWith(MapGenerator.TrailCollisionFilterName))
                {
                    // Disable collision detection for line collision shapes
                    CollisionShape2D collision = (CollisionShape2D)subchild;
                    collision.SetDeferred("disabled", !value);
                }
                // Junction collision shapes
                else if (
                    subchild.Name.ToString().StartsWith(MapJunctionArea.JunctionAreaFilterName)
                )
                {
                    // Disable collision detection for junction collision shapes
                    CollisionShape2D collision = subchild.GetNodeOrNull<CollisionShape2D>(
                        MapJunctionArea.JunctionCollisionFilterName
                    );
                    collision?.SetDeferred("disabled", !value);
                    GD.Print($"collision Name {collision?.Name} {!value}");
                }
            }
        }
    }
}
