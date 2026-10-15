using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

// -----------------------
// Not for you to edit. The physics lab : an arena where everything that costs time in the physics
// loop can be switched on from the keyboard (or by clicking the panel), with the Profiler open.
//
//   Frame load / FixedUpdate load  -> how many fixed steps run in one frame (0, 1, 2, ... or the spiral of death)
//   Bodies, colliders, sleeping    -> Physics.Simulate (PhysX)
//   Callbacks, triggers            -> Physics.SendEvents, after the simulation
//   Queries                        -> your FixedUpdate, and the GC
//   Paddles                        -> Physics.SyncTransforms, before the simulation
//   Cannon                         -> tunnelling and continuous collision detection
//
// Each thing the lab does by itself is wrapped in a ProfilerMarker named Lab.*, so it can be told
// apart from the engine's own markers in the Hierarchy view.
// -----------------------

public class PhysicsLab : MonoBehaviour
{
    [Tooltip("One per shape : sphere, cube, capsule")]
    public Material[] BodyMaterials;

    public Material BulletMaterial;

    [Tooltip("Kinematic rigidbodies : the right way to move something that pushes bodies around")]
    public Rigidbody[] KinematicPaddles;

    [Tooltip("Colliders with no rigidbody : the wrong way")]
    public Transform[] StaticPaddles;

    [Tooltip("Fires along its forward, at the thin wall")]
    public Transform Cannon;

    public int InitialBodies = 250;
    public float PaddleSpeed_DegreesPerSecond = 80.0f;
    public float BulletSpeed = 90.0f;
    public const float ExplosionAcceleration = 500.0f;

    // ---- What the keys cycle through ----------------------------------------------------

    static readonly string[] CallbackNames = { "off", "Enter + Exit", "+ Stay", "+ Stay (heavy)" };
    static readonly int[] StayWork = { 0, 0, 0, 3000 };
    static readonly string[] QueryNames = { "Raycast", "RaycastAll (allocates)", "OverlapSphere (allocates)", "OverlapSphereNonAlloc" };
    static readonly int[] QueryCounts = { 0, 100, 500, 2000 };
    static readonly string[] PaddleNames = { "off", "Rigidbody, in FixedUpdate", "Rigidbody's transform, in Update", "no Rigidbody, in Update" };
    static readonly float[] Timesteps = { 0.02f, 0.01f, 0.005f, 0.04f };
    static readonly float[] FrameLoads_Ms = { 0.0f, 8.0f, 25.0f, 60.0f };
    static readonly float[] FixedLoads_Ms = { 0.0f, 5.0f, 15.0f, 30.0f };
    static readonly float[] TimeScales = { 1.0f, 0.2f, 0.0f, 3.0f };
    static readonly CollisionDetectionMode[] BulletModes =
    {
        CollisionDetectionMode.Discrete, CollisionDetectionMode.Continuous,
        CollisionDetectionMode.ContinuousDynamic, CollisionDetectionMode.ContinuousSpeculative,
    };

    int _callbacks = 1;
    int _queryMode;
    int _queryCount;
    int _paddleMode = 1;
    int _timestep;
    int _frameLoad;
    int _fixedLoad;
    int _timeScale;
    int _bulletMode;
    // 0 : primitive colliders only, 1 : one body in four is a convex mesh, 2 : all of them
    int _meshMode = 1;
    static readonly string[] MeshNames = { "primitive", "1 in 4 convex mesh", "all convex mesh" };
    bool _interpolate;
    bool _neverSleep;
    bool _hidePanel;

    // ---- State --------------------------------------------------------------------------

    static readonly ProfilerMarker SpawnMarker = new ProfilerMarker("Lab.Spawn");
    static readonly ProfilerMarker QueriesMarker = new ProfilerMarker("Lab.Queries");
    static readonly ProfilerMarker FixedLoadMarker = new ProfilerMarker("Lab.FixedLoad");
    static readonly ProfilerMarker FrameLoadMarker = new ProfilerMarker("Lab.FrameLoad");
    static readonly ProfilerMarker PaddlesFixedMarker = new ProfilerMarker("Lab.PaddlesInFixedUpdate");
    static readonly ProfilerMarker PaddlesUpdateMarker = new ProfilerMarker("Lab.PaddlesInUpdate");
    static readonly ProfilerMarker HudMarker = new ProfilerMarker("Lab.HudRefresh");

