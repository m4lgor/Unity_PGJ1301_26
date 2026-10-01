using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level 06. A solar wind that blows in gusts, and pushes the spaceship AWAY from the goal.
///
/// The cycle never stops :
///
///     Calm      nothing happens.
///     Warning   the direction is chosen, announced on screen, and handed to the ship's
///               controller through OnGustWarning. It will not change before the gust is over.
///     Gust      the wind pushes the ship with a constant force, along that direction.
///
/// The force is applied at the ship's centre of mass, so it never makes the ship turn.
/// It is stronger than the engines can fight - two engines together give about 28 N along
/// a diagonal, one engine 20 N - so thrusters alone cannot hold the ship still. Something
/// else has to : a tether to a wall.
///
/// The wind itself only pushes. Around it, this component also sets the rules of the level
/// for the tethers :
///
///     The walls become bodies. Each arena border gets a kinematic Rigidbody, so there is
///     something to CONNECT a joint to - a tether is tied to a wall, not to a point in the air.
///
///     Only a SpringJoint is accepted on the ship. Any other joint is removed, with an error.
///
///     A joint on the ship with no connectedBody is removed, with an error in the console.
///     That is what makes the student give connectedAnchor in the LOCAL space of the wall.
///
///     A rope the ship is far past the end of snaps (see _RopeSnapStretch) : a rope tied too
///     short would otherwise yank the ship with spring x stretch newtons.
/// </summary>
public class SolarWindHazard : MonoBehaviour
{
    public enum Phase { Calm, Warning, Gust }

    public enum DirectionMode
    {
        /// <summary>From the goal toward the ship. Falls back to a random direction when the ship is on the goal.</summary>
        AwayFromGoal,

        /// <summary>Any direction.</summary>
        Random,
    }

    [Header("Cycle")]
    [Tooltip("Seconds of calm before the first warning. 0 : the first warning comes on the very first frame")]
    [SerializeField] private float _FirstCalmSeconds = 0.0f;

    [Tooltip("Seconds of calm between a gust and the next warning")]
    [SerializeField] private float _CalmSeconds = 1.5f;

    [Tooltip("Seconds between the warning and the gust")]
    [SerializeField] private float _WarningSeconds = 1.0f;

    [Tooltip("Seconds the gust blows")]
    [SerializeField] private float _GustSeconds = 2.5f;

    [Header("Wind")]
    [Tooltip("Newtons, applied at the ship's centre of mass for as long as the gust blows. " +
             "Engines give 20 N each : keep this above 28 N, or thrusters can hold the ship")]
    [SerializeField] private float _GustForce = 30.0f;

    [SerializeField] private DirectionMode _Direction = DirectionMode.AwayFromGoal;

    [Tooltip("Closer than this to the goal, 'away from the goal' has no meaning : pick a random direction")]
    [SerializeField] private float _MinDistanceFromGoal = 0.75f;

    [Header("Tether rules")]
    [Tooltip("Gives every arena border a kinematic Rigidbody, so a joint can be connected to a wall. " +
             "Without it a wall is only a collider, and nothing can be connected to it.")]
    [SerializeField] private bool _WallsHaveBodies = true;

    [Tooltip("Only a SpringJoint is accepted on the ship. A FixedJoint or a HingeJoint would pin the ship " +
             "to the wall rigidly : no distance to measure, no rope length to get right. Any other joint " +
             "is removed, and an error says why.")]
    [SerializeField] private bool _RequireSpringJoint = true;

    [Tooltip("A joint on the ship with no connectedBody holds on to a point in the air, not to a wall. " +
             "On : such a joint is removed, and an error says why. The student has to connect to the " +
             "wall's Rigidbody, and so to give connectedAnchor in ITS local space.")]
    [SerializeField] private bool _RequireConnectedBody = true;

    [Tooltip("A rope the ship is further from its anchor than maxDistance by more than this snaps, in " +
             "metres. A SpringJoint pulls with spring x stretch newtons : a rope tied 5 m too short " +
             "would yank a 1 kg ship with 2000 N. A rope that merely holds the wind stretches a few " +
             "centimetres. 0 : never snap.")]
    [SerializeField] private float _RopeSnapStretch = 1.0f;

    [Header("Display")]
    [SerializeField] private bool _ShowOnScreen = true;

    private SpaceshipControllerBase _Controller;
    private Rigidbody _ShipBody;

    private readonly List<Joint> _ShipJoints = new List<Joint>();
    private readonly HashSet<int> _ReportedJoints = new HashSet<int>();
    private float _PhaseTime;

    public static SolarWindHazard Instance { get; private set; }

    public Phase CurrentPhase { get; private set; } = Phase.Calm;

    /// <summary>Where the wind pushes. Unit vector on the Z = 0 plane. Chosen at the warning, fixed until the gust ends.</summary>
    public Vector3 CurrentDirection { get; private set; } = Vector3.right;

