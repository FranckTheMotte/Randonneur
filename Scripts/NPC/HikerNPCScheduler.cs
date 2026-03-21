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
        private const double _TIMER_INTERVAL = 100;

        /// <summary>
        /// Maximum number of hikers.
        /// </summary>
        private const uint _NB_HIKERS = 1;

        /// <summary>
        /// Update timer that advances hiker activity.
        /// </summary>
        private System.Timers.Timer _updateHikerTimer = new(_TIMER_INTERVAL);

        /// <summary>
        /// Last level used by player.
        /// </summary>
        private TemplateLevel _lastPlayerLevel = new();

        /// <summary>
        /// POC: An arbitrary route.
        /// </summary>
        private readonly string[] _routeOne =
        [
            "Mantet",
            "Port du C++",
            "Col de l'embarqué",
            "Col du sanglier",
            "Port du C++",
        ];

        public HikerNPCScheduler(Dictionary<string, Level> trailScenes)
        {
            _trailScenes = trailScenes;

            Random random = new(DateTime.Now.Millisecond);
            Waypoints waypoints = (Waypoints)Waypoints.Instance;

            if (waypoints == null)
            {
                throw new InvalidOperationException("Failed to get Waypoints.");
            }

            // POC: it's a predefined trace.
            String gpx = "traceE.gpx";

            Dictionary<string, Level>.KeyCollection gpxKeyList = trailScenes.Keys;
            for (int i = 0; i < _NB_HIKERS; i++)
            {
                PackedScene hikerScene = GD.Load<PackedScene>("res://Scenes/HikerNPC.tscn");
                _hikerNpcs[i] = hikerScene.Instantiate<HikerNpc>();
                _hikerNpcs[i].Init(_trailScenes, gpx, _routeOne);
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
        /// Called every _TIMER_INTERVAL seconds to update the NPC position.
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

            // handle every hikers
            for (int i = 0; i < _NB_HIKERS; i++)
            {
                if (_hikerNpcs[i].CurrentLevel == null)
                    continue;

                Level hikerLevel = _hikerNpcs[i].CurrentLevel!;

                bool sameScene = currentPlayerLevel.CurrentTraceName == hikerLevel.TraceName;
                if (!_hikerNpcs[i].InPlayerLevel && sameScene)
                {
                    GD.Print($"Hiker {i} and PLAYER are now in the same scene");
                    _hikerNpcs[i].AddToLevel(currentPlayerLevel);
                }
                else if (!sameScene)
                {
                    _hikerNpcs[i].UpdatePosition(_TIMER_INTERVAL, true);
                }
            }
            _lastPlayerLevel = currentPlayerLevel;
        }
    }
}
