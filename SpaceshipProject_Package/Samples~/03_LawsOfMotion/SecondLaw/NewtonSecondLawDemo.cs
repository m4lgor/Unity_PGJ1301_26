using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Newton's second law - F = m * a, better read as a = F / m.
/// A net force does not produce speed, it produces ACCELERATION, and the same force
/// produces less of it on a heavier body.
///
/// Four probes float in space, gravity off, no damping. Each one is pushed by a constant
/// force for the whole run, and the only things that change from one lane to the next are
/// the force and the mass:
///   1. m = 1, F = 1  ->  a = 1.0   the reference
///   2. m = 2, F = 1  ->  a = 0.5   twice the mass, half the acceleration
///   3. m = 1, F = 2  ->  a = 2.0   twice the force, twice the acceleration
///   4. m = 2, F = 2  ->  a = 1.0   both doubled, so it ends level with probe 1
///
/// Probe 4 is the punchline: neither the force alone nor the mass alone decides the
/// motion, only their ratio does.
///
/// Same stroboscope as the first law scene: a breadcrumb every BreadcrumbInterval
/// seconds. Under a constant force the crumbs SPREAD OUT instead of staying evenly
/// spaced, and that widening gap is exactly what acceleration looks like.
///
/// The panel prints the measured speed and distance next to the values predicted by
/// a = F / m, so the law can be checked and not just asserted.
/// </summary>
public class NewtonSecondLawDemo : MonoBehaviour
{
    [System.Serializable]
    public class Probe
    {
        public string Label;

        [Tooltip("The mass lives on this Rigidbody, it is not repeated here")]
        public Rigidbody Body;

        [Tooltip("Constant force pushing along PushDirection for the whole run, in newtons")]
        public float Force;

        [Tooltip("One line shown in the panel: what this probe is here to prove")]
        public string Comment;

        [Tooltip("Colour of this probe's row in the panel. Match it to the material")]
        public Color PanelColour = Color.white;

        [System.NonSerialized] public Vector3 StartPosition;
        [System.NonSerialized] public Transform Trail;
        [System.NonSerialized] public Material TrailMaterial;

        // Frozen at the end of the run so the comparison pause shows the final numbers.
        [System.NonSerialized] public float ShownSpeed;
        [System.NonSerialized] public float ShownDistance;
    }

    [SerializeField] Probe[] Probes;

    [Header("Run")]
    [Tooltip("Direction the constant force pushes along")]
    [SerializeField] Vector3 PushDirection = Vector3.right;

    [Tooltip("How long the force is applied")]
    [SerializeField] float RunDuration = 5.0f;

    [Tooltip("Everything freezes for this long afterwards, to compare where each probe got")]
    [SerializeField] float HoldDuration = 3.0f;

    [SerializeField] Key RestartKey = Key.R;

    [Header("Breadcrumbs")]
    [Tooltip("A marker dropped at a fixed time interval. A widening gap = acceleration")]
    [SerializeField] bool DropBreadcrumbs = true;

    [SerializeField] float BreadcrumbInterval = 0.5f;
    [SerializeField] float BreadcrumbSize = 0.22f;

    [Header("Display")]
    [SerializeField] bool DisplayGUI = true;
    [SerializeField] bool DisplayGizmos = true;

    [Tooltip("A force of 1 N is drawn this many metres long")]
    [SerializeField] float ForceGizmoScale = 1.5f;

    float _PhaseTime;
    bool _Holding;
    int _CrumbsDropped;

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
    /// Puts every probe back on the start line, at rest, and starts pushing again.
    /// </summary>
    [ContextMenu("Start Run")]
    void StartRun()
    {
        _PhaseTime = 0.0f;
        _Holding = false;
        _CrumbsDropped = 0;

        foreach (Probe probe in Probes)
        {
            if (probe.Body == null)
            {
                continue;
            }

            probe.Body.isKinematic = false;
            probe.Body.position = probe.StartPosition;
            probe.Body.rotation = Quaternion.identity;
            probe.Body.linearVelocity = Vector3.zero;
            probe.Body.angularVelocity = Vector3.zero;

            probe.ShownSpeed = 0.0f;
            probe.ShownDistance = 0.0f;

            ClearTrail(probe);
        }
    }

