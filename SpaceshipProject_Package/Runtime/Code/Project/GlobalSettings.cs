using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-1000)] // s�ex�cute tr�s t�t
public class GlobalSettings : MonoBehaviour
{
    public static GlobalSettings Instance { get; private set; }

    public Plane ScrollingPlane = new Plane(Vector3.forward, Vector3.zero);
    public Vector3 WorldRight = new Vector3(1.0f, 0.0f, 0.0f); 
    public Vector3 WorldUp = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 WorldDepth = new Vector3(0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Display console with GlobalSettings
    /// </summary>
    [SerializeField] bool _DisplayGUI = true;

    /// <summary>
    /// Which prefab to use for the arena borders (should have a BoxCollider)
    /// </summary>
    [SerializeField] GameObject _ArenaBorderPrefab;
    [SerializeField] public Vector2 ArenaSize = new Vector2(20, 20);

    /// <summary>
    /// Rules
    /// </summary>
    // The scenes were authored when this field was called "_EnableGravity". Without this
    // attribute their overrides bind to nothing and silently fall back to the prefab value.
    [SerializeField, FormerlySerializedAs("_EnableGravity")] bool EnableGravity = false;
    [SerializeField] bool RandomizeSpaceshipZRotation = false;
    [SerializeField] bool UnlockSpaceshipZRotation = false;

    /// <summary>
    /// One-shot torque impulse (N.m.s, around Z) given to the ship on begin play, so a
    /// level can start it already spinning instead of at rest. 0 means no impulse, and
    /// the sign chooses the direction.
    ///
    /// An IMPULSE, not a torque: the spin it produces is impulse / inertia, so it does
    /// not depend on framerate or on how long start-up took. With the ship's default body
    /// (mass 1, sphere collider r = 0.6, so inertia ~= 0.144) 0.2 gives roughly 80 deg/s.
    ///
    /// Needs UnlockSpaceshipZRotation to be visible: a level that keeps Z frozen absorbs
    /// the impulse completely. The Rules panel says so rather than letting it look broken.
    /// </summary>
    [SerializeField] float SpaceshipStartAngularImpulse = 0.0f;

    /// <summary>
    /// Arena
    /// </summary>
    [SerializeField, Min(3)] private int _ArenaSides = 4; 
    [SerializeField] private float BorderThickness = 1f;

    GUIStyle _Panel, _Header, _Item;

    private void Awake()
    {
        // ------------- Singleton Pattern -------------
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Avoid duplicate singletons
            return;
        }

        Instance = this;

        if (Application.isEditor)
        {
            Application.runInBackground = true;
        }

        // ------------- Singleton Pattern -------------

        // ------------- Spaceship Setup -------------
        // Find the ship by its ENGINES, not by its controller. The controller is what the
        // student writes and adds later; the level rules below (random start rotation,
        // rotation constraints) must apply from the very first Play, whether or not a
        // controller exists yet. Gating them on the controller silently disabled every
        // rule in a level where the student had not written one.
        Rigidbody shipBody = FindSpaceshipBody();
        if (!shipBody)
        {
            Debug.LogError("No spaceship found in the scene (expected a Rigidbody with RocketEngine children).");
        }
        else
        {
            if (RandomizeSpaceshipZRotation)
            {
                // Replace only Z with a random value between 0 and 360
                Vector3 currentEuler = shipBody.transform.eulerAngles;
                currentEuler.z = UnityEngine.Random.Range(0f, 360f);                                                                                                                                                                                        
                Quaternion randomized = Quaternion.Euler(currentEuler);                                                                                                                                                                                                                                   
                                                                                                                                                                                                                                                                                                         
                // Assign through the RIGIDBODY, not the Transform. The ship is interpolated,                                                                                                                                                                                                             
                // and an interpolated body re-drives its Transform from its own internal                                                                                                                                                                                                                 
                // physics pose every frame -- writing transform.rotation alone gets snapped                                                                                                                                                                                                              
                // straight back to identity on the first frame.                                                                                                                                                                                                                                          
                shipBody.rotation = randomized;                                                            
            }

            shipBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

            // Lock Z rotation if needed
            if (!UnlockSpaceshipZRotation)
            {
                shipBody.constraints |= RigidbodyConstraints.FreezeRotationZ;
            }

            // Deliberately AFTER the constraints above, so a level with Z frozen has the
            // impulse absorbed by the constraint exactly as the physics engine would do
            // on its own -- there is no second copy of the rule here to keep in sync.
            //
            // Queued from Awake, integrated by the first physics step: the Rigidbody
            // accumulates the impulse and applies it on the next FixedUpdate, which always
            // comes after every Awake/Start. Setting angularVelocity directly instead would
            // mean doing the inertia division by hand, and would fight the same
            // interpolation that already bit us on rotation above.
            if (SpaceshipStartAngularImpulse != 0.0f)
            {
                shipBody.AddTorque(WorldDepth * SpaceshipStartAngularImpulse, ForceMode.Impulse);
            }

            // Kept so the Rules panel can report what the physics engine is really doing,
            // instead of repeating the inspector checkboxes back at us.
            _ShipBody = shipBody;
        }

        // The controller is a separate concern. Having none is the normal starting state
        // of a level, so it must never block the rules above.
        var controllers = FindObjectsByType<SpaceshipControllerBase>(FindObjectsSortMode.None);
        if (controllers.Length == 0)
        {
            Debug.Log("No spaceship controller in the scene yet. Add one that inherits from SpaceshipControllerBase.");
        }
        else if (controllers.Length > 1)
        {
            Debug.LogWarning("Multiple spaceship controllers found in the scene. Using the first one found.");
        }

        WarnIfUpdateIsHidden(controllers);
        // ------------- Spaceship Setup -------------


        // ------------- Scene Initialization -------------

        // Enable/Disable gravity
        if (!EnableGravity)
        {
            Physics.gravity = Vector3.zero;
        }

        // Setup arena borders
        if (_ArenaBorderPrefab != null)
        {
            BuildArena();
        }
        else
        {
            Debug.LogWarning("GlobalSettings - No Arena Border Prefab assigned. Skipping arena border creation.");
        }
    }

