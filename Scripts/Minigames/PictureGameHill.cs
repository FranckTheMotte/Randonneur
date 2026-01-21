using System;
using System.Drawing;
using System.Numerics;
using Godot;
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

    // Outside of landascape
    const float OFFLANDSCAPE = 1000000.0f;
    private readonly Random _rand = new();

    /// <summary>
    /// Bounds of landscape.
    /// </summary>
    private Vector3 _planeStartPosition;
    private Vector3 _planeEndPosition;

    /// <summary>
    /// Ref to plane.
    /// </summary>
    private MeshInstance3D? _plane;

    public override void _Ready()
    {
        _plane = GetNode<MeshInstance3D>("HillLandscape/Plane");
        Node3D hillLandscape = GetNode<Node3D>("HillLandscape");
        Vector3 planeSize = _plane.Mesh.GetAabb().Size * _plane.Scale;

        // Divide by 2 because it's centered
        _planeStartPosition = (-planeSize / 2) + hillLandscape.Position;
        _planeEndPosition = (planeSize / 2) + hillLandscape.Position;

        InitTrees();
    }

    /// <summary>
    /// Place randomly trees in areas.
    /// </summary>
    private void InitTrees()
    {
        // sanity checks
        if (_plane == null)
        {
            GD.PushError("InitTrees() sanity check failed.");
            return;
        }

        // Load tree Textures
        Texture2D[] treeTexture2D =
        {
            GD.Load<Texture2D>("res://Art/Background/Tree1.png"),
            GD.Load<Texture2D>("res://Art/Background/Tree2.png"),
            GD.Load<Texture2D>("res://Art/Background/Tree3.png"),
        };

        // use various polygons to define tree areas
        // polygons forms are defined in the scene
        Polygon2D[] polygons = new Polygon2D[9];
        for (int i = 0; i < polygons.Length; i++)
        {
            // place somewhere in the landscape
            Vector2 randomPositions = new(
                _planeStartPosition.X + _rand.Next(100, 900),
                _planeStartPosition.Z + _rand.Next(100, 900)
            );
            polygons[i] = GetNode<Polygon2D>("RandoPoly2D0" + (i + 1));
            Vector2[] polygon = polygons[i].Polygon;

            // shift each polygon's coordinates
            for (int j = 0; j < polygons[i].Polygon.Length; j++)
            {
                polygon[j] += randomPositions;
            }
            polygons[i].Polygon = polygon;

            // hide defined Polygon2D
            polygons[i].Visible = false;
        }

        int treeCounter = 0;
        // Populate trees in each polygon
        for (int i = 0; i < polygons.Length; i++)
        {
            // Browse all coords to place items randomly
            for (float x = _planeStartPosition.X; x < _planeEndPosition.X; x += TreeDensity)
            {
                for (float z = _planeStartPosition.Z; z < _planeEndPosition.Z; z += TreeDensity)
                {
                    // Trees are added randomly within allowed areas, not on every pixel.
                    if (
                        Geometry2D.IsPointInPolygon(new Vector2(x, z), polygons[i].Polygon)
                        && _rand.Next(0, TreeDensity * 15) == 0
                    )
                    {
                        treeCounter++;
                        float landscapeY = GetYOnLandscape(x, z);
                        if (landscapeY != OFFLANDSCAPE)
                        {
                            Sprite3D tree = new()
                            {
                                Texture = treeTexture2D[_rand.Next(0, treeTexture2D.Length)],
                                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                                Shaded = true,
                                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                                Scale = new Vector3(TreeSize, TreeSize, TreeSize),
                            };
                            tree.Position = new Vector3(
                                x,
                                landscapeY + (tree.Texture.GetSize().Y / _plane.Scale.Y) + 1,
                                z
                            );
                            AddChild(tree);
                        }
                    }
                }
            }
        }
        GD.Print($"{treeCounter} trees added.");
    }

    /// <summary>
    /// From a specific point, use ray casting to retrieve y hitting the landscape (StaticBody3D).
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns>A valid y coord if found, OFFLANDSCAPE otherwise.</returns>
    private float GetYOnLandscape(float x, float z)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        Vector3 rayOrigin = new(x, 1000f, z);
        Vector3 rayEnd = new(x, -1000f, z);

        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            Vector3 hitPosition = (Vector3)result["position"];
            float meshY = hitPosition.Y;
            return meshY;
        }

        // no collision
        return OFFLANDSCAPE;
    }
}
