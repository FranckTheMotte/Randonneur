using Godot;

public partial class Player : CharacterBody2D
{
    /* Coefficient of speed */
    [Export]
    public int Walk = 1;

    [Export]
    public Sol sol;

    [Signal]
    public delegate void TrailJunctionChoiceEventHandler();

    [Export]
    public Level1 level;

    public const float Speed = 100.0f;
    public Godot.Vector2 worldLimit;

    public static Player Instance { get; private set; }

    // next junction index
    private int m_junctionIndex = -1;

    // next junction
    private GpxTrailJunction m_junction = null;

    public override void _Ready()
    {
        Instance = this;

        // not all trails got a junction
        if (sol.CurrentTrack.m_trailJunctions.Count > 0)
        {
            // start from first index
            m_junctionIndex = 0;
            m_junction = (GpxTrailJunction)sol.CurrentTrack.m_trailJunctions[m_junctionIndex];
        }
        // the player can climb all
        FloorBlockOnWall = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        Godot.Vector2 velocity = Velocity;

        // Don't fall
        if (this.Position.X >= 0 && this.Position.X < worldLimit.X)
        {
            /* Does the player reach a trail junction ? */
            if (m_junction != null && m_junction.distance - this.Position.X < 0 && Walk != 0)
            {
                /* For the moment, just stop walking */
                Walk = 0;

                /* Update destinations list on sign */
                GD.Print($"trail junction index: {m_junctionIndex}");
                level.EmitSignal(Level1.SignalName.TrailJunctionChoice, m_junctionIndex);
                m_junctionIndex++;
                // remains some junctions?
                if (m_junctionIndex < sol.CurrentTrack.m_trailJunctions.Count)
                {
                    m_junction = (GpxTrailJunction)
                        sol.CurrentTrack.m_trailJunctions[m_junctionIndex];
                }
                else
                {
                    m_junction = null;
                }
            }
        }

        // Add the gravity.
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }
        else
        {
            velocity.X = Walk * Speed;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void _on_trail_junction_choice(string gpxFile)
    {
        level.EmitSignal(Level1.SignalName.TrailJunctionChoiceDone, gpxFile);
        this.Position = new Godot.Vector2(this.Position.X + 1, this.Position.Y);
        Walk = 1;
    }
}
