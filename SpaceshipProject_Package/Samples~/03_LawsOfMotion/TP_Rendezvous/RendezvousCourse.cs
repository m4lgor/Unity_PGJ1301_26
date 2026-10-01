using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The exercise harness for TP_Rendezvous. Not for students to edit.
///
/// Holds the list of targets, decides when the probe has actually PARKED on one (close
/// enough AND slow enough, for long enough), moves on to the next, and reports.
///
/// Arriving is not the exercise. Arriving at zero speed is. In space nothing slows you
/// down (first law), so the only way to stop is to spend as much thrust braking as you
/// spent accelerating, and to start braking at the right moment (second law).
/// </summary>
public class RendezvousCourse : MonoBehaviour
{
    [Header("Course")]
    [SerializeField] Rigidbody Probe;

    [Tooltip("The probe parks on these in order")]
    [SerializeField] Transform[] Targets;

    [Tooltip("Moved onto whichever target is currently active")]
    [SerializeField] Transform ActiveMarker;

    [Tooltip("Kept clear of the target itself, so the marker does not hide it")]
    [SerializeField] Vector3 ActiveMarkerOffset = new Vector3(0.0f, 2.8f, 0.0f);

    [Header("Force indicator")]
    [Tooltip("Exhaust plume drawn opposite the force actually applied this step")]
    [SerializeField] Transform ForceBar;

    [SerializeField] float ForceBarScale = 0.5f;

    [Header("What counts as parked")]
    [SerializeField] float ArrivalRadius = 0.75f;
    [SerializeField] float ArrivalSpeed = 0.3f;

    [Tooltip("Both conditions have to hold for this long before the target counts")]
    [SerializeField] float HoldTime = 0.4f;

    [Header("Breadcrumbs")]
    [Tooltip("A marker dropped at a fixed time interval: bunching up means you are braking")]
    [SerializeField] bool DropBreadcrumbs = true;

    [SerializeField] float BreadcrumbInterval = 0.5f;
    [SerializeField] float BreadcrumbSize = 0.22f;
    [SerializeField] Material BreadcrumbMaterial;

    [Header("Display")]
    [SerializeField] bool DisplayGUI = true;
    [SerializeField] Key RestartKey = Key.R;

    int _Index;
    float _HoldTimer;
    float _CourseTime;
    bool _Complete;
    int _Overshoots;
    float _PreviousOffset;
    bool _HasPreviousOffset;

    Vector3 _ProbeStart;
    Transform _Trail;
    float _CrumbTimer;

    Vector3 _VelocityLastStep;
    Vector3 _MeasuredForce;

    GUIStyle _Panel, _Header, _Item, _Punch;

    /// <summary>
    /// Where the probe should be heading right now. Read by RendezvousControllerBase.
    /// </summary>
    public Vector3 CurrentTargetPosition
    {
        get
        {
            if (Targets == null || Targets.Length == 0 || _Index >= Targets.Length)
            {
                return Probe != null ? Probe.position : transform.position;
            }

            return Targets[_Index].position;
        }
    }

    void Start()
    {
        if (Probe != null)
        {
            _ProbeStart = Probe.position;
        }

        GameObject trail = new GameObject("Trail");
        trail.transform.SetParent(transform, false);
        _Trail = trail.transform;

        WarnAboutUnreachableTargets();
        Restart();
    }

    /// <summary>
    /// The probe is constrained to the X axis, so a target that is off its Y/Z plane can
    /// never be parked on: the distance can never drop below the offset. Worse, the
    /// direction to the target tilts out of the axis as you close in, which makes the
    /// closing speed collapse towards zero and any sane controller overshoot.
    ///
    /// Silent and baffling, so say it out loud instead.
    /// </summary>
    void WarnAboutUnreachableTargets()
    {
        if (Probe == null || Targets == null)
        {
            return;
        }

        foreach (Transform target in Targets)
        {
            if (target == null)
            {
                continue;
            }

            Vector3 offset = target.position - Probe.position;
            float outOfPlane = new Vector2(offset.y, offset.z).magnitude;

            if (outOfPlane > ArrivalRadius)
            {
                Debug.LogError("RendezvousCourse - target '" + target.name + "' is "
                               + outOfPlane.ToString("0.00") + " m off the probe's axis, "
                               + "which is more than the arrival radius of "
                               + ArrivalRadius.ToString("0.00")
                               + " m. The probe can never park on it. Put it on the same "
                               + "Y and Z as the probe.", target);
            }
        }
    }

