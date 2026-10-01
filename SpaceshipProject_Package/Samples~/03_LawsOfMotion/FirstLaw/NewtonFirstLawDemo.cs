using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Newton's first law - the law of inertia.
/// A body keeps its velocity, meaning its SPEED and its DIRECTION, until a net external
/// force acts on it.
///
/// Four probes float in space, gravity switched off on every Rigidbody. They all receive
/// the exact same push at the start of a run. After that push, only the forces acting on
/// them differ:
///   1. never pushed      -> stays at rest, on its own
///   2. no force at all   -> constant velocity, forever. This is the law.
///   3. linear damping    -> a force against the motion, so the speed drops to zero
///   4. a sideways force  -> the speed is kept, but the direction bends
///
/// Each probe drops a breadcrumb every BreadcrumbInterval seconds, so the trail is a
/// picture of the velocity itself: even spacing means constant speed, and a straight
/// trail means constant direction. Probe 2 is the only one whose trail is both.
/// </summary>
public class NewtonFirstLawDemo : MonoBehaviour
{
    [System.Serializable]
    public class Probe
    {
        public string Label;
        public Rigidbody Body;

        [Tooltip("Speed given by the single push at the start of the run, in m/s. " +
                 "Nothing touches the probe afterwards")]
        public float LaunchSpeed;

        [Tooltip("Net external force applied at every physics step, in newtons. " +
                 "Zero means the probe is left completely alone")]
        public Vector3 ConstantForce;

        [Tooltip("One line shown in the panel: what this probe is here to prove")]
        public string Comment;

        [Tooltip("Colour of this probe's row in the panel. Match it to the material")]
        public Color PanelColour = Color.white;

        [System.NonSerialized] public Vector3 StartPosition;
        [System.NonSerialized] public Transform Trail;
        [System.NonSerialized] public Material TrailMaterial;
    }

    [SerializeField] Probe[] Probes;

    [Header("Run")]
    [Tooltip("Direction of the single push that starts the run")]
    [SerializeField] Vector3 LaunchDirection = Vector3.right;

    [Tooltip("The run restarts by itself after this many seconds")]
    [SerializeField] float RunDuration = 12.0f;

    [SerializeField] Key RestartKey = Key.R;

    [Header("Breadcrumbs")]
    [Tooltip("A marker dropped at a fixed time interval. Even spacing = constant speed")]
    [SerializeField] bool DropBreadcrumbs = true;

    [SerializeField] float BreadcrumbInterval = 0.5f;
    [SerializeField] float BreadcrumbSize = 0.22f;

    [Header("Display")]
    [SerializeField] bool DisplayGUI = true;
    [SerializeField] bool DisplayGizmos = true;

    [Tooltip("A velocity of 1 m/s is drawn this many metres long")]
    [SerializeField] float VelocityGizmoScale = 1.5f;

    float _RunTime;
    float _BreadcrumbTime;

    GUIStyle _Panel, _Header, _Item, _Punch, _Row;

    void Start()
    {
        if (Probes == null)
        {
            return;
        }

        foreach (Probe probe in Probes)
        {
            if (probe.Body == null)
            {
                continue;
            }

            probe.StartPosition = probe.Body.position;

            Renderer renderer = probe.Body.GetComponentInChildren<Renderer>();
            probe.TrailMaterial = renderer != null ? renderer.sharedMaterial : null;

            GameObject trail = new GameObject(probe.Label + " trail");
            trail.transform.SetParent(transform, false);
            probe.Trail = trail.transform;
        }

        StartRun();
    }

    /// <summary>
    /// Puts every probe back on its start position and gives it its one and only push.
    /// </summary>
    [ContextMenu("Start Run")]
    void StartRun()
    {
        _RunTime = 0.0f;
        _BreadcrumbTime = 0.0f;

        Vector3 direction = LaunchDirection.normalized;

        foreach (Probe probe in Probes)
        {
            if (probe.Body == null)
            {
                continue;
            }

            probe.Body.position = probe.StartPosition;
            probe.Body.rotation = Quaternion.identity;
            probe.Body.linearVelocity = Vector3.zero;
            probe.Body.angularVelocity = Vector3.zero;

            // The push itself. VelocityChange ignores the mass, so every probe leaves the
            // start line at exactly LaunchSpeed and the only difference left between them
            // is the force acting afterwards. Mass is the subject of the second law.
            probe.Body.AddForce(direction * probe.LaunchSpeed, ForceMode.VelocityChange);

            ClearTrail(probe);
        }
    }

    void FixedUpdate()
    {
        if (Probes == null)
        {
            return;
        }

        foreach (Probe probe in Probes)
        {
            // This is the ONLY thing the demo does to a probe once the run has started.
            // A probe with a zero ConstantForce is never touched again.
            if (probe.Body != null && probe.ConstantForce != Vector3.zero)
            {
                probe.Body.AddForce(probe.ConstantForce, ForceMode.Force);
            }
        }
    }

