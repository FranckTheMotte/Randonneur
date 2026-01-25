using Godot;

/// <summary>
/// Implements a fixed-position, photographic-style camera controller for Godot 3D scenes.
///
/// The camera behaves like a real tripod-mounted photo camera:
/// - The camera node never translates; its world position remains fixed.
/// - Zoom is simulated optically by interpolating the camera Field of View (FOV),
///   producing true telephoto and wide-angle behavior without moving the camera.
/// - Mouse-driven pan and tilt are applied as angular offsets (yaw and pitch),
///   with rotation sensitivity dynamically scaled by the current FOV to preserve
///   perceptual consistency across zoom levels.
/// - Horizontal rotation (pan) is performed around the global vertical axis,
///   while vertical rotation (tilt) is applied around a dynamically computed
///   horizontal axis, ensuring a level horizon and eliminating unintended roll.
/// - Optional damping provides smooth, inertial motion for both rotation and zoom,
///   closely matching the feel of a physical tripod head and zoom lens.
///
/// </summary>
public partial class PhotoCameraController : Camera3D
{
    /* -----------------------------
     * Configuration
     * ----------------------------- */

    [ExportGroup("Zoom")]
    /// <summary>
    /// Minimum allowed Field of View (in degrees).
    /// Represents the telephoto limit of the lens.
    /// Lower values increase perceived motion and require finer control.
    /// </summary>
    [Export]
    public float MinFov = 5f;

    /// <summary>
    /// Maximum allowed Field of View (in degrees).
    /// Represents the wide-angle limit of the lens.
    /// Higher values provide a broader view with lower perceived motion.
    /// </summary>
    [Export]
    public float MaxFov = 90f;

    /// <summary>
    /// Amount of FOV change applied per mouse wheel step.
    /// Controls how quickly the camera zooms in or out in response to input.
    /// </summary>
    [Export]
    public float ZoomStep = 5f;

    /// <summary>
    /// Damping factor for smooth zoom interpolation.
    /// Higher values result in faster, snappier zoom transitions.
    /// Lower values produce slower, heavier lens-like behavior.
    /// </summary>
    [Export]
    public float ZoomDamping = 10f;

    [ExportGroup("Rotation")]
    /// <summary>
    /// Base angular sensitivity (radians per pixel) at a reference FOV of 90 degrees.
    /// The effective sensitivity is dynamically scaled by the current FOV
    /// to preserve consistent photographic behavior across zoom levels.
    /// </summary>
    [Export]
    public float BaseSensitivity = 0.002f;

    /// <summary>
    /// Minimum vertical rotation angle (in degrees).
    /// Limits how far the camera can tilt downward.
    /// Prevents unnatural inversion or excessive downward tilt.
    /// </summary>
    [Export]
    public float MinPitch = -85f;

    /// <summary>
    /// Maximum vertical rotation angle (in degrees).
    /// Limits how far the camera can tilt upward.
    /// Prevents gimbal lock and maintains a realistic pan/tilt range.
    /// </summary>
    [Export]
    public float MaxPitch = 85f;

    [ExportGroup("Rotation Damping")]
    /// <summary>
    /// Enables or disables rotational damping (inertial smoothing).
    /// When enabled, pan and tilt motions interpolate smoothly
    /// toward their target angles, simulating a physical tripod head.
    /// </summary>
    [Export]
    public bool UseRotationDamping = true;

    /// <summary>
    /// Speed at which the camera rotation interpolates toward the target angles.
    /// Higher values result in faster response with less inertia.
    /// Lower values create heavier, smoother motion characteristic of fluid heads.
    /// </summary>
    [Export]
    public float RotationDampingSpeed = 12f;

    [ExportGroup("Object")]
    /// <summary>
    /// Reference to PNJ to find.
    /// </summary>
    [Export]
    public Sprite3D? PNJ;

    /// <summary>
    /// Last found state of PNJ.
    /// </summary>
    private bool _isPNJFound = false;

    /// <summary>
    /// Robin's textures.
    /// </summary>
    Texture2D robinTexture = GD.Load<Texture2D>("res://Art/Minigame/Picture/robin.png");
    Texture2D outlinedRobinTexture = GD.Load<Texture2D>(
        "res://Art/Minigame/Picture/OutlinedRobin.png"
    );

    /* -----------------------------
     * State
     * ----------------------------- */

    private float yaw;
    private float pitch;

    private float targetYaw;
    private float targetPitch;

    private float targetFov;

    /* -----------------------------
     * Lifecycle
     * ----------------------------- */

    public override void _Ready()
    {
        Vector3 euler = GlobalTransform.Basis.GetEuler();

        yaw = targetYaw = euler.Y;
        pitch = targetPitch = euler.X;

        targetFov = Fov;
    }

    /* -----------------------------
     * Input
     * ----------------------------- */

    public override void _Input(InputEvent e)
    {
        // move camera with the right button
        if (e is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Right))
        {
            float sensitivity = GetScaledSensitivity();

            // The higher the zoom, the slower the rotation
            targetYaw -= motion.Relative.X * sensitivity;
            targetPitch -= motion.Relative.Y * sensitivity;

            targetPitch = Mathf.Clamp(
                targetPitch,
                Mathf.DegToRad(MinPitch),
                Mathf.DegToRad(MaxPitch)
            );
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // zoom inputs
        if (e is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
                targetFov = Mathf.Clamp(targetFov - ZoomStep, MinFov, MaxFov);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                targetFov = Mathf.Clamp(targetFov + ZoomStep, MinFov, MaxFov);
        }
    }