    readonly List<Rigidbody> _bodies = new List<Rigidbody>();
    readonly Queue<GameObject> _bullets = new Queue<GameObject>();
    readonly Collider[] _overlaps = new Collider[256];
    PhysicsMaterial _bouncy;
    Transform _root;

    float _paddleAngle_Degrees;
    int _stepsThisFrame;
    int _stepsLastFrame;

    // The number of fixed steps of each of the last frames : the bars of the panel.
    const int History = 90;
    readonly int[] _stepHistory = new int[History];
    int _historyHead;

    float _fps;
    float _nextRefresh;
    int _awake;
    int _enterBase;
    int _stayBase;
    float _enterPerSecond;
    float _stayPerSecond;
    float _stayWindowStart;

    class Entry
    {
        public Key Key;
        public string Label;
        public Func<string> Value;
        public Action Do;
        public string ValueText = "";
    }

    Entry[] _entries;
    GUIStyle _label;
    GUIStyle _value;
    GUIStyle _button;
    Texture2D _background;

    void Awake()
    {
        LabContactListener.Enters = 0;
        LabContactListener.Exits = 0;
        LabStayListener.Stays = 0;
        LabStayListener.WorkIterations = 0;
        LabTriggerZone.Inside = 0;
        LabTriggerZone.Enters = 0;
        LabTriggerZone.Stays = 0;

        _bouncy = new PhysicsMaterial("LabBouncy") { bounciness = 0.35f, dynamicFriction = 0.4f, staticFriction = 0.4f };
        _root = new GameObject("Bodies").transform;

        _entries = new[]
        {
            new Entry { Key = Key.B, Label = "Spawn 100 bodies", Value = () => _bodies.Count + " bodies", Do = () => SpawnBodies(100) },
            new Entry { Key = Key.N, Label = "Clear bodies", Value = () => "", Do = ClearBodies },
            new Entry { Key = Key.M, Label = "Colliders of new bodies", Value = () => MeshNames[_meshMode], Do = () => _meshMode = (_meshMode + 1) % MeshNames.Length },
            new Entry { Key = Key.C, Label = "Collision callbacks", Value = () => CallbackNames[_callbacks], Do = CycleCallbacks },
            new Entry { Key = Key.Q, Label = "Query type", Value = () => QueryNames[_queryMode], Do = () => _queryMode = (_queryMode + 1) % QueryNames.Length },
            new Entry { Key = Key.W, Label = "Queries per fixed step", Value = () => QueryCounts[_queryCount].ToString(), Do = () => _queryCount = (_queryCount + 1) % QueryCounts.Length },
            new Entry { Key = Key.P, Label = "Paddles move", Value = () => PaddleNames[_paddleMode], Do = () => _paddleMode = (_paddleMode + 1) % PaddleNames.Length },
            new Entry { Key = Key.T, Label = "Fixed timestep", Value = () => (Timesteps[_timestep] * 1000.0f).ToString("0.#") + " ms (" + (1.0f / Timesteps[_timestep]).ToString("0") + " Hz)", Do = CycleTimestep },
            new Entry { Key = Key.L, Label = "Update load", Value = () => FrameLoads_Ms[_frameLoad].ToString("0") + " ms per frame", Do = () => _frameLoad = (_frameLoad + 1) % FrameLoads_Ms.Length },
            new Entry { Key = Key.K, Label = "FixedUpdate load", Value = () => FixedLoads_Ms[_fixedLoad].ToString("0") + " ms per step", Do = () => _fixedLoad = (_fixedLoad + 1) % FixedLoads_Ms.Length },
            new Entry { Key = Key.Z, Label = "Time scale", Value = () => TimeScales[_timeScale].ToString("0.0#"), Do = CycleTimeScale },
            new Entry { Key = Key.I, Label = "Interpolation", Value = () => _interpolate ? "on" : "off", Do = ToggleInterpolation },
            new Entry { Key = Key.S, Label = "Sleeping", Value = () => _neverSleep ? "never" : "normal", Do = ToggleSleeping },
            new Entry { Key = Key.F, Label = "Fire the cannon", Value = () => "", Do = () => Fire() },
            new Entry { Key = Key.G, Label = "Bullet detection", Value = () => BulletModes[_bulletMode].ToString(), Do = () => _bulletMode = (_bulletMode + 1) % BulletModes.Length },
            new Entry { Key = Key.E, Label = "Explosion at the mouse", Value = () => "", Do = Explode },
            new Entry { Key = Key.H, Label = "Hide this panel", Value = () => "", Do = () => _hidePanel = !_hidePanel },
            new Entry { Key = Key.R, Label = "Reset the lab", Value = () => "", Do = ResetLab },
        };
    }

