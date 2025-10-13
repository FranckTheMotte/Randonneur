using Godot;
using XmlGpx;

public partial class DestinationsList : VBoxContainer
{
    [Export]
    public Label? Location;

    [Export]
    public HBoxContainer? Destination;

    [Signal]
    public delegate void DestinationsUpdateEventHandler();

    // Called when the node enters the scene tree for the first time.
    public void populateDestination(GpxTrailJunction trailJunction)
    {
        // Sanity checks
        if (Location == null || Destination == null || trailJunction.Destinations == null)
        {
            GD.PushWarning($"${nameof(populateDestination)}: sanity checks failed");
            return;
        }

        // Sign title
        Location.Text = trailJunction.Name;

        // Clean previous destinations
        for (int i = 0; i < this.GetChildCount(); i++)
        {
            this.RemoveChild(this.GetChild(1));
        }

        // Populate with new ones
        foreach (GpxDestination destination in trailJunction.Destinations)
        {
            HBoxContainer newDest = (HBoxContainer)Destination.Duplicate();
            DestinationButton destButton = (DestinationButton)newDest.GetChild(0);
            destButton.Text = destination.Name;
            destButton.GpxFile = destination.GpxFile;
            GD.Print($"dest button : {destButton.Text}");
            Label distanceLabel = (Label)newDest.GetChild(1);
            distanceLabel.Text = destination.Distance.ToString() + " km";
            Label trailLabel = (Label)newDest.GetChild(3);
            trailLabel.Text = destination.Trail;
            newDest.Show();
            this.AddChild(newDest);
        }
        // Hide the template
        Destination.Hide();

        // Adapt the sign size
        NinePatchRect backgroundSign = (NinePatchRect)this.GetParent().GetParent();
        HBoxContainer dest = (HBoxContainer)this.GetChild(1);
        // margin H up + margin H low + title Height + nb dest * Height TODO remove hardcoded values
        float height =
            (15 * 2) + 32 + (trailJunction.Destinations.Count + 1) * dest.CustomMinimumSize.Y;
        backgroundSign.CustomMinimumSize = new Vector2(256, height);
    }

    private void _on_ready() { }

    private void _on_destinations_update(GpxTrailJunction trailJunction)
    {
        populateDestination(trailJunction);
    }
}
