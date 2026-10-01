using UnityEngine;
using UnityEngine.InputSystem;

// -----------------------
// Not for you to edit. This is the gun : it puts a ball back at its muzzle, gives it a
// speed, and says afterwards whether the wall stopped it. It measures, it never fixes.
// -----------------------

public class TunnelingCourse : MonoBehaviour
{
    [System.Serializable]
    public class Lane
    {
        public string Label;

        public Transform Muzzle;

        [Tooltip("The ball that gets fired, aimed straight at Wall")]
        public Rigidbody Projectile;

        public Transform Wall;

        [Tooltip("Wall thickness along the firing axis, in metres. Only used for the " +
                 "prediction printed on each shot - it is not read back from the scale")]
        public float WallThickness = 0.1f;

        [Tooltip("Ball radius : half its scale")]
        public float ProjectileRadius = 0.25f;

        public float Speed = 50.0f;

        [Tooltip("Added to Speed after every run. The run where the verdict flips is the " +
                 "speed you were looking for")]
        public float SpeedIncrementPerRun = 0.0f;

        [Tooltip("How far past the wall the ball has to get before it counts as gone " +
                 "through, in metres")]
        public float PassMargin = 1.0f;
    }

    [SerializeField] Lane[] Lanes;

    [Tooltip("Physics step, in seconds. 0 leaves the project setting alone.\n\n" +
             "This scene runs at 0.1 - ten steps a second instead of fifty. Tunneling " +
             "comes from the distance covered in ONE step, so a long step buys the same " +
             "bug at a fifth of the speed : slow enough to watch")]
    [SerializeField] float FixedTimestep = 0.1f;

    [SerializeField] Key RestartKey = Key.R;

    [Tooltip("The run restarts by itself after this many seconds. Zero never does")]
    [SerializeField] float AutoRestartAfter = 4.0f;

    [SerializeField] bool LogToScreen = true;

    float _RunTime;
    float _PreviousFixedTimestep;

    Vector3[] _FireDirections;
    bool[] _Reported;

    void Awake()
    {
        if (Lanes == null)
        {
            return;
        }

        _FireDirections = new Vector3[Lanes.Length];
        _Reported = new bool[Lanes.Length];
    }

    // Set here rather than in Project Settings, and put back on the way out : a stray
    // Time.fixedDeltaTime outlives play mode and follows you into the next scene.
    void OnEnable()
    {
        if (FixedTimestep > 0.0f)
        {
            _PreviousFixedTimestep = Time.fixedDeltaTime;

            Time.fixedDeltaTime = FixedTimestep;
        }
    }

    void OnDisable()
    {
        if (_PreviousFixedTimestep > 0.0f)
        {
            Time.fixedDeltaTime = _PreviousFixedTimestep;

            _PreviousFixedTimestep = 0.0f;
        }
    }

    void Start()
    {
        Launch();
    }

    [ContextMenu("Launch")]
    public void Launch()
    {
        _RunTime = 0.0f;

        if (Lanes == null)
        {
            return;
        }

        for (int i = 0; i < Lanes.Length; i++)
        {
            Lane lane = Lanes[i];

            if (lane.Muzzle == null || lane.Projectile == null)
            {
                continue;
            }

            // Aimed at the wall rather than along the muzzle's rotation : one less thing
            // to get wrong when the scene is assembled.
            Vector3 direction = lane.Wall != null
                ? (lane.Wall.position - lane.Muzzle.position)
                : lane.Muzzle.forward;

            _FireDirections[i] = direction.sqrMagnitude > 0.0f
                ? direction.normalized
                : lane.Muzzle.forward;

            _Reported[i] = false;

            Place(lane.Projectile, lane.Muzzle.position, _FireDirections[i] * lane.Speed);

            if (LogToScreen)
            {
                Report(Predict(lane));
            }
        }
    }

