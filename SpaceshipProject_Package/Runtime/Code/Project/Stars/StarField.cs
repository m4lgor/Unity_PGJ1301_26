using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places the stars, and keeps the list of the live ones.
///
/// The interesting mode is BarrierOnGoalSpawn. Every time a Goal appears, a wall of stars
/// is laid across the route to it, halfway between the ship and the Goal. The gaps are
/// narrower than the ship, so there is no clean way through: you have to push the stars
/// out of your way, one contact at a time.
///
/// Same reasoning as HazardSpawner.BetweenShipAndGoal -- a wall placed by hand would be
/// somewhere else than the Goal most runs, and whether it blocked anything would be luck.
/// Rebuilding it on the ship-to-Goal line every spawn keeps the difficulty constant and
/// stops anyone learning "the stars are on the left".
///
/// The stars do not move. All of the relative velocity is therefore YOURS, which is what
/// makes the impulse a student has to compute depend on how fast they chose to arrive.
///
/// The list is handed to you on purpose. Finding what might be touching you is a
/// different problem -- broadphase, and the queries you get next class. Today the pairs
/// are given, exactly as they were in TP_Manifold.
///
/// -----------------------
/// This file is not for you to edit.
/// -----------------------
/// </summary>
[DefaultExecutionOrder(-500)]
public class StarField : MonoBehaviour
{
    public enum PlacementMode
    {
        /// <summary>Stars drift across the arena, from the Goal's side towards the ship.</summary>
        Stream,

        /// <summary>A wall of still stars across the route, rebuilt whenever a Goal spawns.</summary>
        BarrierOnGoalSpawn,
    }

    public static StarField Instance { get; private set; }

    [Header("Mode")]
    [SerializeField] PlacementMode _Placement = PlacementMode.BarrierOnGoalSpawn;

    [SerializeField] GameObject _StarPrefab;

    [Tooltip("Ceiling on live stars, both modes.")]
    [SerializeField] int _MaxAlive = 40;

    [Tooltip("Uniform scale range. A Unity sphere is 1 unit across, so the world radius " +
             "is half of this.")]
    [SerializeField] Vector2 _ScaleRange = new Vector2(0.35f, 0.7f);

    [Tooltip("Mass range in kg. Well under the ship's mass, so pushing one aside costs " +
             "speed and attitude rather than the run.")]
    [SerializeField] Vector2 _MassRange = new Vector2(0.05f, 0.15f);

    [Header("Barrier")]
    [Tooltip("Half the wall's width in metres. Wide enough that going around is a long " +
             "way, not wide enough to close the arena off.")]
    [SerializeField] float _WallHalfWidth = 6.0f;

    [Tooltip("Free space between two neighbouring stars' SURFACES, in metres. Keep it " +
             "under the ship's diameter or there is a hole to slip through and the level " +
             "asks nothing.")]
    [SerializeField] float _WallGap = 0.6f;

    [Tooltip("Random shift along the route, in metres, so the wall is not a drawn line. " +
             "It does not open gaps: it only moves stars nearer or further along.")]
    [SerializeField] float _WallJitter = 0.35f;

    [Tooltip("Free space to leave around the ship and the Goal. Under twice this, the " +
             "Goal is too close to fit a wall and none is built.")]
    [SerializeField] float _EndClearance = 3.0f;

    [Tooltip("Hull impacts before the Goal is taken away and a fresh wall built somewhere " +
             "else. 1 means a single impact costs you the whole approach. 0 never moves it.")]
    [SerializeField] int _ImpactsBeforeRespawn = 1;

    [Header("Stream")]
    [Tooltip("Seconds between two stars.")]
    [SerializeField] float _SpawnInterval = 0.3f;

    [SerializeField] Vector2 _SpeedRange = new Vector2(3.0f, 7.0f);

