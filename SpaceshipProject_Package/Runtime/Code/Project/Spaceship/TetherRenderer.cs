using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual feedback for the tethers of the ship. Like RocketEngineFX for a thruster, this
/// component NEVER applies a force and never creates a joint : it only READS the SpringJoints
/// the controller built, and shows what they are doing. A tether that is not where you meant,
/// too long, or on the wrong side of the goal is visible at a glance.
///
///     the rope     blue and drooping when slack, yellow and straight when just taut,
///                  then orange and thicker as the wind loads it, red when overloaded.
///     the sag      a slack rope droops in a curve : the longer it is for the distance, the
///                  deeper. A rope that is too long shows it.
///     the anchors  a marker at each end. The one on the wall pulses faster as the load grows.
///
/// What it reads, on each SpringJoint of the ship (or of its children) :
///     anchor, connectedAnchor, connectedBody   the two ends
///     maxDistance, spring                      how stretched, and how hard it pulls back
///     currentForce                             what the engine reports, as a cross-check
///
/// Added to the ship by the SolarWindHazard. Nothing to wire up.
/// </summary>
public class TetherRenderer : MonoBehaviour
{
    [Header("Colours : slack, just taut, loaded, overloaded")]
    [SerializeField] private Color _SlackColour = new Color(0.35f, 0.65f, 1.0f);
    [SerializeField] private Color _TautColour = new Color(1.0f, 0.9f, 0.25f);
    [SerializeField] private Color _LoadedColour = new Color(1.0f, 0.5f, 0.1f);
    [SerializeField] private Color _OverloadedColour = new Color(1.0f, 0.1f, 0.1f);

    [Header("Rope")]
    [SerializeField] private float _SlackWidth = 0.04f;
    [SerializeField] private float _LoadedWidth = 0.12f;
    [Tooltip("Points along the rope. More is a smoother droop.")]
    [SerializeField] private int _Segments = 24;
    [Tooltip("The deepest a slack rope droops, in metres.")]
    [SerializeField] private float _MaxSag = 2.5f;

    [Tooltip("Newtons that count as 'fully loaded'. 0 uses the solar wind's gust force, or 30 N.")]
    [SerializeField] private float _ReferenceForce = 0.0f;

    [Header("Anchors")]
    [SerializeField] private float _WallMarkerSize = 0.3f;
    [SerializeField] private float _ShipMarkerSize = 0.15f;

    [Tooltip("Pulses per second on the wall marker, when the rope is just taut. It speeds up with the load.")]
    [SerializeField] private float _PulseSpeed = 1.0f;

    [Header("Response")]
    [Tooltip("How fast the visuals catch up with the joint. Higher = snappier.")]
    [SerializeField] private float _Responsiveness = 12.0f;

    // Where the colour steps sit on the 0..1.4 tension scale.
    private const float TautTension = 0.3f;
    private const float LoadedTension = 1.0f;
    private const float OverloadedTension = 1.4f;

    // A rope this much shorter than it is long is slack. Over this range it is "almost taut".
    private const float SlackRange = 0.15f;

    private class Tether
    {
        public SpringJoint Joint;
        public LineRenderer Line;
        public Transform WallMarker;
        public Transform ShipMarker;
        public Material Material;
        public float Tension;
        public float Sag;
        public float Pulse;
        public bool Shown;
        public Vector3[] Points;
    }

    private readonly Dictionary<SpringJoint, Tether> _Tethers = new Dictionary<SpringJoint, Tether>();
    private readonly List<SpringJoint> _Joints = new List<SpringJoint>();
    private readonly List<SpringJoint> _Gone = new List<SpringJoint>();

    /// <summary>The highest tension among the tethers, on the scale the colours use : 0 slack, 0.3 just taut, 1 fully loaded.</summary>
    public float MaxTension { get; private set; }

    /// <summary>The deepest droop among the tethers, in metres. 0 when every rope is taut.</summary>
    public float MaxSag { get; private set; }

