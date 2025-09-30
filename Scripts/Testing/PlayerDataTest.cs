using Godot;

public partial class PlayerDataTest : Node
{
    [Export]
    public Label? myLabel;

    public override void _Ready()
    {
        // Sanity checks
        if (myLabel == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        myLabel.Text =
            "GetTree().CurrentScene.SceneFilePath: " + GetTree().CurrentScene.SceneFilePath + "\n";
        myLabel.Text += "GameMaster Current Slot " + GameMaster.currentSlotNum + "\n";
        myLabel.Text += "Sample Dictionary Text: " + GameMaster.playerData.sampleDictionary["test"];
    }

    /* TODO make a scrolling
     - load background
     - move circulary background with only x forward
     - move circulary background with only x forward and y variations
     */
}
