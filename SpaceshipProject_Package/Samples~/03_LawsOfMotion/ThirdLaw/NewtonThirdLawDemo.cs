using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Newton's third law - action and reaction.
/// If A pushes B, then B pushes A with a force of the same magnitude and the opposite
/// direction. Forces never exist alone, they exist in pairs.
///
/// The catch that trips everyone up: if the two forces are equal and opposite, why does
/// anything move at all? Because THE TWO FORCES ACT ON DIFFERENT BODIES. They never sit
/// on the same object, so they never cancel. What they do cancel is the total momentum:
/// it starts at zero and stays at zero, collision included.
///
/// Four pairs of probes start well apart. For PushDuration seconds each pair is pushed
/// TOGETHER, with the same force magnitude on both sides, then it coasts, and the two
/// probes collide head on. The bounce is left entirely to the physics engine: the demo
/// applies nothing at that moment. What comes back out is measured by an ImpactReporter
/// on each body as J = m * (v_after - v_before), and the two readings are equal and
/// opposite in every lane.
///
/// The blue probe gets heavier lane by lane while the yellow one stays at 1 kg:
///   1. 1 kg vs 1 kg  ->  both approach and leave at the same speed
///   2. 2 kg vs 1 kg  ->  blue moves half as fast, both ways
///   3. 4 kg vs 1 kg  ->  a quarter
///   4. 8 kg vs 1 kg  ->  blue barely moves, which is why a big body looks like it does
///                        not react at all. It does: read the impulse.
///
/// The lanes are set up so that all four pairs meet on the contact line at the same
/// instant, however heavy the blue probe is.
/// </summary>
public class NewtonThirdLawDemo : MonoBehaviour
{
    [System.Serializable]
    public class Side
    {
        public Rigidbody Body;

        [Tooltip("Red bar drawn from this body along whatever is acting on it right now: " +
                 "the push while it lasts, then the impulse of the impact")]
        public Transform Bar;

        [System.NonSerialized] public ImpactReporter Reporter;
        [System.NonSerialized] public Vector3 StartPosition;
        [System.NonSerialized] public Transform Trail;
        [System.NonSerialized] public Material TrailMaterial;
        [System.NonSerialized] public float Radius = 0.5f;

        // Frozen at the end of the run so the comparison pause keeps the final numbers.
        [System.NonSerialized] public float ShownVelocity;
    }

    [System.Serializable]
    public class Interaction
    {
        public string Label;

        [Tooltip("One line shown in the panel: what this pair is here to prove")]
        public string Comment;

        [Tooltip("The left probe. Pushed along PushDirection, towards B")]
        public Side A;

        [Tooltip("The right probe. Pushed the opposite way, towards A")]
        public Side B;
    }

    [SerializeField] Interaction[] Interactions;

    [Header("The push that brings them together")]
    [Tooltip("Applied to A and B at once, same magnitude, opposite directions")]
    [SerializeField] float PushForce = 3.0f;

    [Tooltip("How long the two probes are pushed towards each other")]
    [SerializeField] float PushDuration = 1.0f;

    [Tooltip("A is pushed this way, B is pushed exactly the other way")]
    [SerializeField] Vector3 PushDirection = Vector3.right;

    [Header("Run")]
    [SerializeField] float RunDuration = 5.0f;

    [Tooltip("Everything freezes for this long afterwards, to compare the rebounds")]
    [SerializeField] float HoldDuration = 3.0f;

    [SerializeField] Key RestartKey = Key.R;

    [Header("Breadcrumbs")]
    [SerializeField] bool DropBreadcrumbs = true;
    [SerializeField] float BreadcrumbInterval = 0.5f;
    [SerializeField] float BreadcrumbSize = 0.22f;

    [Header("Bars")]
    [Tooltip("Metres of red bar per newton of push")]
    [SerializeField] float ForceBarScale = 0.3f;

    [Tooltip("Metres of red bar per newton second of impact impulse")]
    [SerializeField] float ImpulseBarScale = 0.15f;

    [Tooltip("How long the impact impulse stays on screen after the collision")]
    [SerializeField] float ImpulseBarDuration = 0.6f;

    [SerializeField] float BarGap = 0.15f;