    [ContextMenu("Restart")]
    void Restart()
    {
        _Index = 0;
        _HoldTimer = 0.0f;
        _CourseTime = 0.0f;
        _CrumbTimer = 0.0f;
        _Complete = false;
        _Overshoots = 0;
        _HasPreviousOffset = false;
        _MeasuredForce = Vector3.zero;
        _VelocityLastStep = Vector3.zero;

        if (Probe != null)
        {
            Probe.position = _ProbeStart;
            Probe.linearVelocity = Vector3.zero;
            Probe.angularVelocity = Vector3.zero;
        }

        if (_Trail != null)
        {
            for (int i = _Trail.childCount - 1; i >= 0; i--)
            {
                Destroy(_Trail.GetChild(i).gameObject);
            }
        }

        UpdateActiveMarker();
    }

    /// <summary>
    /// Measures the force the student's code actually applied, without trusting it to
    /// report anything: F = m dv / dt over one physics step.
    ///
    /// Every FixedUpdate in the frame runs BEFORE the physics step integrates, so reading
    /// linearVelocity here always gives the value from before this step. Comparing two
    /// consecutive readings therefore spans exactly one step, whatever order this
    /// component and the student's happen to run in.
    ///
    /// It is exact here only because nothing else touches the probe: no gravity, no
    /// damping, no contacts.
    /// </summary>
    void FixedUpdate()
    {
        if (Probe == null)
        {
            return;
        }

        Vector3 velocity = Probe.linearVelocity;
        _MeasuredForce = Probe.mass * (velocity - _VelocityLastStep) / Time.fixedDeltaTime;
        _VelocityLastStep = velocity;

        UpdateForceBar();
    }

    /// <summary>
    /// Shows what the probe was really pushed with, not what the code asked for, drawn
    /// as an exhaust plume: it comes out the opposite side to the force. While braking,
    /// the plume is therefore on the same side as the target.
    /// </summary>
    void UpdateForceBar()
    {
        if (ForceBar == null)
        {
            return;
        }

        float length = _MeasuredForce.magnitude * ForceBarScale;
        if (length < 0.02f)
        {
            ForceBar.gameObject.SetActive(false);
            return;
        }

        // Drawn as the exhaust, so it comes out of the OPPOSITE side to the push, the way
        // a real thruster does. Pushing right puts the plume on the left. Drawing it on
        // the same side as the force reads as if the probe were being pulled along.
        Vector3 plume = -_MeasuredForce.normalized;

        ForceBar.gameObject.SetActive(true);
        ForceBar.position = Probe.position + plume * (0.5f + length * 0.5f);
        ForceBar.rotation = Quaternion.LookRotation(Vector3.forward, plume);
        ForceBar.localScale = new Vector3(0.12f, length, 0.12f);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[RestartKey].wasPressedThisFrame)
        {
            Restart();
        }

        if (Probe == null || Targets == null || Targets.Length == 0)
        {
            return;
        }

        if (_Complete)
        {
            return;
        }

        _CourseTime += Time.deltaTime;