    void BuildArena()
    {
        if (!_ArenaBorderPrefab) return;

        int n = Mathf.Max(3, _ArenaSides);

        if (n == 4)
        {
            BuildRectangle();
            return;
        }

        // -------- Regular N-gon (inscribed in circle) --------
        float R = 0.5f * Mathf.Min(ArenaSize.x, ArenaSize.y);   // circumradius (fits inside the given rect)
        float a = R * Mathf.Cos(Mathf.PI / n);                  // apothem (center -> side center)
        float L = 2f * R * Mathf.Sin(Mathf.PI / n);             // side length

        for (int i = 0; i < n; i++)
        {
            float ang = i * Mathf.PI * 2f / n;

            // Outward normal of side i in 2D
            Vector2 normal2 = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            // Center of side i is apothem units along the normal
            Vector3 pos = new Vector3(normal2.x * a, normal2.y * a, 0f);

            // Rotate so local +X aligns with the side's tangent (perpendicular to normal)
            // Tangent angle is ang + 90�
            float rotDeg = ang * Mathf.Rad2Deg + 90f;
            Quaternion rot = Quaternion.Euler(0f, 0f, rotDeg); // rotate around Z for XY plane

            var t = Instantiate(_ArenaBorderPrefab, pos, rot, transform);

            // Scale: length along local X; thickness along the axis normal to the side within the plane
            var s = t.transform.localScale;
            s.x = L;
            // In XY, the inward/outward thickness axis is local Y
            s.y = BorderThickness;

            t.transform.localScale = s;
            t.name = $"Border_{i}";
        }
    }
    private void BuildRectangle()
    {
        // Exactly replicates your original 4-border behavior (XY version),
        // and adapts properly if you're on the XZ plane.
        // XY plane (2D top-down)
        var top = Instantiate(_ArenaBorderPrefab, new Vector3(0f, +ArenaSize.y * 0.5f, 0f), Quaternion.identity, transform);
        var bottom = Instantiate(_ArenaBorderPrefab, new Vector3(0f, -ArenaSize.y * 0.5f, 0f), Quaternion.identity, transform);
        var left = Instantiate(_ArenaBorderPrefab, new Vector3(-ArenaSize.x * 0.5f, 0f, 0f), Quaternion.identity, transform);
        var right = Instantiate(_ArenaBorderPrefab, new Vector3(+ArenaSize.x * 0.5f, 0f, 0f), Quaternion.identity, transform);

        top.transform.localScale = new Vector3(ArenaSize.x, BorderThickness, top.transform.localScale.z);
        bottom.transform.localScale = new Vector3(ArenaSize.x, BorderThickness, bottom.transform.localScale.z);
        left.transform.localScale = new Vector3(BorderThickness, ArenaSize.y, left.transform.localScale.z);
        right.transform.localScale = new Vector3(BorderThickness, ArenaSize.y, right.transform.localScale.z);

        top.name = "Border_Top"; bottom.name = "Border_Bottom";
        left.name = "Border_Left"; right.name = "Border_Right";
    }

