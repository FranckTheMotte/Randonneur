using System.Collections.ObjectModel;
using System.Timers;
using Godot;
using Randonneur.Scripts;

namespace Randonneur
{
    /// <summary>
    /// Class to handle the Hiker NPC over trails.
    /// </summary>
    public class HikerNPCScheduler
    {
        /// <summary>
        /// Link to all the Scenes (trails) with gpx file name as the key and the level
        /// as the value.
        /// @see SceneManager
        /// </summary>
        private readonly Dictionary<string, Level> _trailScenes = [];

        private const int MAX_NPC = 10;
        private readonly HikerNpc[] _hikerNpcs = new HikerNpc[MAX_NPC];

        /// <summary>
        /// In ms.
        /// </summary>
        private const double _TIMER_INTERVAL = 1000;

        /// <summary>
        /// Update timer that advances hiker activity.
        /// </summary>
        private System.Timers.Timer _updateHikerTimer = new(_TIMER_INTERVAL);

        /// <summary>
        /// Last level used by player.
        /// </summary>
        private TemplateLevel _lastPlayerLevel = new();

        public HikerNPCScheduler(Dictionary<string, Level> trailScenes)
        {
            _trailScenes = trailScenes;

            Random random = new(DateTime.Now.Millisecond);

            int numberNPC = 1;
            Dictionary<string, Level>.KeyCollection gpxKeyList = trailScenes.Keys;
            for (int i = 0; i < numberNPC; i++)
            {
                // Place randomly the Hiker NPCS
                String gpx = gpxKeyList.ElementAt(random.Next(0, gpxKeyList.Count));

                PackedScene hikerScene = GD.Load<PackedScene>("res://Scenes/HikerNPC.tscn");
                _hikerNpcs[i] = hikerScene.Instantiate<HikerNpc>();
                _hikerNpcs[i].CurrentLevel = _trailScenes[gpx];
                _hikerNpcs[i].Position = new Vector2(0, 44000);

                GD.Print($"Hiker {i} is placed in {gpx}");
            }

            // - define the route
            //

            // setup and launch timer to update NPCs status
            InitTimer();
        }

        /// <summary>
        /// Initialize timer event.
        /// </summary>
        private void InitTimer()
        {
            if (_updateHikerTimer == null)
            {
                GD.PushError("InitTimer(): update time is not available");
                return;
            }

            // start now
            _updateHikerTimer.Elapsed += OnHikerTimerEvent;
            _updateHikerTimer.AutoReset = true;
            _updateHikerTimer.Enabled = true;
        }

        /// <summary>
        /// Called every _TIMER_INTERVAL seconds to update the current game time and
        /// to display ingame.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHikerTimerEvent(object? sender, ElapsedEventArgs e)
        {
            // get current player level
            if (Player.Instance == null || Player.Instance.Level == null)
                return;

            Player player = Player.Instance;

            TemplateLevel currentPlayerLevel = player.Level;

            for (int i = 0; i < 1; i++)
            {
                if (_hikerNpcs[i].CurrentLevel == null)
                    continue;

                GD.Print(
                    $"_lastPlayerLevel {_lastPlayerLevel} player.CurrentWaypoint.TraceName {currentPlayerLevel.CurrentTraceName}"
                );
                // player level change
                if (_lastPlayerLevel != currentPlayerLevel)
                {
                    Level hikerLevel = _hikerNpcs[i].CurrentLevel!;
                    GD.Print(
                        $"player.CurrentWaypoint.TraceName {currentPlayerLevel.CurrentTraceName} hikerLevel.GpxFile {hikerLevel.TraceName}"
                    );
                    if (currentPlayerLevel.CurrentTraceName == hikerLevel.TraceName)
                    {
                        _hikerNpcs[i].AddToLevel(player.Level);
                    }
                    else if (_hikerNpcs[i].InPlayerLevel)
                    {
                        _hikerNpcs[i].RemoveFromLevel(_lastPlayerLevel);
                    }
                }
            }
            _lastPlayerLevel = currentPlayerLevel;
        }
    }
}
