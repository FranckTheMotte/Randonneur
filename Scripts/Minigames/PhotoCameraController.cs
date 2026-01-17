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
    [Export]
    public float MinFov = 5f;

    [Export]
    public float MaxFov = 90f;

    [Export]
    public float ZoomStep = 5f; // Mouse wheel step

    [Export]
    public float ZoomDamping = 10f; // Higher = snappier zoom

    [ExportGroup("Rotation")]
    [Export]
    public float BaseSensitivity = 0.002f;

    [Export]
    public float MinPitch = -85f;

    [Export]
    public float MaxPitch = 85f;

    [ExportGroup("Rotation Damping")]
    [Export]
    public bool UseRotationDamping = true;

    [Export]
    public float RotationDampingSpeed = 12f;

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
