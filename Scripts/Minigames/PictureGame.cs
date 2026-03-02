using System.Numerics;
using Godot;
using NUnit.Framework.Constraints;
using Randonneur;
using Randonneur.Scripts;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

/// <summary>
/// Implement the picture game where the goal is to find and shot an element hidden
/// in the landscape.
/// This class will :
/// - take camera picture
/// - find the element in the shot
/// - count elapsed time
/// </summary>
public partial class PictureGame : Node2D
{
    /// <summary>
    /// Duration of picture move in seconds.
    /// </summary>
    private const float PICTURE_MOVE_DURATION = 1.0f;

    /// <summary>
    /// Scale value to apply on picture for the destination.
    /// </summary>
    private const float PICTURE_SCALE = 0.4f;

    /// <summary>
    /// Node to camera container.
    /// </summary>
    private SubViewportContainer? _cameraViewPortContainer;

    /// <summary>
    /// Node to camera viewport.
    /// </summary>
    private SubViewport? _cameraViewPort = null;

    /// <summary>
    /// Link to HUD canvas layer.
    /// </summary>
    private CanvasLayer? _HUDLayer = null;

    /// <summary>
    /// Node destination of last picture.
    /// </summary>
    private Sprite2D? _picture = null;

    /// <summary>
    /// Link to HUD canvas layer.
    /// </summary>
    private PhotoCameraController? _gameCamera = null;

    /// <summary>
    /// Texture rects to contain the picture.
    /// Two rects are required to keep last displayed when a new picture
    /// is shot.
    /// </summary>
    private TextureRect _pictureTexture = new();
    private TextureRect _pictureTexture2 = new();

    /// <summary>
    /// Next unused picture, in order to remove from parent when a new
    /// picture taken (and move is done).
    /// </summary>
    private TextureRect? _unusedPicture;

    /// <summary>
    /// This flag block picture during move.
    /// </summary>
    bool _moveFinished = true;
    private static System.Threading.Mutex _moveMutex = new();

    /// <summary>
    /// last picture note;
    /// </summary>
    private int _pictureNote = 0;

    /// <summary>
    /// Star textures.
    /// </summary>
    private Texture2D _starOnTexture = GD.Load<Texture2D>("res://Art/UI/starOn.png");
    private Texture2D _starOffTexture = GD.Load<Texture2D>("res://Art/UI/starOff.png");
    private TextureRect?[] _starTextures = new TextureRect[Global.PictureGameMaxStars];

    /// <summary>
    /// Const.
    /// </summary>
    private const float _starScale = 0.25f;
    private const float _starSpacing = 10.0f;

    public override void _Ready()
    {
        _cameraViewPortContainer = GetNode<SubViewportContainer>("CameraContainer");
        _cameraViewPort = GetNode<SubViewport>("CameraContainer/CameraViewport");
        _cameraViewPort.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        _picture = GetNode<Sprite2D>("Picture");
        _HUDLayer = GetNode<CanvasLayer>("CameraContainer/CameraViewport/HUDLayer");
        _gameCamera = _cameraViewPort.GetNode<PhotoCameraController>(
            "PictureGameHill/PlayerPosition/Camera"
        );

        // Gap width between stars
        float starWidthGap = _starOnTexture.GetWidth() * _starScale + _starSpacing;
        // Height position of top left corner of a star, here just under the picture
        float startHeightPosition = _cameraViewPort.Size.Y * PICTURE_SCALE + 4;

        // allocate stars without texture
        for (int i = 0; i < _starTextures.Length; i++)
        {
            // display the note with stars
            TextureRect starTrect = new()
            {
                Scale = new Vector2(_starScale, _starScale),
                ZIndex = Global.ZIndexUILayer1, // over the picture
                Position = new Vector2(i * starWidthGap, startHeightPosition),
            };
            _starTextures[i] = starTrect;
            _picture.AddChild(starTrect);
        }
    }