    [Header("Display")]
    [SerializeField] bool DisplayGUI = true;
    [SerializeField] bool DisplayGizmos = true;
    [SerializeField] Color ColourA = new Color(0.45f, 0.5f, 1.0f);
    [SerializeField] Color ColourB = new Color(0.95f, 0.85f, 0.3f);

    float _PhaseTime;
    bool _Holding;
    int _CrumbsDropped;

    GUIStyle _Panel, _Header, _Item, _Punch, _Row;

    bool Pushing => !_Holding && _PhaseTime < PushDuration;

    void Start()
    {
        if (Interactions == null)
        {
            return;
        }

        foreach (Interaction pair in Interactions)
        {
            SetUpSide(pair.A, pair.Label + " A");
            SetUpSide(pair.B, pair.Label + " B");
        }

        StartRun();
    }

    void SetUpSide(Side side, string trailName)
    {
        if (side == null || side.Body == null)
        {
            return;
        }

        side.StartPosition = side.Body.position;
        side.Reporter = side.Body.GetComponent<ImpactReporter>();

        SphereCollider sphere = side.Body.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            side.Radius = sphere.radius;
        }

        Renderer renderer = side.Body.GetComponentInChildren<Renderer>();
        side.TrailMaterial = renderer != null ? renderer.sharedMaterial : null;