    void EnsureGUIStyles()
    {
        if (_Panel != null) return;

        _Panel = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        _Header = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14
        };

        _Item = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            wordWrap = true
        };
    }

    // ---- Rules panel ----------------------------------------------------------
    // The panel lists EVERY rule with its state. The previous version only listed the
    // rules that were ON, so "gravity is off" was communicated by a missing line --
    // invisible, and impossible to tell apart from "this panel is not working".
    //
    // Lines are rebuilt only when a value actually changes: OnGUI runs several times
    // per frame (layout + repaint), so building strings in there allocates constantly.

    Rigidbody _ShipBody;
    string[] _RuleLines;
    bool _HasRuleLines;
    bool _ShownGravity, _ShownZLocked, _ShownRandomStart;
    float _ShownStartImpulse;
    GoalComponentBase _Goal;
    int _ShownGoalState = -1;

    /// <summary>
    /// The spaceship is the body its RocketEngines push on. Looking it up this way keeps
    /// the level rules independent from whatever controller the student has written,
    /// and cannot be confused with the Payload (which has a Rigidbody but no engines).
    /// </summary>
    /// <summary>
    /// SpaceshipControllerBase.Update() is NOT virtual. A student who writes their own
    /// Update() silently hides it, UpdateTarget() stops running, and an auto-found Goal
    /// never sets TargetPosition -- with no error anywhere. This is the single most
    /// confusing failure in the project, so say it out loud.
    ///
    /// Controllers that drive TargetPosition themselves opt out via ManagesOwnTarget.
    /// </summary>
    void WarnIfUpdateIsHidden(SpaceshipControllerBase[] controllers)
    {
        for (int i = 0; i < controllers.Length; i++)
        {
            SpaceshipControllerBase controller = controllers[i];
            if (!controller || controller.ManagesOwnTarget)
            {
                continue;
            }

            System.Type type = controller.GetType();
            if (type == typeof(SpaceshipControllerBase))
            {
                continue;
            }

            System.Reflection.MethodInfo declared = type.GetMethod(
                "Update",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.DeclaredOnly);

            if (declared != null)
            {
                Debug.LogWarning(
                    type.Name + " declares its own Update(), which hides SpaceshipControllerBase.Update(). "
                    + "UpdateTarget() will not run, so an auto-found Goal will never set TargetPosition "
                    + "and your ship will have no target. Either set TargetPosition yourself (and override "
                    + "ManagesOwnTarget to return true), or move your code out of Update().",
                    controller);
            }
        }
    }

    /// <summary>
    /// The spaceship's body, already resolved during Awake. Exposed so other systems
    /// (hazard placement, HUDs) do not each reinvent the lookup.
    /// </summary>
    public Rigidbody ShipBody
    {
        get { return _ShipBody; }
    }

    Rigidbody FindSpaceshipBody()
    {
        var engines = FindObjectsByType<RocketEngine>(FindObjectsSortMode.None);

        for (int i = 0; i < engines.Length; i++)
        {
            // Search from the parent, mirroring how RocketEngine finds the body it pushes.
            Transform parent = engines[i].transform.parent;
            Rigidbody body = parent ? parent.GetComponentInParent<Rigidbody>() : null;

            if (body)
            {
                return body;
            }
        }

        return null;
    }

    /// <summary>
    /// Is the spaceship's Z rotation really frozen? Asked to the Rigidbody rather than
    /// to our own checkbox, so the panel reports the simulation and not our intent.
    /// </summary>
    bool IsShipZRotationLocked()
    {
        if (!_ShipBody)
        {
            return !UnlockSpaceshipZRotation;
        }

        return (_ShipBody.constraints & RigidbodyConstraints.FreezeRotationZ) != 0;
    }

    /// <summary>
    /// 0 = no goal in the scene right now, 1 = handed to you, 2 = you must query for it.
    ///
    /// This rule is the only one a student cannot possibly see: it is a checkbox on the
    /// GOAL PREFAB, and the goal is spawned at runtime, so there is nothing to inspect
    /// before pressing Play. When it is off, TargetPosition simply never updates and
    /// nothing anywhere says why.
    /// </summary>
    int GetGoalState()
    {
        if (!_Goal)
        {
            // Goals are spawned and destroyed repeatedly, but this only searches once
            // per goal lifetime rather than once per frame.
            _Goal = FindFirstObjectByType<GoalComponentBase>();
        }

        if (!_Goal)
        {
            return 0;
        }

        return _Goal.IsAutoFound ? 1 : 2;
    }

    void RefreshRuleLines()
    {
        // Read the engine, not the inspector.
        bool gravity = Physics.gravity != Vector3.zero;
        bool zLocked = IsShipZRotationLocked();
        bool randomStart = RandomizeSpaceshipZRotation;
        float startImpulse = SpaceshipStartAngularImpulse;
        int goalState = GetGoalState();

        if (_HasRuleLines && gravity == _ShownGravity && zLocked == _ShownZLocked
            && randomStart == _ShownRandomStart && startImpulse == _ShownStartImpulse
            && goalState == _ShownGoalState)
        {
            return;
        }

        _ShownGravity = gravity;
        _ShownZLocked = zLocked;
        _ShownRandomStart = randomStart;
        _ShownStartImpulse = startImpulse;
        _ShownGoalState = goalState;
        _HasRuleLines = true;

        // Phrased as an instruction, not a fact: it tells the student what to DO.
        string goalText = goalState == 1 ? "AUTO-DETECTED"
                        : goalState == 2 ? "FIND IT (query)"
                        : "none right now";

        // Spelled out rather than echoing the number alone: an impulse around a frozen Z
        // axis is absorbed entirely, and nothing else in the game would show that.
        string spinText = startImpulse == 0.0f ? "NONE"
                        : zLocked ? startImpulse.ToString("0.###") + " (ABSORBED, Z locked)"
                        : startImpulse.ToString("0.###") + " impulse";

        _RuleLines = new string[]
        {
            "Gravity: <b>" + (gravity ? "ON" : "OFF") + "</b>",
            "Z rotation: <b>" + (zLocked ? "LOCKED" : "UNLOCKED") + "</b>",
            "Start rotation: <b>" + (randomStart ? "RANDOM" : "FIXED") + "</b>",
            "Start spin: <b>" + spinText + "</b>",
            "Goal: <b>" + goalText + "</b>",
        };
    }

    void OnGUI()
    {
        if (!_DisplayGUI)
        {
            return;
        }

        EnsureGUIStyles();
        RefreshRuleLines();

        // The vertical box sizes itself to its content, so nothing clips any more
        // and there is no hardcoded height to keep in sync.
        const float x = 10f, y = 10f, width = 300f;

        GUILayout.BeginArea(new Rect(x, y, width, Screen.height - y * 2.0f));
        GUILayout.BeginVertical(_Panel);

        GUILayout.Label("Rules", _Header);

        for (int i = 0; i < _RuleLines.Length; i++)
        {
            GUILayout.Label(_RuleLines[i], _Item);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}