using System.Timers;
using Godot;

public partial class GameTime : Label
{
    /// <summary>
    /// Singleton.
    /// </summary>
    private static GameTime? _instance;
    public static GameTime? Instance { get; private set; } = _instance;

    private const double _TIMER_INTERVAL = 1000;

    private const int _GAME_TIMER_INTERVAL = 15;

    private const int _1H_SECONDS = 60 * 60;

    private const int _24H_SECONDS = 24 * _1H_SECONDS;

    /// <summary>
    /// Update timer that advances game clock.
    /// </summary>
    private System.Timers.Timer _updateTimer = new(_TIMER_INTERVAL);

    /// <summary>
    /// Current game time (HH:MM).
    /// </summary>
    private string _time = "08:00";

    /// <summary>
    /// Current game time (seconds).
    /// </summary>
    private int currentSecond = 8 * _1H_SECONDS;

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public override void _Ready()
    {
        Instance = this;

        // not visible at start
        Visible = false;

        InitTimer();
        Update();
    }

    /// <summary>
    /// Initialize timer event.
    /// </summary>
    private void InitTimer()
    {
        if (_updateTimer == null)
        {
            GD.PushError("InitTimer(): update time is not available");
            return;
        }

        // start now
        _updateTimer.Elapsed += OnGameTimerEvent;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    /// <summary>
    /// Called every _TIMER_INTERVAL seconds to update the current game time and
    /// to display ingame.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGameTimerEvent(object? sender, ElapsedEventArgs e)
    {
        currentSecond += _GAME_TIMER_INTERVAL;
        if (currentSecond >= _24H_SECONDS)
        {
            currentSecond = 0;
        }

        TimeSpan t = TimeSpan.FromSeconds(currentSecond);
        _time = string.Format("{0:D2}:{1:D2}", t.Hours, t.Minutes);

        _ = CallDeferred("Update");
    }

    /// <summary>
    /// Update the label value with current game time.
    /// </summary>
    private void Update()
    {
        Text = _time;
    }
}
