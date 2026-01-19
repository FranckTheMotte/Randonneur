using System;
using System.Numerics;
using Godot;
// Because of System.Numerics
using Vector3 = Godot.Vector3;

/// <summary>
/// Class to populate a hill for the picture game.
/// The following things will be randomly added :
/// - vegetations
/// - rocks
/// </summary>
public partial class PictureGameHill : Node3D
{
    // Outside of landascape
    const float OFFLANDSCAPE = 1000000.0f;
    private readonly Random _rand = new();

    public override void _Ready()
    {
        MeshInstance3D plane = GetNode<MeshInstance3D>("HillLandscape/Plane");
        Node3D hillLandscape = GetNode<Node3D>("HillLandscape");

        Vector3 planeSize = plane.Mesh.GetAabb().Size * plane.Scale;
        // Divice by 2 because it's centered
        Vector3 planeStartPosition = (-planeSize / 2) + hillLandscape.Position;
        Vector3 planeEndPosition = (planeSize / 2) + hillLandscape.Position;

        // Load tree Textures
        Texture2D[] treeTexture2D =
        {
            GD.Load<Texture2D>("res://Art/Background/Tree1.png"),
            GD.Load<Texture2D>("res://Art/Background/Tree2.png"),
            GD.Load<Texture2D>("res://Art/Background/Tree3.png"),
        };

        // Browse all coords to put items randomly
        for (float x = planeStartPosition.X; x < planeEndPosition.X; x += 30.0f)
        {
            for (float z = planeStartPosition.Z; z < planeEndPosition.Z; z += 30.0f)
            {
                float landscapeY = GetYOnLandscape(x, z);
                if (landscapeY != OFFLANDSCAPE)
                {
                    Sprite3D tree = new()
                    {
                        Texture = treeTexture2D[_rand.Next(0, treeTexture2D.Length)],
                        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                        Shaded = true,
                        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                    };
                    tree.Position = new Vector3(
                        x,
                        landscapeY + (tree.Texture.GetSize().Y / plane.Scale.Y) + 1,
                        z
                    );
                    AddChild(tree);
                }
            }
        }
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
