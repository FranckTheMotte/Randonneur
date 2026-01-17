using System.Numerics;
using Godot;
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
public partial class PictureGame : Control
{
    /// <summary>
    /// Node to camera viewport.
    /// </summary>
    private SubViewport? _cameraViewPort = null;

    /// <summary>
    /// Node destination of last picture.
    /// </summary>
    private TextureRect? _pictureRect = null;

    /// <summary>
    /// Region inside HUD camera (where picture must be shoot).
    /// </summary>
    private Rect2I _captureRegion;

    public override void _Ready()
    {
        _cameraViewPort = GetNode<SubViewport>(
            "GameContainer/BgNinePathRect/MapMargin/MapRect/HBoxContainer/CameraContainer/CameraViewport"
        );

        _cameraViewPort.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        _pictureRect = GetNode<TextureRect>(
            "GameContainer/BgNinePathRect/MapMargin/MapRect/HBoxContainer/PictureRect"
        );

        Node2D cameraHUD = GetNode<Node2D>("CameraHUD");

        Sprite2D HUD = cameraHUD.GetNode<Sprite2D>("HUD");
        Vector2I _HUDSize = (Vector2I)(HUD.Texture.GetSize() * HUD.Scale);
        // Capture only inside the camera HUD
        _captureRegion = new(
            (_cameraViewPort.Size.X - _HUDSize.X) / 2,
            (_cameraViewPort.Size.Y - _HUDSize.Y) / 2,
            _HUDSize.X,
            _HUDSize.Y
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (_cameraViewPort == null || _pictureRect == null)
            return;

        if (@event is InputEventMouseButton mouseEvent)
        {
            // take a picture with left button
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                Image screenshot = _cameraViewPort.GetTexture().GetImage();
                ImageTexture imageTexture = new();
                imageTexture.SetImage(screenshot.GetRegion(_captureRegion));
                imageTexture.SetSizeOverride((Vector2I)_pictureRect.Size);
                _pictureRect.Texture = imageTexture;
            }
        }
    }
}
