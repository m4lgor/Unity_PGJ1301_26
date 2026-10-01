using System;
using UnityEngine;

public class GoalComponentBase : MonoBehaviour
{
    // Store the spawner and the spaceship to broadcast when the goal is reached
    protected GoalSpawner _spawner;
    protected SpaceshipControllerBase _spaceship;

    public Action OnReady;
    public Action OnEnd;

    [SerializeField] bool _IsGloballyFound = false;

    [Tooltip("How long the goal must stay occupied before it counts. 0 validates on " +
             "contact, which is the original behaviour. A level's GoalSpawner can " +
             "override this value without needing its own goal prefab.")]
    [SerializeField] float _RequiredTimeInZone = 0.0f;

    // How many colliders of interest are inside right now. A count and not a bool, so an
    // object with several colliders cannot switch us off on its first OnTriggerExit.
    int _OccupantCount;

    float _TimeInZone;
    bool _Completed;

    // Idle animation of the shader falloff, used whenever nothing is in the zone.
    float _FalloffDistance = 0.0f;
    float _FalloffSpeed = 1.0f;

    // Positive or Negative
    float _PN = 1.0f;

    // The range the shader clamps _Falloff to.
    const float MinFalloff = 0.05f;
    const float MaxFalloff = 0.95f;

    Renderer _Renderer;

    public bool IsAutoFound
    {
        get { return _IsGloballyFound; }
    }

    /// <summary>
    /// True while something that counts is inside the goal.
    /// </summary>
    public bool IsOccupied
    {
        get { return _OccupantCount > 0; }
    }

    /// <summary>
    /// 0 to 1, how much of the required time has been held so far.
    /// </summary>
    public float Progress
    {
        get
        {
            if (_RequiredTimeInZone <= 0.0f)
            {
                return IsOccupied ? 1.0f : 0.0f;
            }

            return Mathf.Clamp01(_TimeInZone / _RequiredTimeInZone);
        }
    }

    public float RequiredTimeInZone
    {
        get { return _RequiredTimeInZone; }
    }

    protected void Awake()
    {
        _Renderer = GetComponent<Renderer>();
    }

    protected void Start()
    {
        OnReady?.Invoke();
    }

    protected void OnDestroy()
    {
        OnEnd?.Invoke();
    }

    public void Init(GoalSpawner spawner, SpaceshipControllerBase spaceship)
    {
        Init(spawner, spaceship, -1.0f);
    }

    /// <summary>
    /// Wires the goal up. A requiredTimeOverride of 0 or more replaces the value carried
    /// by the prefab, which is how a level asks for its own dwell time while still using
    /// the shared goal prefab. Anything negative leaves the prefab's value alone.
    /// </summary>
    public void Init(GoalSpawner spawner, SpaceshipControllerBase spaceship,
                     float requiredTimeOverride)
    {
        _spawner = spawner;
        _spaceship = spaceship;

        if (requiredTimeOverride >= 0.0f)
        {
            _RequiredTimeInZone = requiredTimeOverride;
        }
    }

    /// <summary>
    /// Called by the subclasses when something that counts enters the goal.
    /// </summary>
    protected void EnterZone()
    {
        _OccupantCount++;

        // With no required time this validates on the spot, exactly as it did before,
        // rather than waiting for the next frame.
        TryComplete();
    }

    /// <summary>
    /// Called by the subclasses when it leaves again. Leaving early loses the progress:
    /// the goal has to be held, not merely touched.
    /// </summary>
    protected void ExitZone()
    {
        _OccupantCount = Mathf.Max(0, _OccupantCount - 1);

        if (!IsOccupied)
        {
            _TimeInZone = 0.0f;
        }
    }

    /// <summary>
    /// What happens once the goal has been held long enough. The base class owns the
    /// timing, the subclass owns who to tell.
    /// </summary>
    protected virtual void OnCompleted()
    {
    }

    void TryComplete()
    {
        if (_Completed || !IsOccupied)
        {
            return;
        }

        if (_TimeInZone < _RequiredTimeInZone)
        {
            return;
        }

        _Completed = true;
        OnCompleted();
    }

    /// <summary>
    /// The time is counted here and not in OnTriggerStay on purpose: a body that comes to
    /// a complete stop inside the goal can fall asleep, and Unity then stops sending
    /// OnTriggerStay. The timer would freeze exactly when the piloting was perfect.
    /// </summary>
    private void Update()
    {
        if (IsOccupied && !_Completed)
        {
            _TimeInZone += Time.deltaTime;
            TryComplete();
        }

        UpdateFalloff();
    }

    /// <summary>
    /// The shader's falloff eats into the sphere's alpha, so it doubles as a progress
    /// readout: the goal solidifies as the time is held, and drops back to its idle
    /// pulsing the moment the zone is left.
    /// </summary>
    void UpdateFalloff()
    {
        if (_Renderer == null)
        {
            return;
        }

        float falloff;

        if (IsOccupied && _RequiredTimeInZone > 0.0f)
        {
            falloff = Mathf.Lerp(MaxFalloff, MinFalloff, Progress);
        }
        else
        {
            _FalloffDistance += Time.deltaTime * _PN * _FalloffSpeed;
            if (_FalloffDistance > 1.0f)
            {
                _PN = -1.0f;
            }
            if (_FalloffDistance < 0.0f)
            {
                _PN = 1.0f;
            }

            _FalloffDistance = Mathf.Clamp(_FalloffDistance, 0.0f, 1.0f);
            falloff = _FalloffDistance;
        }

        _Renderer.material.SetFloat("_Falloff", falloff);
    }
}