    private void LateUpdate()
    {
        GetComponentsInChildren(_Joints);

        float reference = ReferenceForce();
        float catchUp = 1.0f - Mathf.Exp(-_Responsiveness * Time.deltaTime);

        MaxTension = 0.0f;
        MaxSag = 0.0f;

        foreach (SpringJoint joint in _Joints)
        {
            if (!_Tethers.TryGetValue(joint, out Tether tether))
            {
                tether = CreateTether(joint);
                _Tethers.Add(joint, tether);
            }

            Show(tether, joint, reference, catchUp);
            MaxTension = Mathf.Max(MaxTension, tether.Tension);
            MaxSag = Mathf.Max(MaxSag, tether.Sag);
        }

        RemoveGone();
    }

    private float ReferenceForce()
    {
        if (_ReferenceForce > 0.0f)
        {
            return _ReferenceForce;
        }

        return SolarWindHazard.Instance ? SolarWindHazard.Instance.GustForce : 30.0f;
    }

    private void Show(Tether tether, SpringJoint joint, float reference, float catchUp)
    {
        // anchor : local to the body that owns the joint. connectedAnchor : local to the
        // connected body - or already a world position when there is none.
        Vector3 anchor_WorldSpace = joint.transform.TransformPoint(joint.anchor);
        Vector3 connectedAnchor_WorldSpace = joint.connectedBody != null
            ? joint.connectedBody.transform.TransformPoint(joint.connectedAnchor)
            : joint.connectedAnchor;

        Vector3 chord = connectedAnchor_WorldSpace - anchor_WorldSpace;
        float length = chord.magnitude;

        // Negative : slack, by that much. Positive : stretched past maxDistance.
        float stretch = length - joint.maxDistance;

        float targetTension = TensionOf(stretch, joint.spring, joint.currentForce.magnitude, reference);
        float targetSag = stretch < 0.0f ? SagOf(length, -stretch) : 0.0f;

        // A rope that has just appeared starts as what it is, not as a slack one that tightens.
        if (!tether.Shown)
        {
            tether.Shown = true;
            tether.Tension = targetTension;
            tether.Sag = targetSag;
        }

        // Frame-rate independent smoothing, so the rope does not pop between two physics steps.
        tether.Tension = Mathf.Lerp(tether.Tension, targetTension, catchUp);
        tether.Sag = Mathf.Lerp(tether.Sag, targetSag, catchUp);
        tether.Pulse += Time.deltaTime * _PulseSpeed * (1.0f + 5.0f * Mathf.Clamp01(tether.Tension));

        Color colour = ColourOf(tether.Tension);
        tether.Material.color = colour;

        DrawRope(tether, anchor_WorldSpace, connectedAnchor_WorldSpace, chord);

        float width = Mathf.Lerp(_SlackWidth, _LoadedWidth, Mathf.Clamp01(tether.Tension));
        tether.Line.startWidth = width;
        tether.Line.endWidth = width;

        // The wall end beats like a pulse, the faster the harder the pull. The ship end just marks the spot.
        float beat = 1.0f + 0.35f * Mathf.Clamp01(tether.Tension) * Mathf.Sin(tether.Pulse * 2.0f * Mathf.PI);
        tether.WallMarker.position = connectedAnchor_WorldSpace;
        tether.WallMarker.localScale = Vector3.one * (_WallMarkerSize * beat);
        tether.ShipMarker.position = anchor_WorldSpace;
        tether.ShipMarker.localScale = Vector3.one * _ShipMarkerSize;
    }

    /// <summary>
    /// 0 : slack. 0.3 : just taut. 1 : the rope is holding the reference force. Above : overloaded.
    ///
    /// Read from the geometry first - how far past maxDistance the ship is, times the spring - since
    /// that is what the joint pulls with, and from the engine's own currentForce as a cross-check.
    /// </summary>
    private static float TensionOf(float stretch, float spring, float currentForce, float reference)
    {
        if (stretch < 0.0f)
        {
            // Slack, but the nearer to straight, the nearer to taut.
            return TautTension * Mathf.Clamp01(1.0f + stretch / SlackRange);
        }

        float springForce = spring * stretch;
        float load = Mathf.Max(springForce, currentForce) / Mathf.Max(reference, 0.001f);
        return TautTension + (LoadedTension - TautTension) * load;
    }

