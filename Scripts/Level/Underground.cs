using Godot;

public partial class Underground : Polygon2D
{
    public override void _Ready()
    {
        // Assign a repeating rock texture
        Texture = GD.Load<Texture2D>("res://Art/Background/undergroundRock.png");
        TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
        TextureScale = new Vector2(2.0f, 2.0f);
    }
}