        GameObject trail = new GameObject(trailName + " trail");
        trail.transform.SetParent(transform, false);
        side.Trail = trail.transform;
    }

    [ContextMenu("Start Run")]
    void StartRun()
    {
        _PhaseTime = 0.0f;
        _Holding = false;
        _CrumbsDropped = 0;

        foreach (Interaction pair in Interactions)
        {
            ResetSide(pair.A);
            ResetSide(pair.B);
        }
    }

    void ResetSide(Side side)
    {
        if (side == null || side.Body == null)
        {
            return;
        }

        side.Body.isKinematic = false;
        side.Body.position = side.StartPosition;
        side.Body.rotation = Quaternion.identity;
        side.Body.linearVelocity = Vector3.zero;
        side.Body.angularVelocity = Vector3.zero;
        side.ShownVelocity = 0.0f;

        if (side.Reporter != null)
        {
            side.Reporter.Clear();
        }

        if (side.Trail != null)
        {
            for (int i = side.Trail.childCount - 1; i >= 0; i--)
            {
                Destroy(side.Trail.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// Stops the world so the four rebounds can be compared side by side.
    /// </summary>
    void EnterHold()
    {
        _PhaseTime = 0.0f;
        _Holding = true;

        foreach (Interaction pair in Interactions)
        {
            Freeze(pair.A);
            Freeze(pair.B);
        }
    }

    void Freeze(Side side)
    {
        if (side != null && side.Body != null)
        {
            side.Body.isKinematic = true;
        }
    }

    void FixedUpdate()
    {
        if (Interactions == null || !Pushing)
        {
            return;
        }

        Vector3 force = PushDirection.normalized * PushForce;

        foreach (Interaction pair in Interactions)
        {
            // Equal magnitude, opposite directions, on two DIFFERENT rigidbodies. This is
            // only the run-up: the collision itself is left entirely to the physics
            // engine, which pairs its own forces without being told to.
            if (pair.A != null && pair.A.Body != null)
            {
                pair.A.Body.AddForce(force, ForceMode.Force);
            }

            if (pair.B != null && pair.B.Body != null)
            {
                pair.B.Body.AddForce(-force, ForceMode.Force);
            }
        }
    }

    void Update()
    {
        _PhaseTime += Time.deltaTime;

        if (!_Holding)
        {
            SampleSides();
            UpdateBars();

            if (DropBreadcrumbs)
            {
                // The first crumb belongs on the start position, hence the + 1.
                int wanted = Mathf.FloorToInt(_PhaseTime / BreadcrumbInterval) + 1;
                while (_CrumbsDropped < wanted)
                {
                    DropBreadcrumbRow();
                }
            }

            if (_PhaseTime >= RunDuration)
            {
                HideBars();
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

    void SampleSides()
    {
        Vector3 direction = PushDirection.normalized;

        foreach (Interaction pair in Interactions)
        {
            // Signed along the push axis, so approaching and separating read as + and -.
            if (pair.A != null && pair.A.Body != null)
            {
                pair.A.ShownVelocity = Vector3.Dot(pair.A.Body.linearVelocity, direction);
            }

            if (pair.B != null && pair.B.Body != null)
            {
                pair.B.ShownVelocity = Vector3.Dot(pair.B.Body.linearVelocity, direction);
            }
        }
    }

    /// <summary>
    /// Each bar is drawn from its body along whatever is acting on it: while the push
    /// lasts the two bars point at each other, and just after the impact they point apart.
    /// Same length on both sides in both cases, which is the whole lesson.
    /// </summary>
    void UpdateBars()
    {
        foreach (Interaction pair in Interactions)
        {
            if (Pushing)
            {
                SetBar(pair.A, PushForce, ForceBarScale);
                SetBar(pair.B, -PushForce, ForceBarScale);
                continue;
            }

            SetBar(pair.A, ImpulseToShow(pair.A), ImpulseBarScale);
            SetBar(pair.B, ImpulseToShow(pair.B), ImpulseBarScale);
        }
    }

    /// <summary>
    /// The measured impact impulse, for as long as it is worth flashing on screen.
    /// </summary>
    float ImpulseToShow(Side side)
    {
        if (side == null || side.Reporter == null || !side.Reporter.HasImpact)
        {
            return 0.0f;
        }

        if (Time.time - side.Reporter.ImpactTime > ImpulseBarDuration)
        {
            return 0.0f;
        }

        return Vector3.Dot(side.Reporter.Impulse, PushDirection.normalized);
    }

    void SetBar(Side side, float signedMagnitude, float scale)
    {
        if (side == null || side.Bar == null)
        {
            return;
        }

        float length = Mathf.Abs(signedMagnitude) * scale;
        if (length < 0.01f)
        {
            side.Bar.gameObject.SetActive(false);
            return;
        }

        side.Bar.gameObject.SetActive(true);
        side.Bar.localScale = new Vector3(length, 0.12f, 0.12f);
        side.Bar.localPosition = new Vector3(
            Mathf.Sign(signedMagnitude) * (side.Radius + BarGap + length * 0.5f), 0.0f, 0.0f);
    }

    void HideBars()
    {
        foreach (Interaction pair in Interactions)
        {
            if (pair.A != null && pair.A.Bar != null)
            {
                pair.A.Bar.gameObject.SetActive(false);
            }

            if (pair.B != null && pair.B.Bar != null)
            {
                pair.B.Bar.gameObject.SetActive(false);
            }
        }
    }

    void DropBreadcrumbRow()
    {
        _CrumbsDropped++;

        foreach (Interaction pair in Interactions)
        {
            DropCrumb(pair.A);
            DropCrumb(pair.B);
        }
    }

    void DropCrumb(Side side)
    {
        if (side == null || side.Body == null || side.Trail == null)
        {
            return;
        }

        GameObject crumb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crumb.name = "Crumb";
        Destroy(crumb.GetComponent<Collider>());

        crumb.transform.SetParent(side.Trail, false);
        crumb.transform.position = side.Body.position;
        crumb.transform.localScale = Vector3.one * BreadcrumbSize;

        if (side.TrailMaterial != null)
        {
            crumb.GetComponent<Renderer>().sharedMaterial = side.TrailMaterial;
        }
    }

    /// <summary>
    /// Momentum of one side along the push axis, in kg m/s. A and B always come out equal
    /// and opposite, whatever the masses are, before and after the collision alike.
    /// </summary>
    float Momentum(Side side)
    {
        if (side == null || side.Body == null)
        {
            return 0.0f;
        }

        return side.Body.mass * side.ShownVelocity;
    }

    float MeasuredImpulse(Side side)
    {
        if (side == null || side.Reporter == null || !side.Reporter.HasImpact)
        {
            return 0.0f;
        }

        return Vector3.Dot(side.Reporter.Impulse, PushDirection.normalized);
    }

    bool HasImpact(Interaction pair)
    {
        return pair.A != null && pair.A.Reporter != null && pair.A.Reporter.HasImpact;
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

        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 430.0f, Screen.height - 20.0f));
        GUILayout.BeginVertical(_Panel);

        GUILayout.Label("Newton's Third Law - Action and Reaction", _Header);
        GUILayout.Label("The probes are pushed together, then left alone, and they "
                        + "collide. The demo applies <b>nothing</b> during the impact: "
                        + "the bounce is the physics engine pairing its own forces.", _Item);

        GUILayout.Space(4.0f);
        GUILayout.Label("Each body measures the impulse it received, "
                        + "<b>J = m (v_after - v_before)</b>. The two always come out "
                        + "equal and opposite, so the total momentum never leaves zero.",
                        _Item);

        GUILayout.Space(6.0f);

        foreach (Interaction pair in Interactions)
        {
            DrawPairRow(pair);
        }

        GUILayout.Space(6.0f);
        GUILayout.Label(PhaseName() + " - t = <b>" + ElapsedInRun().ToString("0.0")
                        + " s</b> / " + RunDuration.ToString("0.0") + " s      press <b>"
                        + RestartKey + "</b> to restart", _Item);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    string PhaseName()
    {
        if (_Holding)
        {
            return "COMPARING";
        }

        if (Pushing)
        {
            return "PUSHING TOGETHER";
        }

        return Interactions.Length > 0 && HasImpact(Interactions[0])
            ? "SEPARATING" : "CLOSING IN";
    }

    float ElapsedInRun()
    {
        return _Holding ? RunDuration : Mathf.Min(_PhaseTime, RunDuration);
    }

    void DrawPairRow(Interaction pair)
    {
        if (pair.A == null || pair.A.Body == null || pair.B == null || pair.B.Body == null)
        {
            return;
        }

        string a = ColorUtility.ToHtmlStringRGB(ColourA);
        string b = ColorUtility.ToHtmlStringRGB(ColourB);

        GUILayout.BeginVertical(_Row);

        GUILayout.Label("<b>" + pair.Label + "</b>", _Item);
        GUILayout.Label(pair.Comment, _Item);

        GUILayout.Label("<color=#" + a + ">A  m " + pair.A.Body.mass.ToString("0.#")
                        + " kg   v " + pair.A.ShownVelocity.ToString("+0.00;-0.00")
                        + "   p " + Momentum(pair.A).ToString("+0.00;-0.00")
                        + "</color>", _Item);
        GUILayout.Label("<color=#" + b + ">B  m " + pair.B.Body.mass.ToString("0.#")
                        + " kg   v " + pair.B.ShownVelocity.ToString("+0.00;-0.00")
                        + "   p " + Momentum(pair.B).ToString("+0.00;-0.00")
                        + "</color>", _Item);

        float total = Momentum(pair.A) + Momentum(pair.B);

        if (HasImpact(pair))
        {
            GUILayout.Label("impact <b>J on A " + MeasuredImpulse(pair.A).ToString("+0.00;-0.00")
                            + "</b>   <b>J on B " + MeasuredImpulse(pair.B).ToString("+0.00;-0.00")
                            + "</b> N s    total p " + total.ToString("0.00"), _Punch);
        }
        else
        {
            GUILayout.Label("impact <b>J</b> - not yet -      total p "
                            + total.ToString("0.00"), _Punch);
        }

        GUILayout.EndVertical();
    }

    void OnDrawGizmos()
    {
        if (!DisplayGizmos || Interactions == null)
        {
            return;
        }

        foreach (Interaction pair in Interactions)
        {
            if (pair.A == null || pair.A.Body == null || pair.B == null || pair.B.Body == null)
            {
                continue;
            }

            Gizmos.color = Color.cyan;
            DrawUtils.DrawArrow(pair.A.Body.position, pair.A.Body.linearVelocity);
            DrawUtils.DrawArrow(pair.B.Body.position, pair.B.Body.linearVelocity);

            // The measured action and reaction, drawn on the two bodies they acted on.
            if (Application.isPlaying && HasImpact(pair))
            {
                Gizmos.color = Color.red;
                DrawUtils.DrawArrow(pair.A.Body.position,
                                    pair.A.Reporter.Impulse * ImpulseBarScale);
                DrawUtils.DrawArrow(pair.B.Body.position,
                                    pair.B.Reporter.Impulse * ImpulseBarScale);
            }
        }
    }
}
