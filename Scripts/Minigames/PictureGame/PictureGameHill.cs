using System;
using System.Drawing;
using System.Numerics;
using Godot;
using Randonneur;
// Because of System.Numerics
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

/// <summary>
/// Class to populate a hill for the picture game.
/// The following things will be randomly added :
/// - vegetations
/// - rocks
/// </summary>
public partial class PictureGameHill : Node3D
{
    /// <summary>
    /// Density of trees in their area, lower is the number, higher is the number of trees.
    /// </summary>
    [Export]
    public int TreeDensity = 3;

    /// <summary>
    /// Size multiplier for the trees.
    /// </summary>
    [Export]
    public int TreeSize = 2;

    /// <summary>
    /// Bounds of landscape.
    /// </summary>
    private Vector3 _planeStartPosition;
    private Vector3 _planeEndPosition;

    /// <summary>
    /// Access to Landscape tools.
    /// </summary>
    Landscape? _landscape;

    public override void _Ready()
    {
        // TODO
        // _ready must provide:
        // which landscape to use (blender mesh)
        // which landscape object will be added
        // object to find
        // properties to help or annoy

        _landscape = new(this);

        // default value
        if (!_landscape.Init(5, 5))
        {
            throw new InvalidOperationException("Failed to init landscape.");
        }

        _landscape.InitTrees(TreeDensity, TreeSize);
        _landscape.InitHiddenNPC(GetNode<VisibleOnScreenNotifier3D>("HiddenNPCNotifier3D"));
    }

    void _on_hidden_npc_notifier_3d_screen_entered()
    {
        GD.Print("Hidden NPC visible");
    }

    void _on_hidden_npc_notifier_3d_screen_exited()
    {
        GD.Print("Hidden NPC hidden");
    }
}
