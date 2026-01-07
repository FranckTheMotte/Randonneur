using System.Threading.Tasks;
using Godot;

/// <summary>
/// Screen fader in/out for scene transition. Use of alpha channel.
/// </summary>
public partial class ScreenFader : CanvasLayer
{
    [Export]
    public float FadeDuration = 0.5f;

    private ColorRect? _rect;

    // Singleton
    public static ScreenFader? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        _rect = GetNode<ColorRect>("FadeColorRect");
    }

    /// <summary>
    /// Fade in.
    /// </summary>
    public async Task FadeIn()
    {
        if (_rect == null)
            return;
        _rect.Visible = true;
        var color = _rect.Color;
        color.A = 1f;
        _rect.Color = color;

        await FadeTo(0f);
    }

    /// <summary>
    /// Fade out.
    /// </summary>
    public async Task FadeOut()
    {
        if (_rect == null)
            return;
        _rect.Visible = true;
        await FadeTo(1f);
    }

    /// <summary>
    /// Do the fade transition.
    /// </summary>
    /// <param name="targetAlpha">Targeted alpha</param>
    /// <returns></returns>
    private async Task FadeTo(float targetAlpha)
    {
        if (_rect == null)
            return;

        float startAlpha = _rect.Color.A;
        float time = 0f;

        // update at each frame
        while (time < FadeDuration)
        {
            time += (float)GetProcessDeltaTime();
            float t = time / FadeDuration;

            var color = _rect.Color;
            color.A = Mathf.Lerp(startAlpha, targetAlpha, t);
            _rect.Color = color;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // update alpha only
        var finalColor = _rect.Color;
        finalColor.A = targetAlpha;
        _rect.Color = finalColor;
    }
}