    /* -----------------------------
     * Update
     * ----------------------------- */

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Smooth rotation
        if (UseRotationDamping)
        {
            yaw = Mathf.Lerp(yaw, targetYaw, dt * RotationDampingSpeed);
            pitch = Mathf.Lerp(pitch, targetPitch, dt * RotationDampingSpeed);
        }
        else
        {
            yaw = targetYaw;
            pitch = targetPitch;
        }

        // Smooth zoom (lens-like)
        Fov = Mathf.Lerp(Fov, targetFov, dt * ZoomDamping);

        ApplyRotation();
        IsPNJFound();
    }

    /// <summary>
    /// Check if PNJ is close and inside the camera HUD.
    /// </summary>
    private void IsPNJFound()
    {
        if (PNJ == null)
            return;

        if (IsPositionBehind(PNJ.GlobalPosition))
        {
            return;
        }

        // 3D -> screen 2D projection
        Vector2 screenPos = UnprojectPosition(PNJ.GlobalPosition);
        Vector2 viewportCenter = GetViewport().GetVisibleRect().Size * 0.5f;

        Vector3 toSprite = PNJ.GlobalPosition - GlobalPosition;

        // Camera forward direction (negative Z in camera space)
        float depth = toSprite.Dot(-GlobalTransform.Basis.Z);
        float normalizedDepth = Mathf.Clamp((depth - Near) / (Far - Near), 0f, 1f);

        // to assess whether the NPC is sufficiently visible
        Vector2 spriteScreenSize = GetSpriteScreenSize(PNJ, this);

        GD.Print($"normalizedDepth {normalizedDepth}");
        GD.Print($"distance {screenPos.DistanceTo(viewportCenter)} an depth {depth}");
        GD.Print($"Sprite screen size: {spriteScreenSize}");

        _isPNJFound = screenPos.DistanceTo(viewportCenter) <= 50.0f && spriteScreenSize.X > 90.0f;

        HighlightPNJ(_isPNJFound);
    }

    /// <summary>
    /// Calculates the actual size on screen (pixels) of a Sprite3D,
    /// by projecting its 3D volume through Camera3D.
    /// The results takes into account the FOV, distance, orientation
    /// and scale of the sprite.
    /// </summary>
    /// <param name="sprite">Sprite to measure</param>
    /// <param name="camera"></param>
    /// <returns>Sprite's size on screen (pixels)</returns>
    private static Vector2 GetSpriteScreenSize(Sprite3D sprite, Camera3D camera)
    {
        // Bounding box of Sprite3D
        Aabb localAabb = sprite.GetAabb();
        Transform3D globalTransform = sprite.GlobalTransform;

        // Describe a 2D bounding rect (screen min/max)
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);

        // 8 bouding box corners
        Vector3 pos = localAabb.Position;
        Vector3 size = localAabb.Size;
        Vector3[] corners =
        {
            pos,
            pos + new Vector3(size.X, 0, 0),
            pos + new Vector3(0, size.Y, 0),
            pos + new Vector3(size.X, size.Y, 0),
            pos + new Vector3(0, 0, size.Z),
            pos + new Vector3(size.X, 0, size.Z),
            pos + new Vector3(0, size.Y, size.Z),
            pos + size,
        };

        // Unproject every corner of AABB to 2D screen
        foreach (Vector3 corner in corners)
        {
            Vector3 worldPoint = globalTransform * corner;

            // Ignore point behind camera
            if (camera.IsPositionBehind(worldPoint))
                continue;

            // 3D -> screen 2D projection
            Vector2 screenPoint = camera.UnprojectPosition(worldPoint);

            // Update bounding 2D rect
            min = min.Min(screenPoint);
            max = max.Max(screenPoint);
        }

        // if unprojection fails for all points, the sprite is not in
        // camera field
        if (min.X == float.MaxValue)
            return Vector2.Zero;

        // Size used in screen
        return max - min;
    }

    /// <summary>
    /// Visually indicate whether the NPC is detected.
    /// </summary>
    /// <param name="detected"></param>
    private void HighlightPNJ(bool detected)
    {
        if (PNJ == null)
            return;

        if (detected)
        {
            PNJ.Texture = outlinedRobinTexture;
        }
        else
        {
            PNJ.Texture = robinTexture;
        }
    }

    /* -----------------------------
     * Helpers
     * ----------------------------- */

    /// <summary>
    /// Rotation, like a camera on a tripod.
    /// </summary>
    private void ApplyRotation()
    {
        // Yaw around vertical world axis
        Basis yawBasis = Basis.Identity.Rotated(Vector3.Up, yaw);

        // Real hozontal axis (yaw)
        Vector3 right = yawBasis.X;

        // Pitch around this axis
        Basis pitchBasis = Basis.Identity.Rotated(right, pitch);

        Basis finalBasis = pitchBasis * yawBasis;

        GlobalTransform = new Transform3D(finalBasis, GlobalTransform.Origin);
    }

    /// <summary>
    /// Ensures that the camera's angular velocity is proportional to its field of view.
    /// It transforms a simple mathematical rotation to a realistic optical behavior of a photo camera.
    /// </summary>
    /// <returns></returns>
    private float GetScaledSensitivity()
    {
        float fovFactor = Mathf.DegToRad(Fov) / Mathf.DegToRad(90f);
        return BaseSensitivity * fovFactor;
    }
}