    /// <summary>
    /// How deep a slack rope droops. A rope of length (chord + slack) hanging between two points
    /// a chord apart is, for a shallow droop, about a parabola : slack = 8 * depth^2 / (3 * chord).
    /// </summary>
    private float SagOf(float chord, float slack)
    {
        return Mathf.Min(_MaxSag, Mathf.Sqrt(0.375f * chord * slack));
    }

    private Color ColourOf(float tension)
    {
        if (tension <= TautTension)
        {
            return Color.Lerp(_SlackColour, _TautColour, tension / TautTension);
        }

        if (tension <= LoadedTension)
        {
            return Color.Lerp(_TautColour, _LoadedColour, (tension - TautTension) / (LoadedTension - TautTension));
        }

        return Color.Lerp(_LoadedColour, _OverloadedColour, Mathf.Clamp01((tension - LoadedTension) / (OverloadedTension - LoadedTension)));
    }

    private void DrawRope(Tether tether, Vector3 from, Vector3 to, Vector3 chord)
    {
        int count = Mathf.Max(2, _Segments);
        if (tether.Points == null || tether.Points.Length != count)
        {
            tether.Points = new Vector3[count];
            tether.Line.positionCount = count;
        }

        // The droop is sideways to the rope, on the screen plane, and always on the side
        // that looks like down - whatever way round the two ends are.
        Vector3 droop = Vector3.Cross(chord.normalized, Vector3.forward);
        if (droop.y > 0.001f || (Mathf.Abs(droop.y) <= 0.001f && droop.x > 0.0f))
        {
            droop = -droop;
        }

        for (int i = 0; i < count; i++)
        {
            float u = i / (float)(count - 1);
            tether.Points[i] = Vector3.Lerp(from, to, u) + droop * (4.0f * tether.Sag * u * (1.0f - u));
        }

        tether.Line.SetPositions(tether.Points);
    }

    private Tether CreateTether(SpringJoint joint)
    {
        Tether tether = new Tether { Joint = joint };

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        tether.Material = new Material(shader);

        GameObject lineObject = new GameObject("Tether");
        lineObject.transform.SetParent(transform, false);
        tether.Line = lineObject.AddComponent<LineRenderer>();
        tether.Line.useWorldSpace = true;
        tether.Line.sharedMaterial = tether.Material;
        tether.Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tether.Line.receiveShadows = false;

        tether.WallMarker = CreateMarker("Tether_WallMarker", tether.Material);
        tether.ShipMarker = CreateMarker("Tether_ShipMarker", tether.Material);

        tether.Tension = 0.0f;
        return tether;
    }

    private Transform CreateMarker(string name, Material material)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;

        // A marker is only a picture : it must never touch the physics.
        Collider collider = marker.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);

        marker.transform.SetParent(transform, true);

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.sharedMaterial = material;
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;

        return marker.transform;
    }

    private void RemoveGone()
    {
        _Gone.Clear();
        foreach (KeyValuePair<SpringJoint, Tether> pair in _Tethers)
        {
            if (pair.Key == null)
            {
                _Gone.Add(pair.Key);
            }
        }

        foreach (SpringJoint joint in _Gone)
        {
            DestroyTether(_Tethers[joint]);
            _Tethers.Remove(joint);
        }
    }

    private void DestroyTether(Tether tether)
    {
        Destroy(tether.Line.gameObject);
        Destroy(tether.WallMarker.gameObject);
        Destroy(tether.ShipMarker.gameObject);
        Destroy(tether.Material);
    }

    private void OnDestroy()
    {
        foreach (Tether tether in _Tethers.Values)
        {
            if (tether.Material != null)
            {
                Destroy(tether.Material);
            }
        }
    }
}
