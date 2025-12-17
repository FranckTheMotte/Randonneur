using System;
using Godot;
using Randonneur;

/// <summary>
/// A flower is an animated object that can be placed in the game world.
/// </summary>
public partial class Flower : Area2D
{
    [Export]
    public Sprite2D? FlowerSprite;

    public override void _Ready()
    {
        // Material must be unique
        if (FlowerSprite != null && FlowerSprite.Material != null)
        {
            FlowerSprite.Material = (Material)FlowerSprite.Material.Duplicate();
        }
    }

    /// <summary>
    /// Called when the flower's body area is entered by another node.
    /// The function will make the flower bend and return to its original position.
    /// </summary>
    /// <param name="Body">The entering node.</param>
    void _on_body_entered(Node2D Body)
    {
        // TODO: tests values, to update
        int skewValue = 500;
        float bendGrassAnimationSpeed = 0.3f;
        float grassReturnAnimationSpeed = 5.0f;

        if (FlowerSprite == null)
        {
            GD.PushWarning("No flower, no collision");
            return;
        }

        if (Body == GetTree().GetFirstNodeInGroup(Global.PlayerGroup))
        {
            Vector2 direction = GlobalPosition.DirectionTo(Body.GlobalPosition);
            int skew = (int)(-direction.X * (float)skewValue);

            // Start oscilltion
            Tween tween = CreateTween();
            tween
                .TweenProperty(
                    FlowerSprite,
                    "material:shader_parameter/skew",
                    skew,
                    bendGrassAnimationSpeed
                )
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            // Add some random movement
            Random minStrengh = new();
            tween
                .TweenProperty(
                    FlowerSprite,
                    "material:shader_parameter/minStrength",
                    minStrengh.NextDouble() * 0.3f,
                    0.0
                )
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            tween
                .TweenProperty(
                    FlowerSprite,
                    "material:shader_parameter/skew",
                    0.0,
                    grassReturnAnimationSpeed
                )
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Elastic);
        }
    }
}