        CountOvershoots();
        CheckArrival();
        DropCrumbs();
    }

    /// <summary>
    /// Every time the probe crosses the target and comes out the other side, it has spent
    /// thrust it did not need to. Worth counting: it is the whole point of the exercise.
    /// </summary>
    void CountOvershoots()
    {
        float offset = Vector3.Dot(Probe.position - CurrentTargetPosition, Vector3.right);

        if (_HasPreviousOffset && Mathf.Sign(offset) != Mathf.Sign(_PreviousOffset)
            && Mathf.Abs(offset) > 0.01f && Mathf.Abs(_PreviousOffset) > 0.01f)
        {
            _Overshoots++;
        }

        _PreviousOffset = offset;
        _HasPreviousOffset = true;
    }

    void CheckArrival()
    {
        bool closeEnough = Distance() <= ArrivalRadius;
        bool slowEnough = Speed() <= ArrivalSpeed;

        if (closeEnough && slowEnough)
        {
            _HoldTimer += Time.deltaTime;
        }
        else
        {
            _HoldTimer = 0.0f;
        }

        if (_HoldTimer < HoldTime)
        {
            return;
        }

        _HoldTimer = 0.0f;
        _HasPreviousOffset = false;
        _Index++;

        if (_Index >= Targets.Length)
        {
            _Complete = true;
        }

        UpdateActiveMarker();
    }

    void UpdateActiveMarker()
    {
        if (ActiveMarker == null)
        {
            return;
        }

        bool running = !_Complete && Targets != null && _Index < Targets.Length;
        ActiveMarker.gameObject.SetActive(running);

        if (running)
        {
            ActiveMarker.position = Targets[_Index].position + ActiveMarkerOffset;
        }
    }

    void DropCrumbs()
    {
        if (!DropBreadcrumbs || _Trail == null)
        {
            return;
        }

        _CrumbTimer += Time.deltaTime;
        while (_CrumbTimer >= BreadcrumbInterval)
        {
            _CrumbTimer -= BreadcrumbInterval;

            GameObject crumb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crumb.name = "Crumb";
            Destroy(crumb.GetComponent<Collider>());

            crumb.transform.SetParent(_Trail, false);
            crumb.transform.position = Probe.position;
            crumb.transform.localScale = Vector3.one * BreadcrumbSize;

            if (BreadcrumbMaterial != null)
            {
                crumb.GetComponent<Renderer>().sharedMaterial = BreadcrumbMaterial;
            }
        }
    }

    float Distance()
    {
        return Vector3.Distance(Probe.position, CurrentTargetPosition);
    }

    float Speed()
    {
        return Probe.linearVelocity.magnitude;
    }

    /// <summary>
    /// Positive while the probe is getting closer, negative once it is moving away.
    /// </summary>
    float ClosingSpeed()
    {
        Vector3 toTarget = CurrentTargetPosition - Probe.position;
        if (toTarget.sqrMagnitude < 0.000001f)
        {
            return 0.0f;
        }

        return Vector3.Dot(Probe.linearVelocity, toTarget.normalized);
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
    }

    void OnGUI()
    {
        if (!DisplayGUI || Probe == null)
        {
            return;
        }

        EnsureStyles();

        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 420.0f, Screen.height - 20.0f));
        GUILayout.BeginVertical(_Panel);

        GUILayout.Label("TP Rendezvous - park on every target", _Header);
        GUILayout.Label("Reaching a target is not enough. You have to <b>stop</b> on it: "
                        + "under <b>" + ArrivalRadius.ToString("0.00") + " m</b> away and "
                        + "under <b>" + ArrivalSpeed.ToString("0.00") + " m/s</b>, held for "
                        + HoldTime.ToString("0.0") + " s.", _Item);

        GUILayout.Space(6.0f);
        GUILayout.Label("Probe mass <b>" + Probe.mass.ToString("0.##") + " kg</b>"
                        + "      force now <b>" + _MeasuredForce.magnitude.ToString("0.00")
                        + " N</b>", _Item);

        GUILayout.Space(6.0f);

        if (_Complete)
        {
            GUILayout.Label("<color=#7CFC7C>COURSE COMPLETE</color> in <b>"
                            + _CourseTime.ToString("0.0") + " s</b> with <b>"
                            + _Overshoots + "</b> overshoot" + (_Overshoots == 1 ? "" : "s"),
                            _Punch);
            GUILayout.Label("Press <b>" + RestartKey + "</b> to run it again.", _Item);
        }
        else
        {
            GUILayout.Label("Target <b>" + (_Index + 1) + " / " + Targets.Length + "</b>"
                            + "      overshoots so far <b>" + _Overshoots + "</b>"
                            + "      t <b>" + _CourseTime.ToString("0.0") + " s</b>", _Item);

            GUILayout.Space(4.0f);
            GUILayout.Label(Tick(Distance() <= ArrivalRadius) + "  distance <b>"
                            + Distance().ToString("0.00") + " m</b>", _Punch);
            GUILayout.Label(Tick(Speed() <= ArrivalSpeed) + "  speed <b>"
                            + Speed().ToString("0.00") + " m/s</b>   (closing "
                            + ClosingSpeed().ToString("+0.00;-0.00") + ")", _Punch);

            if (_HoldTimer > 0.0f)
            {
                GUILayout.Label("holding <b>" + _HoldTimer.ToString("0.00") + " / "
                                + HoldTime.ToString("0.00") + " s</b>", _Item);
            }

            GUILayout.Space(4.0f);
            GUILayout.Label("Press <b>" + RestartKey + "</b> to restart the course.", _Item);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    static string Tick(bool ok)
    {
        return ok ? "<color=#7CFC7C>OK </color>" : "<color=#FF8080>-- </color>";
    }

    void OnDrawGizmos()
    {
        if (Targets == null)
        {
            return;
        }

        foreach (Transform target in Targets)
        {
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, ArrivalRadius);
            }
        }
    }
}
