using UnityEngine;

/// <summary>
/// The final level : a tunnel run in three sections, and the rules that tie them together.
///
///     Section 1   small tilted walls           walls            (queries)
///     Section 2   a field of drifting stars    sphere contacts  (collisions)
///     Section 3   a solar storm                gusts            (joints)  - and a few stars too
///
/// All in one rectangle, the same size from end to end. The walls are yellow : touching one is a reset.
///
/// The goals are a fixed sequence of checkpoints (GoalSpawner), ending on the docking port.
///
/// The price of a mistake : a hit on a wall faster than _ImpactSpeed, or a star that reaches the
/// hull, puts the ship back at the LAST checkpoint, at rest, with its joints removed. A run is
/// not lost, but every metre since the last checkpoint is.
///
/// The section the ship is in decides what is switched on : the stars (rate and height), and the
/// wind. They are switched by where the ship IS, so after a reset the right ones come back.
///
/// -----------------------
/// This file is not for you to edit.
/// -----------------------
/// </summary>
[DefaultExecutionOrder(-50)]
public class TunnelRunCourse : MonoBehaviour
{
    [System.Serializable]
    public struct Section
    {
        public string Name;

        [Tooltip("The section starts at this X (the ship's X), and ends where the next one starts.")]
        public float FromX;

        public bool Stars;
        public float StarSeconds;
        public float StarHalfHeight;

        public bool Wind;
        [Tooltip("Calm before the first warning when the ship enters the section.")]
        public float WindFirstCalm;
    }

    [SerializeField] Section[] _Sections = new Section[0];

    [Tooltip("A wall hit slower than this is free, in m/s. A faster one sends the ship back.")]
    [SerializeField] float _ImpactSpeed = 1.5f;

    [Tooltip("Stars this close to the checkpoint are removed when the ship is put back, in metres. " +
             "Otherwise one could be sitting on it.")]
    [SerializeField] float _ClearRadius = 3.5f;

    [SerializeField] bool _ShowHud = true;

    [Tooltip("Painted on every wall of the level (the arena borders too) when it starts : the colour " +
             "that says touching it sends you back.")]
    [SerializeField] Material _WallMaterial;

    SpaceshipControllerBase _Ship;
    Rigidbody _Body;
    GoalSpawner _Spawner;
    StarField _Stars;
    SolarWindHazard _Wind;

    Vector3 _Respawn;
    bool _ResetPending;
    string _ResetReason = "";
    int _SeenStarHits;
    int _Section = -1;

    float _StartTime = -1.0f;
    float _EndTime = -1.0f;

    public int SectionIndex { get { return _Section; } }
    public int CheckpointsReached { get; private set; }
    public int Resets { get; private set; }
    public bool Finished { get; private set; }
    public float ElapsedSeconds
    {
        get
        {
            if (_StartTime < 0.0f) { return 0.0f; }
            return (Finished ? _EndTime : Time.time) - _StartTime;
        }
    }

    /// <summary>The impacts the ship took, by what it hit.</summary>
    public int WallResets { get; private set; }
    public int StarResets { get; private set; }

    /// <summary>Forwards the ship's collisions here. Added to the ship at Start.</summary>
    class ImpactRelay : MonoBehaviour
    {
        public TunnelRunCourse Course;

        void OnCollisionEnter(Collision collision)
        {
            Course.OnShipCollision(collision);
        }
    }

    void Start()
    {
        _Ship = FindFirstObjectByType<SpaceshipControllerBase>();
        _Spawner = FindFirstObjectByType<GoalSpawner>();
        _Stars = FindFirstObjectByType<StarField>();
        _Wind = FindFirstObjectByType<SolarWindHazard>();

        if (_Ship == null || _Spawner == null)
        {
            Debug.LogError("TunnelRunCourse - needs a spaceship controller and a GoalSpawner in the scene.", this);
            enabled = false;
            return;
        }

        _Body = _Ship.GetComponent<Rigidbody>();
        _Respawn = _Body.position;
        _SeenStarHits = _Stars != null ? _Stars.HullHits : 0;

        PaintWalls();

        ImpactRelay relay = _Ship.gameObject.AddComponent<ImpactRelay>();
        relay.Course = this;

        _Spawner.SequenceGoalReached += OnCheckpointReached;
    }

    /// <summary>The arena borders are made at start-up from a prefab : they are painted here, with the slabs' colour.</summary>
    void PaintWalls()
    {
        if (_WallMaterial == null || !GlobalSettings.Instance)
        {
            return;
        }

        foreach (Renderer renderer in GlobalSettings.Instance.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = _WallMaterial;
        }
    }

    void OnDestroy()
    {
        if (_Spawner != null)
        {
            _Spawner.SequenceGoalReached -= OnCheckpointReached;
        }
    }

