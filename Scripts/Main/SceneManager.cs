using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using Randonneur.Scripts;

public enum eSceneNames
{
    Main = 10,
    Level1 = 20,
    Level2 = 30,
}

public partial class SceneManager : Node
{
    private readonly Dictionary<string, Level> Scenes = [];
    public Node? CurrentScene { get; set; }
    public static SceneManager? instance;

    //Update this Dictionary whenever you add or change a scene you want included in the Scene Manager.
    public Dictionary<eSceneNames, SceneCstr> sceneDictionary = new Dictionary<
        eSceneNames,
        SceneCstr
    >()
    {
        { eSceneNames.Main, new SceneCstr("res://Scenes/10_Main.tscn", "Main", false) },
        { eSceneNames.Level1, new SceneCstr("res://Scenes/20_Level1.tscn", "Level One", true) },
        { eSceneNames.Level2, new SceneCstr("res://Scenes/30_Level2.tscn", "Level Two", true) },
    };

    public override void _Ready()
    {
        instance = this;

        Viewport root = GetTree().Root;
        // Using a negative index counts from the end, so this gets the last child node of `root`.
        CurrentScene = root.GetChild(-1);

        //This will tell us that SceneManager object was included in autoload.
        GD.Print("(SceneManager) SceneManager Ready");
    }

    /// <summary>
    /// Load all scenes (gpx files) from disk and populate the Scenes dictionary.
    /// Scenes are loaded from the gpx file and connected traces are used to preload other scenes.
    /// </summary>
    /// <param name="gpxFile">Full path to the gpx file.</param>
    public void LoadScenes(string gpxFile)
    {
        // sanity checks
        if (Scenes.ContainsKey(gpxFile))
        {
            GD.PushWarning($"Scenes {gpxFile} is already loaded");
            return;
        }

        Level level = new(gpxFile);
        Dictionary<string, string> connectedTraceFiles;

        GD.Print($"LoadScenes from {gpxFile}");
        try
        {
            // create first level and check connected traces to preload other scenes.
            connectedTraceFiles = level.Create();
        }
        catch (Exception e)
        {
            GD.PushError($"Failed to load level from {e.Message}");
            return;
        }

        GD.Print($"Add {gpxFile} to Scenes");
        Scenes.Add(gpxFile, level);

        // iterate over connected traces and preload the scenes.
        foreach (KeyValuePair<string, string> item in connectedTraceFiles)
        {
            // if it was already loaded, nothing to do
            if (Scenes.ContainsKey(item.Key) == true)
                continue;

            level = new(item.Key);
            try
            {
                // create first level and ignore connected traces (no preload).
                level.Create();
            }
            catch (Exception e)
            {
                GD.PushError($"Failed to load level from {e.Message}");
                return;
            }
            GD.Print($"Add {item.Key} to Scenes");
            Scenes.Add(item.Key, level);
        }
    }

    public void ChangeScene(eSceneNames mySceneName)
    {
        string myPath = sceneDictionary[mySceneName].path;
        GameMaster.pauseAllowed = sceneDictionary[mySceneName].pauseAllowed;
        GameMaster.playerData.savedScene = mySceneName;
        GetTree().ChangeSceneToFile(myPath);
    }

    /// <summary>
    /// Change the current level to the one defined in the GpxFile.
    /// This function will defer the change of level to the next frame.
    /// </summary>
    /// <param name="GpxFile">Path to the gpx file of the level to change to.</param>
    public void ChangeLevel(string GpxFile)
    {
        // TODO could be done in Level1??
        _ = CallDeferred("DefferedChangeLevel", GpxFile);
    }

    /// <summary>
    /// Change the current level to the one defined in the GpxFile.
    /// This function is called deferred, i.e. it will be called at the end of the frame.
    /// </summary>
    /// <param name="GpxFile">Path to the gpx file of the level to change to.</param>
    public void DefferedChangeLevel(string GpxFile)
    {
        // if there is no current scene, there is nothing to change
        if (CurrentScene == null)
        {
            GD.Print("No current scene to change.");
            return;
        }

        GD.Print($"ChangeLevel {GpxFile}");

        // try to find the level in the dictionary
        if (Scenes.TryGetValue(GpxFile, out Level? level))
        {
            GD.Print($"Level {GpxFile} found!");

            // if the level is valid, change the scene
            if (level != null && level.Scene != null)
            {
                Window root = GetTree().Root;
                root.RemoveChild(CurrentScene);
                root.AddChild(level.Scene);
                CurrentScene = level.Scene;
            }
        }
    }

    //Receive notification from the Operating System's Window Manager
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            GD.Print("(SceneManager) Quit Requested by Window Manager.");

            //Save the Current Game on SceneManager and Quit
            QuitGame();
        }
    }

    //Save GameData and PlayerData then Quit
    public void QuitGame()
    {
        GD.Print("(SceneManager) Saving and Quitting");
        GameMaster.SaveGameData();

        GameMaster.SavePlayerData(GameMaster.currentSlotNum);
        GetTree().Quit();
    }
}
