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

    public const float MinZoomStep = 3.0f;

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

    [ExportGroup("Hand Shake")]
    [Export]
    public bool EnableHandShake = true;

    /// <summary>
    /// Base angular amplitude in degrees at wide FOV.
    /// </summary>
    [Export]
    public float ShakeAmplitude = 0.05f;

    /// <summary>
    /// How fast the shake evolves over time. Will be impacted with stress.
    /// </summary>
    [Export]
    public float ShakeFrequency = 1.5f;

    /// <summary>
    /// How much zoom amplifies the shake.
    /// </summary>
    [Export]
    public float ZoomShakeMultiplier = 2.0f;

    [ExportGroup("Object")]
    /// <summary>
    /// Reference to hidden NPC.
    /// </summary>
    [Export]
    public HiddenNPC? HiddenNPC;

    [ExportGroup("Debug")]
    /// <summary>
    /// Debug activation flag.
    /// </summary>
    [Export]
    public bool DebugActivated = false;

    /// <summary>
    /// Logs on screen.
    /// </summary>
    [Export]
    public Label? LogScreenLabel;

    /// <summary>
    /// Last found state of NPC.
    /// </summary>
    private bool _isNPCFound = false;

    /* -----------------------------
     * States
     * ----------------------------- */

    private float _yaw;
    private float _pitch;

    private float _targetYaw;
    private float _targetPitch;

    private float _targetFov;

    /// <summary>
    /// Cached screenSize.
    /// </summary>
    Vector2 _screenSize;

    /// <summary>
    /// the middle of the current viewport.
    /// </summary>
    Vector2 _viewportCenter;

    /// <summary>
    /// Debug stuffs.
    /// </summary>
    private DebugOverlay? _debugOverlay;

    private FastNoiseLite? _shakeNoise;

    /// <summary>
    /// Shake time reference
    /// </summary>
    private float _shakeTime = 0f;

    private readonly Random _rand = new();

    /* -----------------------------
     * Public properties
     * ----------------------------- */

    public int Note { get; private set; } = 0;

    /* -----------------------------
     * Lifecycle
     * ----------------------------- */

    public override void _Ready()
    {
        if (HiddenNPC == null)
        {
            GD.PushError("_Ready(): sanity check failed.");
            return;
        }

        Vector3 euler = GlobalTransform.Basis.GetEuler();

        _yaw = _targetYaw = euler.Y;
        _pitch = _targetPitch = euler.X;

        _targetFov = Fov;

        _screenSize = GetViewport().GetVisibleRect().Size;

        // get the middle of the current viewport
        _viewportCenter = GetViewport().GetVisibleRect().Size * 0.5f;

        _shakeNoise = new() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin, Frequency = 1.0f };

        if (ZoomStep < MinZoomStep)
            ZoomStep = MinZoomStep;

        _debugOverlay = GetNode<DebugOverlay>("DebugOverlay");
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
            _targetYaw -= motion.Relative.X * sensitivity;
            _targetPitch -= motion.Relative.Y * sensitivity;

            _targetPitch = Mathf.Clamp(
                _targetPitch,
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
            float zoomStep = (ZoomStep - 2) + 2 * (Fov / MaxFov);
            if (mb.ButtonIndex == MouseButton.WheelUp)
                _targetFov = Mathf.Clamp(_targetFov - zoomStep, MinFov, MaxFov);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                _targetFov = Mathf.Clamp(_targetFov + zoomStep, MinFov, MaxFov);
        }
    }

    /* -----------------------------
     * Update
     * ----------------------------- */

    public override void _Process(double delta)
    {
        float roundedDelta = (float)delta;

        // Smooth rotation
        if (UseRotationDamping)
        {
            _yaw = Mathf.Lerp(_yaw, _targetYaw, roundedDelta * RotationDampingSpeed);
            _pitch = Mathf.Lerp(_pitch, _targetPitch, roundedDelta * RotationDampingSpeed);
        }
        else
        {
            _yaw = _targetYaw;
            _pitch = _targetPitch;
        }

        // Handshaking
        if (EnableHandShake && _shakeNoise != null)
        {
            _shakeTime += roundedDelta * ShakeFrequency;

            float zoomFactor = Mathf.Lerp(
                1f,
                ZoomShakeMultiplier,
                Mathf.InverseLerp(MaxFov, MinFov, Fov)
            );

            float noiseYaw = _shakeNoise.GetNoise1D(_shakeTime);
            float noisePitch = _shakeNoise.GetNoise1D(_shakeTime + 100f);

            float shakeYaw = Mathf.DegToRad(ShakeAmplitude * zoomFactor) * noiseYaw;
            float shakePitch = Mathf.DegToRad(ShakeAmplitude * zoomFactor) * noisePitch;

            // apply
            _yaw += shakeYaw;
            _pitch += shakePitch;
        }

        // Smooth zoom (lens-like)
        Fov = Mathf.Lerp(Fov, _targetFov, roundedDelta * ZoomDamping);

        ApplyRotation();
        IsHiddenNPCFound();
    }

    /// <summary>
    /// Check if NPC is close and inside the camera HUD.
    /// </summary>
    private void IsHiddenNPCFound()
    {
        if (HiddenNPC == null)
            return;

        if (IsPositionBehind(HiddenNPC.GlobalPosition))
        {
            return;
        }

        // to assess whether the NPC is sufficiently visible
        float NPCDistanceFromCenter = HiddenNPC.DistanceFromCenter(this, _viewportCenter);

        // evaluate picture quality
        Note = 0;
        (float NPCVisiblePercent, bool NPCFullyVisible, String debuglog) =
            HiddenNPC.GetVisibilityStatus(this, _screenSize, DebugActivated ? _debugOverlay : null);

        // NPC overscreen size
        if (NPCFullyVisible)
        {
            if (NPCVisiblePercent >= 66.0f)
                Note = 5;
            else if (NPCVisiblePercent >= 33.0f && NPCVisiblePercent < 66.0f)
                Note = 3;
            else if (NPCVisiblePercent >= 10.0f && NPCVisiblePercent < 33.0f)
                Note = 1;
        }
        else
        {
            if (NPCVisiblePercent >= 90.0f)
                Note = 3;
            else if (NPCVisiblePercent >= 25.0f && NPCVisiblePercent < 90.0f)
                Note = 1;
        }

        if (Note > 0)
        {
            // NPC centered
            if (NPCDistanceFromCenter <= 0.001)
                Note += 5;
            else if (NPCDistanceFromCenter > 0.001 && NPCDistanceFromCenter <= 0.02)
                Note += 2;
            else if (NPCDistanceFromCenter > 0.02 && NPCDistanceFromCenter <= 0.06)
                Note += 1;
        }

        // debug ?
        if (DebugActivated && LogScreenLabel != null)
        {
            LogScreenLabel.Text =
                $"Note : {Note}\n"
                + $"NPC VisiblePercent {NPCVisiblePercent}\n"
                + $"NPC DistanceFromCenter {NPCDistanceFromCenter}\n"
                + $"Fov {Fov}\n"
                + debuglog;
        }

        _isNPCFound = NPCDistanceFromCenter < 0.001f && NPCVisiblePercent > 5.0f;

        HiddenNPC.Focus(_isNPCFound);
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
        Basis yawBasis = Basis.Identity.Rotated(Vector3.Up, _yaw);

        // Real hozontal axis (yaw)
        Vector3 right = yawBasis.X;

        // Pitch around this axis
        Basis pitchBasis = Basis.Identity.Rotated(right, _pitch);

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
        float t = Mathf.InverseLerp(MaxFov, MinFov, Fov);
        t = Mathf.SmoothStep(0f, 1f, t);
        float to = (float)_rand.NextDouble() * 0.6f;
        float multiplier = Mathf.Lerp(1f, 1f + to, t);
        return BaseSensitivity * multiplier;
    }
}
