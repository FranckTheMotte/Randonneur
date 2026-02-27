using System.Diagnostics;
using Godot;

public partial class HiddenNPC : Sprite3D
{
    /// <summary>
    /// Robin's textures.
    /// </summary>
    private Texture2D robinTexture = GD.Load<Texture2D>("res://Art/Minigame/Picture/robin.png");
    private Texture2D outlinedRobinTexture = GD.Load<Texture2D>(
        "res://Art/Minigame/Picture/OutlinedRobin.png"
    );

    /// <summary>
    /// Area of origin NPC sprite (number of pixels).
    /// </summary>
    public float _originArea { get; private set; } = 0.0f;

    /// <summary>
    /// Cached bounds value.
    /// </summary>
    private Vector3[] _localBounds = [];

    /// <summary>
    /// Current focus state.
    /// </summary>
    private bool _focused = true;

    public override void _Ready()
    {
        Focus(false);
    }

    /// <summary>
    /// NPC Distance from center of the viewport.
    /// </summary>
    /// <param name="camera">3D camera used to display the scene integrating the sprite.</param>
    /// <param name="viewportCenter">Coordinates of his viewport center.</param>
    /// <returns>A distance in pixels.</returns>
    public float DistanceFromCenter(Camera3D camera, Vector2 viewportCenter)
    {
        Vector3 worldPos = GlobalTransform.Origin;

        // Convert to view space
        Transform3D viewInv = camera.GetCameraTransform().AffineInverse();
        Vector3 viewPos = viewInv * worldPos;

        // FOV-neutral relative offset
        Vector2 relative = new(viewPos.X / -viewPos.Z, viewPos.Y / -viewPos.Z);

        return relative.DistanceTo(Vector2.Zero) / camera.Fov;
    }

    /// <summary>
    /// Update texture and linked properties when NPC is change focus state.
    /// </summary>
    /// <param name="enable"></param>
    public void Focus(bool enable)
    {
        // no state change
        if (_focused == enable)
            return;

        if (enable)
        {
            Texture = outlinedRobinTexture;
        }
        else
        {
            Texture = robinTexture;
        }

        // Update cached properties
        // Rectangle real size
        Vector2 size2D = Texture.GetSize() * PixelSize;
        Vector2 half = size2D * 0.5f;

        _localBounds =
        [
            new(-half.X, -half.Y, 0),
            new(half.X, -half.Y, 0),
            new(half.X, half.Y, 0),
            new(-half.X, half.Y, 0),
        ];

        // area
        _originArea = (Texture.GetWidth() * Scale.X) * (Texture.GetHeight() * Scale.Y);

        _focused = enable;
    }

    /// <summary>
    /// Evaluate the percent of sprite displayed on screen (bounded by viewport) and if it's fully displayed.
    /// </summary>
    /// <remarks>
    /// Sprite must be :
    /// - centered
    /// - billboard property to "Enabled"
    /// - front of the camera
    /// </remarks>
    /// <param name="camera">3D camera used to display the scene integrating the sprite.</param>
    /// <param name="viewportSize">Size of the viewport used to display the sprite.</param>
    /// <param name="debugOverlay">Optional debug overlay on screen.</param>
    /// <returns>
    /// - percent is the percent between displayed NPC sprite and the original size.
    /// - fullyVisible is a flag to tell if the sprite is fully displayed in the current viewport.
    /// - debugLog debug logs.
    /// </returns>
    public (float percent, bool fullyVisible, String debugLog) GetVisibilityStatus(
        Camera3D camera,
        Vector2 viewportSize,
        DebugOverlay? debugOverlay = null
    )
    {
        String debugLog = "";

        if (!Centered && Billboard != BaseMaterial3D.BillboardModeEnum.Enabled)
        {
            GD.PushError("GetVisibilityStatus(): Only works with centered and billboarded sprite.");
            return (0.0f, false, debugLog);
        }

        Vector2 spriteSize = new();
        bool fullyVisible = false;

        // Position in the classical view space (align with rectangle coord)
        Vector3 viewPos = camera.GetCameraTransform().AffineInverse() * GlobalTransform.Origin;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        // for each rectangle's corner
        for (int i = 0; i < 4; i++)
        {
            // apply global scale
            Vector3 scaled = new(
                _localBounds[i].X * Scale.X,
                _localBounds[i].Y * Scale.Y,
                _localBounds[i].Z * Scale.Z
            );

            // The sprite 3D use Billboard (align on camera plan)
            Vector3 viewCorner = viewPos + scaled;
            // Ref to world
            Vector3 worldCorner = camera.GetCameraTransform() * viewCorner;

            // switch to 2D coordinates
            Vector2 screen = camera.UnprojectPosition(worldCorner);

            minX = Mathf.Min(minX, screen.X);
            maxX = Mathf.Max(maxX, screen.X);
            minY = Mathf.Min(minY, screen.Y);
            maxY = Mathf.Max(maxY, screen.Y);
        }

        // evaluate the sprite size and her visibility
        float width = maxX - minX;
        float height = maxY - minY;

        Vector2 upLeft = new(minX, minY);
        Vector2 upRight = new(minX + width, minY);
        Vector2 downLeft = new(minX, minY + height);

        // sprite size have to be reduced if a part get out of the screen
        float visibleTopRight2DX2 = Math.Min(upRight.X, viewportSize.X);
        float visibleTopLeft2DX2 = Math.Max(upLeft.X, 0.0f);
        float visibleTopLeft2DY2 = Math.Max(upLeft.Y, 0.0f);
        float visibleBottomLeft2DY2 = Math.Min(downLeft.Y, viewportSize.Y);

        // positions must match the expected corners of sprite
        fullyVisible = Mathf.IsEqualApprox(visibleTopRight2DX2, upRight.X);
        fullyVisible &= Mathf.IsEqualApprox(visibleTopLeft2DX2, upLeft.X);
        fullyVisible &= Mathf.IsEqualApprox(visibleTopLeft2DY2, upLeft.Y);
        fullyVisible &= Mathf.IsEqualApprox(visibleBottomLeft2DY2, downLeft.Y);

        // final size
        spriteSize.X = Math.Max(visibleTopRight2DX2 - visibleTopLeft2DX2, 0.0f);
        spriteSize.Y = Math.Max(visibleBottomLeft2DY2 - visibleTopLeft2DY2, 0.0f);

        debugLog =
            $"topLeft2D {upLeft} spriteSize {spriteSize}\n"
            + $"topRight2D {upRight}\n"
            + $"bottomLeft2D {downLeft}\n"
            + $"fullyVisible {fullyVisible}\n";

        if (debugOverlay != null)
        {
            debugOverlay.ScreenRect = new Rect2(
                new Vector2(minX, minY),
                new Vector2(maxX - minX, maxY - minY)
            );
        }

        // Evaluate percent of displayed NPC
        float currentArea = (spriteSize.X * spriteSize.Y);
        float percent = Mathf.Sqrt(currentArea / _originArea) * 100.0f;

        return (percent, fullyVisible, debugLog);
    }
}