    public float GustForce { get { return _GustForce; } }

    /// <summary>Seconds left in the current phase.</summary>
    public float TimeLeftInPhase { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _Controller = FindFirstObjectByType<SpaceshipControllerBase>();

        if (GlobalSettings.Instance)
        {
            _ShipBody = GlobalSettings.Instance.ShipBody;
        }
        if (!_ShipBody && _Controller)
        {
            _ShipBody = _Controller.GetComponent<Rigidbody>();
        }

        if (!_ShipBody)
        {
            Debug.LogError("SolarWindHazard - no spaceship found, there is nothing to blow on.", this);
            enabled = false;
            return;
        }

        // The wind is only half the level : a tether is only worth the trouble if it can be seen.
        if (!_ShipBody.GetComponent<TetherRenderer>())
        {
            _ShipBody.gameObject.AddComponent<TetherRenderer>();
        }

        if (_WallsHaveBodies)
        {
            GiveWallsBodies();
        }

        EnterPhase(Phase.Calm, _FirstCalmSeconds);
    }

    private void Update()
    {
        _PhaseTime -= Time.deltaTime;
        TimeLeftInPhase = Mathf.Max(0.0f, _PhaseTime);

        if (_PhaseTime > 0.0f)
        {
            return;
        }

        switch (CurrentPhase)
        {
            case Phase.Calm:
                EnterPhase(Phase.Warning, _WarningSeconds);
                break;
            case Phase.Warning:
                EnterPhase(Phase.Gust, _GustSeconds);
                break;
            case Phase.Gust:
                EnterPhase(Phase.Calm, _CalmSeconds);
                break;
        }
    }

