using System.Collections.Generic;
using Godot;

/// <summary>
/// Class to add a border between walking zone and ground.
/// </summary>
public partial class PolygonWithBorder : Node2D
{
    /// <summary>
    /// Ground surface. Polygon must be counterclockwise direction.
    /// </summary>
    [Export]
    public Polygon2D? Surface;

    [Export]
    public Texture2D? BorderTexture;

    /// <summary>
    /// Depth of border (starting from the Surface)
    /// </summary>
    [Export]
    public float BorderDepth = 32f;

    /// <summary>
    /// Default width of the segment to apply a texture.
    /// It is used to set a correct U in UVs.
    /// </summary>
    [Export]
    public float SegmentWidth = 64f;

    public override void _Ready()
    {
        try
        {
            GenerateBorderMesh();
        }
        catch (System.Exception ex)
        {
            GD.PushError($"Failed to generate border mesh: {ex.Message}");
        }
    }

    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;

    /// <summary>
    /// Generate the border with external parameters.
    /// </summary>
    void GenerateBorderMesh()
    {
        if (Surface == null || BorderTexture == null)
        {
            GD.PushWarning("GenerateBorderMesh(): Surface or BorderTexture is null.");
            return;
        }

        if (Surface.Polygon == null || Surface.Polygon.Length < 3)
        {
            GD.PushWarning("GenerateBorderMesh(): Polygon has insufficient points.");
            return;
        }

        if (BorderDepth <= 0f || SegmentWidth <= 0f)
        {
            GD.PushWarning("GenerateBorderMesh(): Invalid BorderDepth or SegmentWidth.");
            return;
        }

        Vector2[] poly = Surface.Polygon;
        ArrayMesh mesh = new();

        int vertexCount = poly.Length * VerticesPerQuad;
        int indexCount = poly.Length * IndicesPerQuad;
        Vector2[] vertices = new Vector2[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] indices = new int[indexCount];

        float u = 0f;
        int vertexIndex = 0;
        int indexOffset = 0;

        // For each polygon segment
        for (int i = 0; i < poly.Length; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % poly.Length];

            // Calculate the normal (outward)
            Vector2 direction = (b - a).Normalized();
            // counterclockwise rotation
            Vector2 normal = new(-direction.Y, direction.X);

            float length = a.DistanceTo(b);

            // Quad
            vertices[vertexIndex + 0] = a;
            vertices[vertexIndex + 1] = b;
            vertices[vertexIndex + 2] = b + normal * BorderDepth;
            vertices[vertexIndex + 3] = a + normal * BorderDepth;

            // UV
            float u0 = u;
            float u1 = u + (length / SegmentWidth);
            uvs[vertexIndex + 0] = new Vector2(u0, 0);
            uvs[vertexIndex + 1] = new Vector2(u1, 0);
            uvs[vertexIndex + 2] = new Vector2(u1, 1);
            uvs[vertexIndex + 3] = new Vector2(u0, 1);

            // Add 2 triangles by quad
            //    0 ---- 1
            //    |    / |
            //    |  /   |
            //    3 ---- 2
            indices[indexOffset + 0] = vertexIndex + 0;
            indices[indexOffset + 1] = vertexIndex + 1;
            indices[indexOffset + 2] = vertexIndex + 2;
            indices[indexOffset + 3] = vertexIndex + 2;
            indices[indexOffset + 4] = vertexIndex + 3;
            indices[indexOffset + 5] = vertexIndex + 0;

            u += length / SegmentWidth;
            vertexIndex += VerticesPerQuad;
            indexOffset += IndicesPerQuad;
        }

        // Create the "grass" surface.
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        MeshInstance2D meshInstance = new()
        {
            Mesh = mesh,
            Texture = BorderTexture,
            TextureFilter = TextureFilterEnum.NearestWithMipmapsAnisotropic,
            TextureRepeat = TextureRepeatEnum.Enabled,
        };

        AddChild(meshInstance);
    }
}