    void OnCheckpointReached(int index)
    {
        CheckpointsReached = index + 1;

        // The ship is on the checkpoint it just held : that is where it comes back to.
        _Respawn = _Spawner.SequencePosition(index);

        if (CheckpointsReached >= _Spawner.SequenceCount)
        {
            Finished = true;
            _EndTime = Time.time;
            SwitchHazards(-1);
            HudConsole.Log("Arrimage reussi en " + ElapsedSeconds.ToString("0.0") + " s, " + Resets + " retour(s) au checkpoint");
        }
        else
        {
            HudConsole.Log("Checkpoint " + CheckpointsReached + " / " + _Spawner.SequenceCount);
        }
    }

    void OnShipCollision(Collision collision)
    {
        if (Finished || _ResetPending)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < _ImpactSpeed)
        {
            return;
        }

        _ResetPending = true;
        _ResetReason = "mur";
        WallResets++;
    }

    void FixedUpdate()
    {
        if (_StartTime < 0.0f && _Body.linearVelocity.sqrMagnitude > 0.01f)
        {
            _StartTime = Time.time;
        }

        if (Finished)
        {
            return;
        }

        UpdateSection();

        // A star that reached the hull : StarField counted it.
        if (_Stars != null && _Stars.HullHits > _SeenStarHits)
        {
            _SeenStarHits = _Stars.HullHits;
            if (!_ResetPending)
            {
                _ResetPending = true;
                _ResetReason = "etoile";
                StarResets++;
            }
        }

        if (_ResetPending)
        {
            SendBack();
        }
    }

    void UpdateSection()
    {
        int section = -1;
        float x = _Body.position.x;

        for (int i = 0; i < _Sections.Length; i++)
        {
            if (x >= _Sections[i].FromX)
            {
                section = i;
            }
        }

        if (section == _Section)
        {
            return;
        }

        _Section = section;
        SwitchHazards(section);
    }

    void SwitchHazards(int section)
    {
        bool stars = section >= 0 && _Sections[section].Stars;
        bool wind = section >= 0 && _Sections[section].Wind;

        if (_Stars != null)
        {
            if (stars)
            {
                _Stars.SetBelt(true, _Sections[section].StarSeconds, _Sections[section].StarHalfHeight);
            }
            else
            {
                _Stars.SetBelt(false, 1.0f, 1.0f);
            }
        }

        if (_Wind != null)
        {
            _Wind.SetActive(wind, wind ? _Sections[section].WindFirstCalm : 0.0f);
        }

        if (section >= 0)
        {
            HudConsole.Log("Section " + (section + 1) + " : " + _Sections[section].Name);
        }
    }

    /// <summary>
    /// Back to the last checkpoint, at rest, level, with nothing attached. Done in FixedUpdate and
    /// through the Rigidbody : the ship is interpolated, and a write to its Transform alone would be
    /// pulled straight back.
    /// </summary>
    void SendBack()
    {
        _ResetPending = false;
        Resets++;

        foreach (Joint joint in _Body.GetComponentsInChildren<Joint>())
        {
            Destroy(joint);
        }

        _Body.linearVelocity = Vector3.zero;
        _Body.angularVelocity = Vector3.zero;
        _Body.position = _Respawn;
        _Body.rotation = Quaternion.identity;
        _Body.transform.SetPositionAndRotation(_Respawn, Quaternion.identity);
        Physics.SyncTransforms();

        if (_Stars != null)
        {
            foreach (StarComponent star in _Stars.Stars)
            {
                if (star != null && Vector3.Distance(star.transform.position, _Respawn) < _ClearRadius)
                {
                    Destroy(star.gameObject);
                }
            }
        }

        HudConsole.Log("Impact (" + _ResetReason + ") : retour au dernier checkpoint");
        _Ship.OnSentBackToCheckpoint(_Respawn);
    }

    // ---------------------------------------------------------------------------
    // The HUD : bottom centre. The Rules panel and the console own the top left, the wind banner
    // the top centre and the briefing the right edge.
    // ---------------------------------------------------------------------------

    GUIStyle _Style;

    void OnGUI()
    {
        if (!_ShowHud || _Spawner == null || _Body == null)
        {
            return;
        }

        if (_Style == null)
        {
            _Style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        string sectionName = _Section >= 0 ? "Section " + (_Section + 1) + " / " + _Sections.Length + " : " + _Sections[_Section].Name : "";
        string text;
        if (Finished)
        {
            _Style.normal.textColor = new Color(0.4f, 1.0f, 0.5f);
            text = "ARRIMAGE REUSSI   " + ElapsedSeconds.ToString("0.0") + " s   " + Resets + " retour(s)";
        }
        else
        {
            _Style.normal.textColor = Color.white;
            text = sectionName + "     Checkpoint " + CheckpointsReached + " / " + _Spawner.SequenceCount
                 + "     Retours : " + Resets + "     " + ElapsedSeconds.ToString("0.0") + " s";
        }

        float width = 720.0f;
        GUI.Label(new Rect((Screen.width - width) * 0.5f, Screen.height - 44.0f, width, 30.0f), text, _Style);
    }
}