    /// <summary>
    /// A wall is a collider. A joint can only be connected to a BODY, so each border of the
    /// arena (GlobalSettings builds them as its own children) gets a kinematic Rigidbody : it
    /// never moves, and it can be the connectedBody of a tether.
    /// </summary>
    private void GiveWallsBodies()
    {
        if (!GlobalSettings.Instance)
        {
            return;
        }

        foreach (Collider wall in GlobalSettings.Instance.GetComponentsInChildren<Collider>())
        {
            if (wall.attachedRigidbody != null)
            {
                continue;
            }

            Rigidbody body = wall.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    /// <summary>
    /// The rule : no tether in the air. Run before each physics step, so a rope tied to nothing
    /// does not get to pull for long.
    /// </summary>
    private void EnforceConnectedBody()
    {
        _ShipBody.GetComponentsInChildren(_ShipJoints);

        foreach (Joint joint in _ShipJoints)
        {
            if (joint.connectedBody != null)
            {
                continue;
            }

            if (_ReportedJoints.Add(joint.GetInstanceID()))
            {
                Debug.LogError("Level 06 - a tether has to be CONNECTED to a Rigidbody. A " + joint.GetType().Name +
                               " with no connectedBody holds on to a point in the air, not to a wall : it was removed.\n" +
                               "The walls have kinematic Rigidbodies. Connect to the one your query hit (hit.rigidbody), " +
                               "and give connectedAnchor in the LOCAL space of that body.", joint);
            }

            Destroy(joint);
        }
    }

    /// <summary>
    /// A rope that is too short for where the ship is would yank it : the spring pulls with
    /// (distance - maxDistance) x spring newtons. Past _RopeSnapStretch it snaps instead, before
    /// the physics step that would have thrown the ship.
    /// </summary>
    private void SnapOverstretchedRopes()
    {
        _ShipBody.GetComponentsInChildren(_ShipJoints);

        foreach (Joint joint in _ShipJoints)
        {
            SpringJoint rope = joint as SpringJoint;
            if (rope == null || rope.connectedBody == null)
            {
                continue; // no body : EnforceConnectedBody deals with it
            }

            // anchor : local to the ship. connectedAnchor : local to the connected body.
            Vector3 anchor_WorldSpace = rope.transform.TransformPoint(rope.anchor);
            Vector3 connectedAnchor_WorldSpace = rope.connectedBody.transform.TransformPoint(rope.connectedAnchor);

            float length = Vector3.Distance(anchor_WorldSpace, connectedAnchor_WorldSpace);
            float stretch = length - rope.maxDistance;

            if (stretch <= _RopeSnapStretch)
            {
                continue;
            }

            if (_ReportedJoints.Add(rope.GetInstanceID()))
            {
                Debug.LogError("Level 06 - the rope snapped. The ship is " + length.ToString("0.0") + " m from its anchor, " +
                               stretch.ToString("0.0") + " m more than its maxDistance (" + rope.maxDistance.ToString("0.0") +
                               " m) : the spring would have pulled with about " + (stretch * rope.spring).ToString("0") + " N.\n" +
                               "Measure the length when you tie it, and never tie it shorter than the ship's distance. " +
                               "(A spring that is too weak for the wind stretches just as far.)", rope);
            }

            Destroy(rope);
        }
    }

    /// <summary>
    /// The rule : the tether is a SpringJoint, and nothing else. It is the one joint that has a
    /// length to get right ; a FixedJoint or a HingeJoint to the wall would just hold the ship.
    /// </summary>
    private void EnforceSpringJoint()
    {
        _ShipBody.GetComponentsInChildren(_ShipJoints);

        foreach (Joint joint in _ShipJoints)
        {
            if (joint is SpringJoint)
            {
                continue;
            }

            if (_ReportedJoints.Add(joint.GetInstanceID()))
            {
                Debug.LogError("Level 06 - only a SpringJoint is accepted. A " + joint.GetType().Name +
                               " would pin the ship to the wall : there would be no distance to measure, and " +
                               "no rope to get the length of. It was removed.\n" +
                               "Use a SpringJoint, with a minDistance and a maxDistance.", joint);
            }

            Destroy(joint);
        }
    }

    private void FixedUpdate()
    {
        if (_RequireSpringJoint && _ShipBody)
        {
            EnforceSpringJoint();
        }

        if (_RequireConnectedBody && _ShipBody)
        {
            EnforceConnectedBody();
        }

        if (_RopeSnapStretch > 0.0f && _ShipBody)
        {
            SnapOverstretchedRopes();
        }

        if (CurrentPhase != Phase.Gust || !_ShipBody)
        {
            return;
        }

        // At the centre of mass : a plain AddForce never turns the ship.
        _ShipBody.AddForce(CurrentDirection * _GustForce, ForceMode.Force);
    }

    private void EnterPhase(Phase phase, float seconds)
    {
        CurrentPhase = phase;
        _PhaseTime = seconds;
        TimeLeftInPhase = seconds;

        switch (phase)
        {
            case Phase.Warning:
                CurrentDirection = ChooseDirection();
                if (_Controller)
                {
                    _Controller.OnGustWarning(CurrentDirection, _GustForce, seconds);
                }
                break;

            case Phase.Gust:
                if (_Controller)
                {
                    _Controller.OnGustStarted();
                }
                break;

            case Phase.Calm:
                // The very first calm is the start of the level, not the end of a gust.
                if (_Controller && Time.timeSinceLevelLoad > 0.5f)
                {
                    _Controller.OnGustEnded();
                }
                break;
        }
    }

    private Vector3 ChooseDirection()
    {
        if (_Direction == DirectionMode.AwayFromGoal)
        {
            GoalComponentBase goal = FindFirstObjectByType<GoalComponentBase>();
            if (goal)
            {
                Vector3 awayFromGoal = _ShipBody.worldCenterOfMass - goal.transform.position;
                awayFromGoal.z = 0.0f;

                if (awayFromGoal.magnitude >= _MinDistanceFromGoal)
                {
                    return awayFromGoal.normalized;
                }
            }
        }

        float angle = Random.Range(0.0f, 360.0f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
    }

    // ---------------------------------------------------------------------------
    // What the student sees : a bar at the top of the screen, and the direction as an arrow.
    // ---------------------------------------------------------------------------

    private void OnGUI()
    {
        if (!_ShowOnScreen || CurrentPhase == Phase.Calm)
        {
            return;
        }

        bool blowing = CurrentPhase == Phase.Gust;
        Color colour = blowing ? new Color(1.0f, 0.35f, 0.15f) : new Color(1.0f, 0.85f, 0.2f);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = colour;

        string text = blowing
            ? "VENT SOLAIRE !   " + TimeLeftInPhase.ToString("0.0") + " s"
            : "Rafale dans " + TimeLeftInPhase.ToString("0.0") + " s";

        float width = 360.0f;
        GUI.Label(new Rect((Screen.width - width) * 0.5f, 10.0f, width, 30.0f), text, style);

        // An arrow in the direction the wind will push, drawn from the middle of the screen.
        Vector2 screenDirection = new Vector2(CurrentDirection.x, -CurrentDirection.y);
        Vector2 centre = new Vector2(Screen.width * 0.5f, 70.0f);
        float length = blowing ? 40.0f : 28.0f;

        Matrix4x4 saved = GUI.matrix;
        float angle = Mathf.Atan2(screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, centre);

        GUI.color = colour;
        GUI.DrawTexture(new Rect(centre.x - length, centre.y - 3.0f, length * 2.0f - 10.0f, 6.0f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centre.x + length - 14.0f, centre.y - 9.0f, 14.0f, 18.0f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.matrix = saved;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || CurrentPhase == Phase.Calm || !_ShipBody)
        {
            return;
        }

        Gizmos.color = CurrentPhase == Phase.Gust ? new Color(1.0f, 0.35f, 0.15f) : new Color(1.0f, 0.85f, 0.2f);
        DrawUtils.DrawArrow(_ShipBody.worldCenterOfMass, CurrentDirection * 3.0f);
    }
}