    /// <summary>
    /// A discrete step moves the body Speed * fixedDeltaTime, then asks "am I touching
    /// anything NOW ?". Nothing looks at the space crossed on the way, so the ball is
    /// certain to miss once one step is longer than the wall plus its own diameter.
    /// </summary>
    static string Predict(Lane lane)
    {
        float stepDistance = lane.Speed * Time.fixedDeltaTime;
        float windowToMiss = lane.WallThickness + lane.ProjectileRadius * 2.0f;

        return lane.Label + " : " + lane.Speed.ToString("0.#") + " m/s"
             + "  ->  step " + stepDistance.ToString("0.###") + " m"
             + "  vs  " + windowToMiss.ToString("0.###") + " m to miss"
             + (stepDistance > windowToMiss
                    ? "   [PREDICTION : goes through]"
                    : "   [PREDICTION : stopped]");
    }

    static void Place(Rigidbody body, Vector3 position, Vector3 velocity)
    {
        body.position = position;
        body.transform.position = position;

        body.angularVelocity = Vector3.zero;
        body.linearVelocity = velocity;

        // Anything doing its own sweeping keeps a "where I was last step", and would
        // otherwise sweep the whole arena on the run after a teleport.
        body.gameObject.SendMessage("OnCourseTeleport", SendMessageOptions.DontRequireReceiver);
    }

    void Update()
    {
        _RunTime += Time.deltaTime;

        if (Lanes == null)
        {
            return;
        }

        for (int i = 0; i < Lanes.Length; i++)
        {
            Judge(i);
        }

        Keyboard keyboard = Keyboard.current;
        bool restartAsked = keyboard != null && keyboard[RestartKey].wasPressedThisFrame;

        if (restartAsked || (AutoRestartAfter > 0.0f && _RunTime >= AutoRestartAfter))
        {
            NextSpeeds();
            Launch();
        }
    }

    /// <summary>
    /// One verdict per run : the ball got past the wall, or it stopped moving forward.
    /// Both measured along the firing axis, so a ball that bounces away at an angle
    /// still counts as stopped.
    /// </summary>
    void Judge(int index)
    {
        Lane lane = Lanes[index];

        if (_Reported[index] || lane.Projectile == null || lane.Wall == null || lane.Muzzle == null)
        {
            return;
        }

        Vector3 direction = _FireDirections[index];

        float wallDistance = Vector3.Dot(lane.Wall.position - lane.Muzzle.position, direction);
        float shotDistance = Vector3.Dot(lane.Projectile.position - lane.Muzzle.position, direction);

        if (shotDistance > wallDistance + lane.WallThickness * 0.5f + lane.PassMargin)
        {
            _Reported[index] = true;

            Report(lane.Label + " : WENT THROUGH at " + lane.Speed.ToString("0.#") + " m/s"
                 + "   (" + lane.Projectile.collisionDetectionMode + ")");
            return;
        }

        bool stillGoingForward = Vector3.Dot(lane.Projectile.linearVelocity, direction) > 0.01f;

        // Give it a moment to leave the muzzle before judging it stopped.
        if (!stillGoingForward && _RunTime > 0.2f)
        {
            _Reported[index] = true;

            Report(lane.Label + " : STOPPED at " + lane.Speed.ToString("0.#") + " m/s"
                 + "   (" + lane.Projectile.collisionDetectionMode + ")");
        }
    }

    void NextSpeeds()
    {
        for (int i = 0; i < Lanes.Length; i++)
        {
            Lanes[i].Speed += Lanes[i].SpeedIncrementPerRun;
        }
    }

    static void Report(string message)
    {
        HudConsole.Log(message);
    }

    void OnDrawGizmos()
    {
        if (Lanes == null)
        {
            return;
        }

        foreach (Lane lane in Lanes)
        {
            if (lane.Muzzle == null || lane.Wall == null)
            {
                continue;
            }

            Vector3 toWall = lane.Wall.position - lane.Muzzle.position;

            Gizmos.color = Color.cyan;
            DrawUtils.DrawArrow(lane.Muzzle.position, toWall);

            // One step at the current speed, to scale. Longer than the wall is thick,
            // and nothing stops the shot.
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(lane.Muzzle.position,
                            lane.Muzzle.position + toWall.normalized * (lane.Speed * Time.fixedDeltaTime));
        }
    }
}