    public override async void _Input(InputEvent @event)
    {
        if (
            _cameraViewPort == null
            || _picture == null
            || _cameraViewPortContainer == null
            || _HUDLayer == null
            || _gameCamera == null
        )
        {
            GD.PushError("_Input(): Sanity check failed.");
            return;
        }

        if (Visible == false)
        {
            GD.Print("Game window is hidden, ignore the event.");
            return;
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            switch (mouseEvent.ButtonIndex)
            {
                // take a picture with left button
                case MouseButton.Left:
                    if (_moveFinished == false)
                        return;
                    _moveMutex.WaitOne();
                    _moveFinished = false;

                    // hide HUD to no put in screenshot
                    // it's required to wait the next processed frame and
                    // next post frame for subviewport
                    _HUDLayer.Visible = false;
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    _ = await ToSignal(
                        RenderingServer.Singleton,
                        RenderingServer.SignalName.FramePostDraw
                    );

                    // clear previous stars
                    RemoveStars();

                    // Retrieve picture note
                    _pictureNote = _gameCamera.Note;

                    // prepare for screenshot
                    TextureRect t;
                    Image screenshot = _cameraViewPort.GetTexture().GetImage();

                    // HUD can be displayed now (will be processed at next frame)
                    _HUDLayer.Visible = true;

                    // Save texture in target node
                    ImageTexture imageTexture = new();
                    imageTexture.SetImage(screenshot);
                    if (_pictureTexture.GetParent() == null)
                    {
                        _pictureTexture.Texture = imageTexture;
                        t = _pictureTexture;
                        _unusedPicture = _pictureTexture2;
                        AddChild(_pictureTexture);
                    }
                    else
                    {
                        _pictureTexture2.Texture = imageTexture;
                        t = _pictureTexture2;
                        _unusedPicture = _pictureTexture;
                        AddChild(_pictureTexture2);
                    }

                    // smooth screenshot move
                    _moveMutex.ReleaseMutex();
                    ImageSmoothMove(
                        t,
                        _cameraViewPortContainer.Position,
                        _picture.Position,
                        PICTURE_SCALE,
                        PICTURE_MOVE_DURATION
                    );

                    // For test purpose
                    TemplateLevel level = GetNode<TemplateLevel>("/root/20_Level1");
                    level?.EmitSignal(TemplateLevel.SignalName.UpdateGrumblingLevel, -10);
                    break;
            }
        }
    }

    /// <summary>
    /// Move a TextureRect from a position to a target position. During movement
    /// the texture will be rescale.
    /// </summary>
    /// <param name="t">A TextureRect</param>
    /// <param name="src">Starting position</param>
    /// <param name="dest">Destination position</param>
    /// <param name="scale">Scale factor</param>
    /// <param name="duration">Duration in seconds of the move animation</param>
    private void ImageSmoothMove(
        TextureRect t,
        Vector2 src,
        Vector2 dest,
        float scale,
        float duration = 1.0f
    )
    {
        t.Position = src;

        Tween tween = CreateTween();
        tween.Parallel().TweenProperty(t, "scale", new Vector2(scale, scale), duration);
        tween
            .Parallel()
            .TweenProperty(t, "position", dest, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);

        tween.Finished += ImageAnimationFinished;
    }

    /// <summary>
    /// Called when image animation (tween) is finished.
    /// </summary>
    private void ImageAnimationFinished()
    {
        // nothing to do
        if (_unusedPicture == null)
            return;

        if (_unusedPicture.GetParent() != null)
        {
            // reset the scaling
            _unusedPicture.Scale = new Vector2(1.0f, 1.0f);
            RemoveChild(_unusedPicture);
        }

        AddStars(_pictureNote);

        _moveMutex.WaitOne();
        _moveFinished = true;
        _moveMutex.ReleaseMutex();
    }

    /// <summary>
    /// Add stars over the picture depending of the note.
    /// </summary>
    /// <param name="note">note from 0 to 10.</param>
    private void AddStars(int note)
    {
        // 0 to 10 allowed
        note = Mathf.Clamp(note, 0, Global.PictureGameMaxNote);

        int nbStars = note * Global.PictureGameMaxStars / Global.PictureGameMaxNote;;

        for (int i = 0; i < _starTextures.Length; i++)
        {
            if (_starTextures[i] is TextureRect star)
                star.Texture = i < nbStars ? _starOnTexture : _starOffTexture;
        }
    }

    /// <summary>
    /// Remove previous stars (remove texture).
    /// </summary>
    private void RemoveStars()
    {
        for (int i = 0; i < _starTextures.Length; i++)
        {
            if (_starTextures[i] is TextureRect star)
            {
                star.Texture = null;
            }
        }
    }
}