    void Start()
    {
        SpawnBodies(InitialBodies);
        RefreshHud();
    }

    void OnDestroy()
    {
        // Changed from a script, the time settings would otherwise be kept for the next scene.
        Time.fixedDeltaTime = 0.02f;
        Time.timeScale = 1.0f;
    }

    // ---- The loop ------------------------------------------------------------------------

    void FixedUpdate()
    {
        ++_stepsThisFrame;

        if (FixedLoads_Ms[_fixedLoad] > 0.0f)
        {
            using (FixedLoadMarker.Auto())
            {
                BurnTime(FixedLoads_Ms[_fixedLoad]);
            }
        }

        if (_queryCount > 0)
        {
            RunQueries();
        }

        if (_paddleMode == 1)
        {
            using (PaddlesFixedMarker.Auto())
            {
                _paddleAngle_Degrees += PaddleSpeed_DegreesPerSecond * Time.fixedDeltaTime;
                for (int i = 0; i < KinematicPaddles.Length; ++i)
                {
                    KinematicPaddles[i].MoveRotation(PaddleRotation(i));
                }
            }
        }
    }

    void Update()
    {
        // Update runs after the fixed steps of the frame : this is how many there were.
        _stepsLastFrame = _stepsThisFrame;
        _stepsThisFrame = 0;
        _stepHistory[_historyHead] = _stepsLastFrame;
        _historyHead = (_historyHead + 1) % History;

        _fps = Mathf.Lerp(_fps, 1.0f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            foreach (Entry entry in _entries)
            {
                if (keyboard[entry.Key].wasPressedThisFrame)
                {
                    entry.Do();
                }
            }
        }

        if (_paddleMode == 2 || _paddleMode == 3)
        {
            using (PaddlesUpdateMarker.Auto())
            {
                _paddleAngle_Degrees += PaddleSpeed_DegreesPerSecond * Time.deltaTime;
                if (_paddleMode == 2)
                {
                    for (int i = 0; i < KinematicPaddles.Length; ++i)
                    {
                        KinematicPaddles[i].transform.rotation = PaddleRotation(i);
                    }
                }
                else
                {
                    for (int i = 0; i < StaticPaddles.Length; ++i)
                    {
                        StaticPaddles[i].rotation = PaddleRotation(i);
                    }
                }
            }
        }

        if (FrameLoads_Ms[_frameLoad] > 0.0f)
        {
            using (FrameLoadMarker.Auto())
            {
                BurnTime(FrameLoads_Ms[_frameLoad]);
            }
        }

        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.25f;
            RefreshHud();
        }
    }

    // Something heavy in a script, as a busy wait.
    static void BurnTime(float milliseconds)
    {
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.Elapsed.TotalMilliseconds < milliseconds)
        {
        }
    }

    Quaternion PaddleRotation(int index)
    {
        // Every other paddle turns the other way.
        float direction = (index % 2 == 0) ? 1.0f : -1.0f;
        return Quaternion.Euler(0.0f, _paddleAngle_Degrees * direction + index * 37.0f, 0.0f);
    }

    void RunQueries()
    {
        using (QueriesMarker.Auto())
        {
            int count = QueryCounts[_queryCount];
            for (int i = 0; i < count; ++i)
            {
                Vector3 origin = new Vector3(Random.Range(-10.0f, 10.0f), Random.Range(1.0f, 6.0f), Random.Range(-10.0f, 10.0f));
                switch (_queryMode)
                {
                    case 0: Physics.Raycast(origin, Random.onUnitSphere, 20.0f); break;
                    case 1: Physics.RaycastAll(origin, Random.onUnitSphere, 20.0f); break;
                    case 2: Physics.OverlapSphere(origin, 1.5f); break;
                    default: Physics.OverlapSphereNonAlloc(origin, 1.5f, _overlaps); break;
                }
            }
        }
    }

    // ---- Things the keys do --------------------------------------------------------------

    // Public so that the scene builder can check the lab without playing it.
    public void SpawnBodies(int count)
    {
        using (SpawnMarker.Auto())
        {
            if (_root == null)
            {
                _root = new GameObject("Bodies").transform;
            }

            for (int i = 0; i < count; ++i)
            {
                int shape = Random.Range(0, 3);
                GameObject body = GameObject.CreatePrimitive(shape == 0 ? PrimitiveType.Sphere : (shape == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule));
                body.name = "Body";
                body.transform.SetParent(_root, false);
                body.transform.position = new Vector3(Random.Range(-7.0f, 9.0f), Random.Range(3.0f, 16.0f), Random.Range(-9.0f, 8.0f));
                body.transform.rotation = Random.rotation;

                float size = Random.Range(0.35f, 0.8f);
                body.transform.localScale = Vector3.one * size;
                if (BodyMaterials != null && BodyMaterials.Length > 0)
                {
                    body.GetComponent<Renderer>().sharedMaterial = BodyMaterials[shape % BodyMaterials.Length];
                }

                Collider collider = body.GetComponent<Collider>();
                if (_meshMode == 2 || (_meshMode == 1 && Random.value < 0.25f))
                {
                    // A convex hull cooked from the render mesh : what people do when they do not know about primitives.
                    DestroyImmediate(collider);
                    MeshCollider hull = body.AddComponent<MeshCollider>();
                    // The mesh first : AddComponent gave it the render mesh, and making THAT convex fails for the sphere and the capsule.
                    Mesh mesh = shape == 1 ? body.GetComponent<MeshFilter>().sharedMesh : HullMesh(shape == 2);
                    hull.sharedMesh = mesh;
                    hull.convex = true;

                    // Drawn with the hull too, so that the bodies with a mesh collider can be told apart : low-poly balls.
                    body.GetComponent<MeshFilter>().sharedMesh = mesh;
                    collider = hull;
                }
                collider.sharedMaterial = _bouncy;

                Rigidbody rigidbody = body.AddComponent<Rigidbody>();
                rigidbody.mass = size * size * size * 4.0f;
                rigidbody.interpolation = _interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                rigidbody.sleepThreshold = _neverSleep ? 0.0f : Physics.sleepThreshold;

                ApplyCallbacks(body);

                _bodies.Add(rigidbody);
            }
        }
    }

    // The built-in sphere and capsule have more triangles than a convex collider may have (256) : these have 80.
    // A low-poly ball, and the same stretched to the capsule's height (an ellipsoid : close enough to push things around).
    static Mesh _ballHull;
    static Mesh _capsuleHull;

    static Mesh HullMesh(bool capsule)
    {
        if (capsule && _capsuleHull != null) return _capsuleHull;
        if (!capsule && _ballHull != null) return _ballHull;

        float t = (1.0f + Mathf.Sqrt(5.0f)) * 0.5f;
        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        int[] faces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        // One subdivision : every edge gets a new vertex in the middle.
        Dictionary<long, int> middles = new Dictionary<long, int>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < faces.Length; i += 3)
        {
            int a = faces[i], b = faces[i + 1], c = faces[i + 2];
            int ab = Middle(vertices, middles, a, b), bc = Middle(vertices, middles, b, c), ca = Middle(vertices, middles, c, a);
            triangles.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }

        for (int i = 0; i < vertices.Count; ++i)
        {
            // The radius of the primitives is 0.5 ; the capsule is 2 high.
            vertices[i] = vertices[i].normalized * 0.5f;
            if (capsule) vertices[i] = new Vector3(vertices[i].x, vertices[i].y * 2.0f, vertices[i].z);
        }

        Mesh mesh = new Mesh { name = capsule ? "LabCapsuleHull" : "LabBallHull" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (capsule) _capsuleHull = mesh; else _ballHull = mesh;
        return mesh;
    }

    static int Middle(List<Vector3> vertices, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
        if (!cache.TryGetValue(key, out int index))
        {
            index = vertices.Count;
            vertices.Add((vertices[a] + vertices[b]) * 0.5f);
            cache[key] = index;
        }
        return index;
    }

    void ClearBodies()
    {
        foreach (Rigidbody body in _bodies)
        {
            if (body != null)
            {
                Destroy(body.gameObject);
            }
        }
        _bodies.Clear();
    }

    void CycleCallbacks()
    {
        _callbacks = (_callbacks + 1) % CallbackNames.Length;
        LabStayListener.WorkIterations = StayWork[_callbacks];
        foreach (Rigidbody body in _bodies)
        {
            if (body != null)
            {
                ApplyCallbacks(body.gameObject);
            }
        }
    }

    // The components are added and removed, not enabled and disabled : Unity sends collision events to disabled
    // behaviours too (so that they can enable themselves), and the cost we want to see is the cost of the callback.
    void ApplyCallbacks(GameObject body)
    {
        SetComponent<LabContactListener>(body, _callbacks >= 1);
        SetComponent<LabStayListener>(body, _callbacks >= 2);
    }

    static void SetComponent<T>(GameObject target, bool wanted) where T : Component
    {
        T existing = target.GetComponent<T>();
        if (wanted && existing == null)
        {
            target.AddComponent<T>();
        }
        else if (!wanted && existing != null)
        {
            Destroy(existing);
        }
    }

    void CycleTimestep()
    {
        _timestep = (_timestep + 1) % Timesteps.Length;
        Time.fixedDeltaTime = Timesteps[_timestep];
    }

    void CycleTimeScale()
    {
        _timeScale = (_timeScale + 1) % TimeScales.Length;
        Time.timeScale = TimeScales[_timeScale];
    }

    void ToggleInterpolation()
    {
        _interpolate = !_interpolate;
        foreach (Rigidbody body in _bodies)
        {
            if (body != null)
            {
                body.interpolation = _interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }
        }

        foreach (Rigidbody paddle in KinematicPaddles)
        {
            paddle.interpolation = _interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        }
    }

    void ToggleSleeping()
    {
        _neverSleep = !_neverSleep;
        foreach (Rigidbody body in _bodies)
        {
            if (body != null)
            {
                body.sleepThreshold = _neverSleep ? 0.0f : Physics.sleepThreshold;
                body.WakeUp();
            }
        }
    }

    public GameObject Fire()
    {
        if (Cannon == null)
        {
            return null;
        }

        // Only the last few bullets stay around.
        while (_bullets.Count >= 12)
        {
            Destroy(_bullets.Dequeue());
        }

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet";
        bullet.transform.position = Cannon.position + Cannon.forward;
        bullet.transform.localScale = Vector3.one * 0.3f;
        if (BulletMaterial != null)
        {
            bullet.GetComponent<Renderer>().sharedMaterial = BulletMaterial;
        }

        Rigidbody rigidbody = bullet.AddComponent<Rigidbody>();
        rigidbody.mass = 2.0f;
        rigidbody.useGravity = false;
        rigidbody.collisionDetectionMode = BulletModes[_bulletMode];
        rigidbody.linearVelocity = Cannon.forward * BulletSpeed;

        _bullets.Enqueue(bullet);
        return bullet;
    }

    void Explode()
    {
        Mouse mouse = Mouse.current;
        Camera camera = Camera.main;
        if (mouse == null || camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200.0f))
        {
            return;
        }

        int count = Physics.OverlapSphereNonAlloc(hit.point, 6.0f, _overlaps);
        for (int i = 0; i < count; ++i)
        {
            Rigidbody body = _overlaps[i].attachedRigidbody;
            if (body != null && !body.isKinematic)
            {
                // Acceleration : the same push whatever the mass. Measured, 500 throws a body at about 10 m/s, and none over the walls.
                body.AddExplosionForce(ExplosionAcceleration, hit.point, 6.0f, 0.5f, ForceMode.Acceleration);
            }
        }
    }

    // Back to how the lab starts : the settings, the bodies, the bullets. The chains and the paddles are left as they are.
    void ResetLab()
    {
        ClearBodies();
        while (_bullets.Count > 0)
        {
            Destroy(_bullets.Dequeue());
        }

        _callbacks = 1;
        _queryMode = 0;
        _queryCount = 0;
        _paddleMode = 1;
        _timestep = 0;
        _frameLoad = 0;
        _fixedLoad = 0;
        _timeScale = 0;
        _bulletMode = 0;
        _meshMode = 1;
        _interpolate = false;
        _neverSleep = false;
        LabStayListener.WorkIterations = 0;
        Time.fixedDeltaTime = Timesteps[0];
        Time.timeScale = TimeScales[0];
        foreach (Rigidbody paddle in KinematicPaddles)
        {
            paddle.interpolation = RigidbodyInterpolation.None;
        }

        SpawnBodies(InitialBodies);
        RefreshHud();
    }

    // ---- The panel ----------------------------------------------------------------------

    void RefreshHud()
    {
        using (HudMarker.Auto())
        {
            _awake = 0;
            foreach (Rigidbody body in _bodies)
            {
                if (body != null && !body.IsSleeping())
                {
                    ++_awake;
                }
            }

            float window = Time.unscaledTime - _stayWindowStart;
            if (window >= 1.0f)
            {
                _enterPerSecond = (LabContactListener.Enters - _enterBase) / window;
                _stayPerSecond = (LabStayListener.Stays - _stayBase) / window;
                _enterBase = LabContactListener.Enters;
                _stayBase = LabStayListener.Stays;
                _stayWindowStart = Time.unscaledTime;
            }

            foreach (Entry entry in _entries)
            {
                entry.ValueText = entry.Value();
            }
        }
    }

    void OnGUI()
    {
        if (_label == null)
        {
            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 0.65f));
            _background.Apply();

            _label = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            _label.normal.textColor = Color.white;
            _value = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            _value.normal.textColor = new Color(1.0f, 0.85f, 0.4f);
            _button = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        }

        if (_hidePanel)
        {
            // Hidden : only a way back. The panel is not free (IMGUI allocates), so hide it before profiling the GC.
            GUI.Label(new Rect(Screen.width - 190, 4, 186, 20), "[H] show the panel", _label);
            return;
        }

        float scale = Mathf.Clamp(Screen.height / 720.0f, 0.7f, 2.0f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));
        float screenWidth = Screen.width / scale;

        const float Width = 420.0f;
        const float Row = 21.0f;
        float x = screenWidth - Width - 8.0f;
        float y = 8.0f;

        GUI.DrawTexture(new Rect(x - 6.0f, y - 4.0f, Width + 12.0f, 7.0f * Row + 56.0f + _entries.Length * Row + 12.0f), _background);

        // -- Numbers
        Line(ref y, x, Width, "Frame", (1000.0f / Mathf.Max(_fps, 1.0f)).ToString("0.0") + " ms  (" + _fps.ToString("0") + " fps)");
        Line(ref y, x, Width, "Fixed steps in the last frame", _stepsLastFrame.ToString());
        Line(ref y, x, Width, "Bodies  (awake)", _bodies.Count + "  (" + _awake + ")");
        Line(ref y, x, Width, "OnCollisionEnter / s", _enterPerSecond.ToString("0"));
        Line(ref y, x, Width, "OnCollisionStay / s", _stayPerSecond.ToString("0"));
        Line(ref y, x, Width, "Trigger zone  (inside, stays)", LabTriggerZone.Inside + ", " + LabTriggerZone.Stays);

        // -- One bar per frame : how many fixed steps ran in it.
        Rect bars = new Rect(x, y + 2.0f, Width, 40.0f);
        y += 46.0f;
        GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);
        GUI.DrawTexture(bars, Texture2D.whiteTexture);
        float barWidth = Width / History;
        for (int i = 0; i < History; ++i)
        {
            int steps = _stepHistory[(_historyHead + i) % History];
            float height = steps == 0 ? 2.0f : Mathf.Min(8.0f * steps + 6.0f, bars.height);
            GUI.color = steps == 0 ? new Color(0.6f, 0.6f, 0.6f) : (steps == 1 ? new Color(0.4f, 0.9f, 0.4f) : new Color(1.0f, 0.55f, 0.2f));
            GUI.DrawTexture(new Rect(bars.x + i * barWidth, bars.yMax - height, Mathf.Max(barWidth - 1.0f, 1.0f), height), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y - 6.0f, Width, Row), "grey : no step    green : 1    orange : 2 or more", _label);
        y += Row - 2.0f;

        // -- The keys
        for (int i = 0; i < _entries.Length; ++i)
        {
            Entry entry = _entries[i];
            if (GUI.Button(new Rect(x, y + 1.0f, 28.0f, Row - 2.0f), entry.Key.ToString(), _button))
            {
                entry.Do();
                entry.ValueText = entry.Value();
            }
            GUI.Label(new Rect(x + 34.0f, y, 180.0f, Row), entry.Label, _label);
            GUI.Label(new Rect(x + 214.0f, y, Width - 214.0f, Row), entry.ValueText, _value);
            y += Row;
        }

        GUI.matrix = Matrix4x4.identity;
    }

    void Line(ref float y, float x, float width, string label, string value)
    {
        GUI.Label(new Rect(x, y, width * 0.6f, 21.0f), label, _label);
        GUI.Label(new Rect(x + width * 0.45f, y, width * 0.55f, 21.0f), value, _value);
        y += 21.0f;
    }
}