    /// <summary>
    /// Stops the world so the four final positions can be read side by side. The numbers
    /// in the panel stay on their last value instead of falling back to zero.
    /// </summary>
    void EnterHold()
    {
        _PhaseTime = 0.0f;
        _Holding = true;

        foreach (Probe probe in Probes)
        {
            if (probe.Body != null)
            {
                probe.Body.isKinematic = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (Probes == null || _Holding)
        {
            return;
        }

        Vector3 direction = PushDirection.normalized;

        foreach (Probe probe in Probes)
        {
            // A constant force, applied every physics step, for the whole run. Nothing
            // else touches the probe: no gravity, no damping, no collisions.
            if (probe.Body != null)
            {
                probe.Body.AddForce(direction * probe.Force, ForceMode.Force);
            }
        }
    }

    void Update()
    {
        _PhaseTime += Time.deltaTime;

        if (!_Holding)
        {
            SampleProbes();

            if (DropBreadcrumbs)
            {
                // The first crumb belongs on the start line, hence the + 1.
                int wanted = Mathf.FloorToInt(_PhaseTime / BreadcrumbInterval) + 1;
                while (_CrumbsDropped < wanted)
                {
                    DropBreadcrumbRow();
                }
            }

            if (_PhaseTime >= RunDuration)
            {
                EnterHold();
            }
        }
        else if (_PhaseTime >= HoldDuration)
        {
            StartRun();
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[RestartKey].wasPressedThisFrame)
        {
            StartRun();
        }
    }

    void SampleProbes()
    {
        foreach (Probe probe in Probes)
        {
            if (probe.Body == null)
            {
                continue;
            }

            probe.ShownSpeed = probe.Body.linearVelocity.magnitude;
            probe.ShownDistance = (probe.Body.position - probe.StartPosition).magnitude;
        }
    }

    void DropBreadcrumbRow()
    {
        _CrumbsDropped++;

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
    /// The acceleration the law predicts for this probe. Nothing here is measured: it is
    /// the force divided by the mass, and the panel compares it to what actually happened.
    /// </summary>
    float PredictedAcceleration(Probe probe)
    {
        if (probe.Body == null || probe.Body.mass <= 0.0f)
        {
            return 0.0f;
        }

        return probe.Force / probe.Body.mass;
    }

    float ElapsedInRun()
    {
        return _Holding ? RunDuration : Mathf.Min(_PhaseTime, RunDuration);
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

        GUILayout.Label("Newton's Second Law - F = m a", _Header);
        GUILayout.Label("A net force does not produce speed, it produces "
                        + "<b>acceleration</b>. The same force moves a heavy body less: "
                        + "<b>a = F / m</b>.", _Item);

        GUILayout.Space(4.0f);
        GUILayout.Label("Everyone starts at rest. The red bar behind each probe is its "
                        + "force, drawn to scale. The crumbs <b>spread out</b>, and that "
                        + "widening gap is the acceleration.", _Item);

        GUILayout.Space(6.0f);

        foreach (Probe probe in Probes)
        {
            DrawProbeRow(probe);
        }

        GUILayout.Space(6.0f);
        GUILayout.Label((_Holding ? "COMPARING - " : "PUSHING - ")
                        + "t = <b>" + ElapsedInRun().ToString("0.0") + " s</b> / "
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

        float t = ElapsedInRun();
        float a = PredictedAcceleration(probe);

        // What the law says should have happened by now, starting from rest.
        float predictedSpeed = a * t;
        float predictedDistance = 0.5f * a * t * t;

        string colour = ColorUtility.ToHtmlStringRGB(probe.PanelColour);

        GUILayout.BeginVertical(_Row);

        GUILayout.Label("<color=#" + colour + "><b>" + probe.Label + "</b></color>", _Item);
        GUILayout.Label(probe.Comment, _Item);

        GUILayout.Label("m = <b>" + probe.Body.mass.ToString("0.#") + " kg</b>"
                        + "      F = <b>" + probe.Force.ToString("0.0") + " N</b>"
                        + "      a = F/m = <b>" + a.ToString("0.00") + " m/s^2</b>", _Punch);

        GUILayout.Label("speed <b>" + probe.ShownSpeed.ToString("0.00") + "</b> m/s"
                        + "   <i>(a t = " + predictedSpeed.ToString("0.00") + ")</i>", _Item);
        GUILayout.Label("distance <b>" + probe.ShownDistance.ToString("0.00") + "</b> m"
                        + "   <i>(a t t / 2 = " + predictedDistance.ToString("0.00")
                        + ")</i>", _Item);

        GUILayout.EndVertical();
    }

    void OnDrawGizmos()
    {
        if (!DisplayGizmos || Probes == null)
        {
            return;
        }

        Vector3 direction = PushDirection.normalized;

        foreach (Probe probe in Probes)
        {
            if (probe.Body == null)
            {
                continue;
            }

            Vector3 position = probe.Body.position;

            // The cause: a force, drawn to scale so the lanes can be compared.
            Gizmos.color = Color.red;
            DrawUtils.DrawArrow(position, direction * probe.Force * ForceGizmoScale);

            // The effect: a velocity that keeps growing, unlike the first law scene.
            Gizmos.color = Color.cyan;
            DrawUtils.DrawArrow(position, probe.Body.linearVelocity);
        }
    }
}