    [Tooltip("Fraction of stars put on a collision course with the ship. The rest are " +
             "spread across the front.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] float _AimedFraction = 0.25f;

    [Tooltip("How far beyond the arena a star is released, in metres.")]
    [SerializeField] float _SpawnMargin = 3.0f;

    [Tooltip("How far beyond the arena a star is deleted, in metres.")]
    [SerializeField] float _DespawnMargin = 6.0f;

    [Header("HUD")]
    [SerializeField] bool _ShowCounter = true;

    readonly List<StarComponent> _Stars = new List<StarComponent>();

    SpaceshipControllerBase _Ship;
    Rigidbody _ShipBody;
    float _ShipRadius = 0.6f;
    float _SpawnCountdown;

    GoalSpawner _GoalSpawner;
    GoalComponentBase _TrackedGoal;

    bool _RespawnPending;
    int _HitsSinceRespawn;

    int _HullHits;
    int _Deflections;

    /// <summary>
    /// The stars alive right now. Never null, and it is the real list -- read it, do not
    /// keep it. A star you cached last frame may have been deleted since.
    /// </summary>
    public IReadOnlyList<StarComponent> Stars
    {
        get { return _Stars; }
    }

    /// <summary>
    /// The ship's collision radius, in metres. Read it instead of hardcoding 0.6: it is
    /// the RadiusA of your sphere-sphere test, and the prefab is allowed to change.
    /// </summary>
    public float ShipRadius
    {
        get { return _ShipRadius; }
    }

    public int HullHits
    {
        get { return _HullHits; }
    }

    public int Deflections
    {
        get { return _Deflections; }
    }

    void Awake()
    {
        Instance = this;

        _Ship = FindFirstObjectByType<SpaceshipControllerBase>();
        if (_Ship == null)
        {
            Debug.LogError("StarField - no spaceship controller in the scene.", this);
            enabled = false;
            return;
        }

        _ShipBody = _Ship.GetComponent<Rigidbody>();
        _GoalSpawner = FindFirstObjectByType<GoalSpawner>();

        // The ship is a sphere, like the stars, so the whole level is one sphere-sphere
        // test. Taken from the collider rather than typed in, so retuning the prefab does
        // not silently make every student's detection wrong by a few centimetres.
        SphereCollider shipCollider = _Ship.GetComponentInChildren<SphereCollider>();
        if (shipCollider != null)
        {
            Vector3 scale = shipCollider.transform.lossyScale;
            float largest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            _ShipRadius = shipCollider.radius * largest;
        }
        else
        {
            Debug.LogWarning("StarField - no SphereCollider on the ship, falling back to "
                             + _ShipRadius + " m.", this);
        }

        if (_WallGap >= _ShipRadius * 2.0f)
        {
            Debug.LogWarning("StarField - _WallGap (" + _WallGap + " m) is at least the ship's "
                             + "diameter (" + (_ShipRadius * 2.0f) + " m): the ship can fly "
                             + "between two stars without ever touching one, and the level "
                             + "asks nothing. Lower it.", this);
        }

        _SpawnCountdown = _SpawnInterval;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        CullStars();

        if (_Placement == PlacementMode.BarrierOnGoalSpawn)
        {
            if (_RespawnPending)
            {
                TakeGoalAway();
                return;
            }

            TrackGoal();
            return;
        }

        _SpawnCountdown -= Time.deltaTime;
        if (_SpawnCountdown > 0.0f)
        {
            return;
        }

        _SpawnCountdown = _SpawnInterval;

        if (_Stars.Count < _MaxAlive)
        {
            SpawnDrifter();
        }
    }

    /// <summary>
    /// A new Goal means a new route, so the old wall is torn down and one is built across
    /// the new one. Compared by reference: the spawner destroys the goal and makes another,
    /// so a different object IS a new goal.
    /// </summary>
    void TrackGoal()
    {
        GoalComponentBase goal = FindFirstObjectByType<GoalComponentBase>();

        if (goal == _TrackedGoal)
        {
            return;
        }

        _TrackedGoal = goal;

        if (goal != null)
        {
            BuildWall(goal.transform.position);
        }
    }

    /// <summary>
    /// Lays a line of stars across the ship-to-Goal route, at its midpoint.
    ///
    /// Walked from one end to the other, advancing by each star's own radius and then by
    /// the gap, so the FREE SPACE between two neighbours is _WallGap whatever sizes came
    /// out of the random draw. Stepping by a fixed pitch instead would leave holes wherever
    /// two small stars landed side by side.
    /// </summary>
    void BuildWall(Vector3 goalPosition)
    {
        ClearStars();

        Vector3 axis = goalPosition - _ShipBody.position;
        float span = axis.magnitude;

        if (span < _EndClearance * 2.0f)
        {
            // The Goal spawned almost on top of the ship. A wall would land inside one end
            // or the other -- possibly ON the ship, which is an impact nobody could avoid --
            // so the route is left open instead.
            //
            // This is a safety net, not a design: an open route is a free Goal, and after
            // an impact it would be a way out. The level closes the hole at the other end
            // by setting GoalSpawner._minDistanceFromShip above twice this, so a Goal can
            // never spawn near enough to trigger it. If you are reading this warning, that
            // is the value to raise.
            Debug.LogWarning("StarField - no wall: the Goal spawned " + span.ToString("F1")
                             + " m away and " + (_EndClearance * 2.0f) + " m is needed. Raise "
                             + "GoalSpawner._minDistanceFromShip, or lower _EndClearance.", this);
            return;
        }

        Vector3 direction = axis / span;
        Vector3 lateral = Vector3.Cross(direction, GlobalSettings.Instance.WorldDepth).normalized;
        Vector3 centre = _ShipBody.position + direction * (span * 0.5f);

        float offset = -_WallHalfWidth;

        while (offset < _WallHalfWidth && _Stars.Count < _MaxAlive)
        {
            float scale = Random.Range(_ScaleRange.x, _ScaleRange.y);
            float radius = scale * 0.5f;

            offset += radius;

            Vector3 position = centre
                             + lateral * offset
                             + direction * Random.Range(-_WallJitter, _WallJitter);

            SpawnStar(position, scale, Vector3.zero);

            offset += radius + _WallGap;
        }
    }

    void ClearStars()
    {
        for (int i = 0; i < _Stars.Count; i++)
        {
            if (_Stars[i] != null)
            {
                Destroy(_Stars[i].gameObject);
            }
        }

        _Stars.Clear();
    }

    /// <summary>
    /// Drops the stars that have left the arena, and any that were destroyed elsewhere.
    /// Backwards, because removing while iterating forwards skips an entry.
    /// </summary>
    void CullStars()
    {
        float bound = ArenaExtent() + _DespawnMargin;

        for (int i = _Stars.Count - 1; i >= 0; i--)
        {
            StarComponent star = _Stars[i];

            if (star == null)
            {
                _Stars.RemoveAt(i);
                continue;
            }

            if (star.transform.position.magnitude > bound)
            {
                _Stars.RemoveAt(i);
                Destroy(star.gameObject);
            }
        }
    }

    /// <summary>
    /// Stream mode: one star released from a line across the flow, upstream of the arena,
    /// so it crosses the whole play area. Releasing them inside it would eventually drop
    /// one on top of the ship, which is a hit nobody could have avoided.
    /// </summary>
    void SpawnDrifter()
    {
        // Recomputed for every star: the Goal moves when it is reached, and so does the
        // ship. The stream follows.
        Vector3 direction = FlowDirection();
        Vector3 lateral = Vector3.Cross(direction, GlobalSettings.Instance.WorldDepth).normalized;

        float extent = ArenaExtent();
        float scale = Random.Range(_ScaleRange.x, _ScaleRange.y);
        float starRadius = scale * 0.5f;

        float offset;
        if (Random.value < _AimedFraction)
        {
            // On a collision course with where the ship is now. It still has to be met --
            // the ship will have moved by the time the star arrives.
            float shipOffset = Vector3.Dot(_ShipBody.position, lateral);
            float band = _ShipRadius + starRadius;
            offset = shipOffset + Random.Range(-band, band);
        }
        else
        {
            offset = Random.Range(-extent, extent);
        }

        Vector3 position = -direction * (extent + _SpawnMargin) + lateral * offset;

        SpawnStar(position, scale, direction * Random.Range(_SpeedRange.x, _SpeedRange.y));
    }

    void SpawnStar(Vector3 position, float scale, Vector3 velocity)
    {
        if (_StarPrefab == null)
        {
            Debug.LogError("StarField - no star prefab assigned.", this);
            enabled = false;
            return;
        }

        GameObject spawned = Instantiate(_StarPrefab, position, Random.rotation, transform);
        spawned.transform.localScale = Vector3.one * scale;

        StarComponent star = spawned.GetComponent<StarComponent>();
        if (star == null)
        {
            Debug.LogError("StarField - the star prefab has no StarComponent.", this);
            Destroy(spawned);
            enabled = false;
            return;
        }

        star.Init(this, _ShipBody, _ShipRadius, Random.Range(_MassRange.x, _MassRange.y), velocity);

        _Stars.Add(star);
    }

    /// <summary>
    /// Stream mode: from the Goal towards the ship. With no Goal in the scene yet, any
    /// direction will do -- the next star will pick the real one up.
    /// </summary>
    Vector3 FlowDirection()
    {
        GoalComponentBase goal = FindFirstObjectByType<GoalComponentBase>();

        if (goal != null)
        {
            Vector3 goalToShip = _ShipBody.position - goal.transform.position;

            if (goalToShip.sqrMagnitude > 0.0001f)
            {
                return goalToShip.normalized;
            }
        }

        float angle = Random.Range(0.0f, 360.0f);
        return Quaternion.AngleAxis(angle, GlobalSettings.Instance.WorldDepth)
               * GlobalSettings.Instance.WorldRight;
    }

    float ArenaExtent()
    {
        Vector2 size = GlobalSettings.Instance.ArenaSize;
        return 0.5f * Mathf.Max(size.x, size.y);
    }

    public void RegisterHullHit()
    {
        _HullHits++;
        _HitsSinceRespawn++;

        if (_Placement != PlacementMode.BarrierOnGoalSpawn || _ImpactsBeforeRespawn <= 0)
        {
            return;
        }

        if (_HitsSinceRespawn < _ImpactsBeforeRespawn)
        {
            return;
        }

        // Only a flag. Barging into a wall lands several impacts in the same step, and
        // each one tearing the goal down and rebuilding it would move it several times in
        // a single frame. Update collapses them into one.
        _RespawnPending = true;
    }

    /// <summary>
    /// The price of an impact: the Goal you were flying to is gone, and the next one comes
    /// with its own wall somewhere else. Nothing is deducted and nothing is declared lost
    /// -- the approach you were most of the way through simply no longer exists.
    /// </summary>
    void TakeGoalAway()
    {
        _RespawnPending = false;
        _HitsSinceRespawn = 0;

        // The wall goes first, so a ship left tumbling by the impact cannot rack up more
        // impacts on a wall that is already condemned.
        ClearStars();

        if (_GoalSpawner == null)
        {
            return;
        }

        HudConsole.Log("Impact : le Goal se deplace, un nouveau mur vous attend");
        _GoalSpawner.RespawnGoal();

        // Deliberately NOT calling TrackGoal this frame. Destroy is deferred to the end of
        // the frame, so the old goal is still findable right now and we would aim the new
        // wall at a corpse. Next frame it is gone and the new goal is the only one left.
    }

    public void RegisterDeflection()
    {
        _Deflections++;
    }

    /// <summary>
    /// Bottom left: the HUD console owns the top left and the briefing owns the right edge.
    /// </summary>
    void OnGUI()
    {
        if (!_ShowCounter)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10.0f, Screen.height - 60.0f, 260.0f, 50.0f));
        GUILayout.Label("Etoiles deviees : " + _Deflections);
        GUILayout.Label("Impacts coque : " + _HullHits);
        GUILayout.EndArea();
    }
}