    void Update()
    {
        _RunTime += Time.deltaTime;

        if (DropBreadcrumbs)
        {
            _BreadcrumbTime += Time.deltaTime;
            while (_BreadcrumbTime >= BreadcrumbInterval)
            {
                _BreadcrumbTime -= BreadcrumbInterval;
                DropBreadcrumbRow();
            }
        }

        Keyboard keyboard = Keyboard.current;
        bool restartAsked = keyboard != null && keyboard[RestartKey].wasPressedThisFrame;

        if (restartAsked || _RunTime >= RunDuration)
        {
            StartRun();
        }
    }

    void DropBreadcrumbRow()
    {
        foreach (Probe probe in Probes)
        {
            if (probe.Body == null || probe.Trail == null)
            {
                continue;
            }

            GameObject crumb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crumb.name = "Crumb";
            Destroy(crumb.GetComponent<Collider>());

            crumb.transform.SetParent(probe.Trail, false);
            crumb.transform.position = probe.Body.position;
            crumb.transform.localScale = Vector3.one * BreadcrumbSize;

            if (probe.TrailMaterial != null)
            {
                crumb.GetComponent<Renderer>().sharedMaterial = probe.TrailMaterial;
            }
        }
    }

    void ClearTrail(Probe probe)
    {
        if (probe.Trail == null)
        {
            return;
        }

        for (int i = probe.Trail.childCount - 1; i >= 0; i--)
        {
            Destroy(probe.Trail.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// The net external force on a probe: what we push with, plus what the linear damping
    /// takes away. Damping is not a setting, it is a force.
    /// </summary>
    Vector3 NetForce(Probe probe)
    {
        if (probe.Body == null)
        {
            return Vector3.zero;
        }

        Vector3 damping = -probe.Body.linearVelocity * probe.Body.linearDamping * probe.Body.mass;
        return probe.ConstantForce + damping;
    }

    void EnsureStyles()
    {
        if (_Panel != null)
        {
            return;
        }

        _Panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
        _Header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
        _Item = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        _Punch = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontStyle = FontStyle.Bold };
        _Row = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
    }

    void OnGUI()
    {
        if (!DisplayGUI)
        {
            return;
        }

        EnsureStyles();

        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 420.0f, Screen.height - 20.0f));
        GUILayout.BeginVertical(_Panel);

        GUILayout.Label("Newton's First Law - Inertia", _Header);
        GUILayout.Label("A body keeps its velocity - its <b>speed</b> AND its <b>direction</b> - "
                        + "until a <b>net external force</b> acts on it.", _Item);

        GUILayout.Space(4.0f);
        GUILayout.Label("Same push for everyone at t = 0, then nothing but the forces listed "
                        + "below. A breadcrumb is dropped every "
                        + BreadcrumbInterval.ToString("0.0")
                        + " s, so <b>even spacing = constant speed</b>.", _Item);

        GUILayout.Space(6.0f);

        foreach (Probe probe in Probes)
        {
            DrawProbeRow(probe);
        }

        GUILayout.Space(6.0f);
        GUILayout.Label("t = <b>" + _RunTime.ToString("0.0") + " s</b> / "
                        + RunDuration.ToString("0.0") + " s      press <b>"
                        + RestartKey + "</b> to restart", _Item);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void DrawProbeRow(Probe probe)
    {
        if (probe.Body == null)
        {
            return;
        }

        float speed = probe.Body.linearVelocity.magnitude;
        Vector3 net = NetForce(probe);

        // Below this the value is only solver noise, and the probe is really left alone.
        bool freeOfForce = net.magnitude < 0.0005f;

        string colour = ColorUtility.ToHtmlStringRGB(probe.PanelColour);

        GUILayout.BeginVertical(_Row);

        GUILayout.Label("<color=#" + colour + "><b>" + probe.Label + "</b></color>", _Item);
        GUILayout.Label(probe.Comment, _Item);
        GUILayout.Label("net force <b>" + net.magnitude.ToString("0.00") + " N</b>"
                        + "      speed <b>" + speed.ToString("0.00") + " m/s</b>", _Item);

        if (freeOfForce)
        {
            GUILayout.Label("<color=#7CFC7C>no force -> velocity unchanged</color>", _Punch);
        }
        else
        {
            GUILayout.Label("<color=#FF8080>a force acts -> velocity changes</color>", _Punch);
        }

        GUILayout.EndVertical();
    }

    void OnDrawGizmos()
    {
        if (!DisplayGizmos || Probes == null)
        {
            return;
        }

        foreach (Probe probe in Probes)
        {
            if (probe.Body == null)
            {
                continue;
            }

            Vector3 position = probe.Body.position;

            // Where the probe is heading, and how fast.
            Gizmos.color = Color.cyan;
            DrawUtils.DrawArrow(position, probe.Body.linearVelocity * VelocityGizmoScale);

            // The force that is bending or braking that velocity, when there is one.
            if (probe.ConstantForce != Vector3.zero)
            {
                Gizmos.color = Color.red;
                DrawUtils.DrawArrow(position, probe.ConstantForce.normalized * 2.0f);
            }
        }
    }
}
