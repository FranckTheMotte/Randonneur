using Godot;

namespace Randonneur;

/// <summary>
/// Intermediate class to add some generical graphics for
/// any waypoint
/// </summary>
public class GfxWaypoint : Waypoint
{
    /// <summary>
    /// Gfx of waypoint (TODO unstuck from junction type).
    /// </summary>
    public MapJunctionArea MapJunctionGfx = new();

    /// <summary>
    /// Displayed name.
    /// </summary>
    public readonly Label Label = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Displayed name.</param>
    public GfxWaypoint(string Name)
        : base(Name)
    {
        Label.Name = Label.Text = Name;
        Label.ZIndex = 2;
        Label.LabelSettings = new LabelSettings { FontColor = Colors.Black };
    }
}
