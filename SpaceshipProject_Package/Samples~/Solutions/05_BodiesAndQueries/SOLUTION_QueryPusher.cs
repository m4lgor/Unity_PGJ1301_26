using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SOLUTION - the pusher that finds its own targets.
///
///   Self            whoever I am attached to   (the old ForceApplier)
///   OverlapSphere   everything inside a ball   (a repulsor)
///   Ray             first thing along a line   (a beam, bouncing)
///   SphereCast      first thing along a tube   (a thick beam)
///
/// The interesting part is not the queries, it is the filters after them : a query
/// answers with colliders, and a push needs a body.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SOLUTION_QueryPusher : MonoBehaviour
{
    public enum PushMode
    {
        Self,
        OverlapSphere,
        Ray,
        SphereCast
    }

    public PushMode Mode = PushMode.OverlapSphere;

    public float Force = 30.0f;

    public ForceMode ApplyAs = ForceMode.Force;

    [Tooltip("OverlapSphere: the radius searched around you")]
    public float Radius = 6.0f;

    [Tooltip("SphereCast : the beam thickness. Separate from Radius on purpose - a beam " +
             "as wide as the detection sphere stops on whatever stands beside you")]
    public float CastRadius = 1.5f;

    [Tooltip("Total length of the beam, bounces included")]
    public float Range = 50.0f;

    public LayerMask TargetMask = ~0;

    [Tooltip("Ray : what the beam bounces off instead of stopping on")]
    public bool BounceOnStatic = false;
    public bool BounceOnDynamic = false;
    public bool BounceOnKinematic = false;

    [Tooltip("Two facing walls would bounce for ever. Each bounce is also one more query")]
    public int MaxBounces = 8;

    const int MaxHits = 16;

    // Restarting a ray exactly ON the surface it just hit lands as often just inside it
    // as just outside : the next cast reports the same collider at distance 0 and the
    // beam stalls in place. Leaving 1 mm of room is enough.
    const float SurfaceOffset = 0.001f;

    // Allocated once. The plain OverlapSphere returns a fresh array every call.
    readonly Collider[] _Hits = new Collider[MaxHits];

    // One body can own several colliders, so without this it gets pushed twice.
    readonly List<Rigidbody> _PushedThisStep = new List<Rigidbody>();

    // The path the beam actually took this step, corner by corner, so the gizmos can
    // draw the beam that was fired instead of a straight line that no longer exists.
    readonly List<Vector3> _BeamPath = new List<Vector3>();

    Rigidbody _Rigidbody;

    void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
    }

    // Query and push both belong here, not in Update.
    void FixedUpdate()
    {
        _PushedThisStep.Clear();
        _BeamPath.Clear();

        switch (Mode)
        {
            case PushMode.Self:
                PushSelf();
                break;

            case PushMode.OverlapSphere:
                PushOverlap();
                break;

            case PushMode.Ray:
                PushRay(transform.position, transform.up, Range, 0);
                break;

            case PushMode.SphereCast:
                PushSphereCast(transform.position, transform.up);
                break;
        }
    }

    void PushSelf()
    {
        _Rigidbody.AddForce(transform.up * Force, ApplyAs);
    }

    void PushOverlap()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, _Hits,
                                                  TargetMask, QueryTriggerInteraction.Ignore);

        if (count >= MaxHits)
        {
            // The buffer filled up and the rest were dropped silently.
            // A bigger buffer or a tighter mask, not a shrug.
            Debug.LogWarning("QueryPusher - the overlap buffer is full, some targets are " +
                             "invisible to me", this);
        }

        for (int i = 0; i < count; i++)
        {
            Rigidbody body = Resolve(_Hits[i]);

            if (body == null)
            {
                continue;
            }

            // Away from me : this is a repulsor.
            Vector3 away = body.position - transform.position;

            if (away.sqrMagnitude <= Mathf.Epsilon)
            {
                continue;
            }

            body.AddForce(away.normalized * Force, ApplyAs);
        }
    }

    /// <summary>
    /// The beam. It bounces off whatever it cannot push and carries on with what is LEFT
    /// of its range, so the whole path is Range metres long however many corners it takes.
    /// </summary>
    void PushRay(Vector3 from, Vector3 direction, float range, int bounce)
    {
        if (bounce == 0)
        {
            _BeamPath.Add(from);
        }

        if (range <= 0.0f || bounce > MaxBounces)
        {
            return;
        }

        RaycastHit hit;

        // One hit, the nearest, or nothing. RaycastAll gives the whole line, and allocates.
        bool touched = Physics.Raycast(from, direction, out hit, range,
                                       TargetMask, QueryTriggerInteraction.Ignore);

        if (!touched)
        {
            _BeamPath.Add(from + direction * range);
            return;
        }

        _BeamPath.Add(hit.point);

        if (!ShouldBounce(hit.rigidbody))
        {
            Push(hit, direction);
            return;
        }

        Vector3 bounced = Vector3.Reflect(direction, hit.normal);

        PushRay(hit.point + bounced * SurfaceOffset, bounced, range - hit.distance, bounce + 1);
    }

    /// hit.rigidbody is null when the collider has no body at all : that is the scenery.
    bool ShouldBounce(Rigidbody body)
    {
        if (body == null)
        {
            return BounceOnStatic;
        }

        return body.isKinematic ? BounceOnKinematic : BounceOnDynamic;
    }

    void PushSphereCast(Vector3 from, Vector3 direction)
    {
        RaycastHit hit;

        // Started clear of my own collider : a sweep that begins already overlapping
        // reports distance 0 and a zero normal.
        float offset = CastRadius + 0.1f;

        Vector3 origin = from + direction * offset;

        bool touched = Physics.SphereCast(origin, CastRadius, direction, out hit,
                                          Range - offset, TargetMask, QueryTriggerInteraction.Ignore);
        if (touched)
        {
            Push(hit, direction);
        }
    }

    /// <summary>
    /// Pushed along the beam AS IT ARRIVES, not along transform.up : after a bounce the
    /// two are not the same direction any more.
    /// </summary>
    void Push(RaycastHit hit, Vector3 direction)
    {
        Rigidbody body = Resolve(hit.collider);

        if (body == null)
        {
            return;
        }
        
        body.AddForceAtPosition(direction * Force, hit.point, ApplyAs);
    }

    /// Collider -> Rigidbody, with the three filters. Skip one and the pusher drifts,
    /// double-pushes, or shoves the scenery.
    Rigidbody Resolve(Collider collider)
    {
        if (collider == null)
        {
            return null;
        }

        // attachedRigidbody, not GetComponent : the body may be several levels up, and
        // null means static scenery - a force on it does nothing.
        Rigidbody body = collider.attachedRigidbody;

        if (body == null || body == _Rigidbody)
        {
            return null;
        }

        if (_PushedThisStep.Contains(body))
        {
            return null;
        }

        _PushedThisStep.Add(body);

        return body;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        switch (Mode)
        {
            case PushMode.OverlapSphere:
                Gizmos.DrawWireSphere(transform.position, Radius);
                break;

            case PushMode.Ray:
                DrawBeam();
                break;

            case PushMode.SphereCast:
                Vector3 origin = transform.position + transform.up * (CastRadius + 0.1f);

                DrawUtils.DrawArrow(origin, transform.up * (Range - CastRadius - 0.1f));
                Gizmos.DrawWireSphere(origin, CastRadius);
                Gizmos.DrawWireSphere(transform.position + transform.up * Range, CastRadius);
                break;
        }
    }

    void DrawBeam()
    {
        // Nothing fired yet (edit mode) : show where the beam would start.
        if (_BeamPath.Count < 2)
        {
            DrawUtils.DrawArrow(transform.position, transform.up * Range);
            return;
        }

        for (int i = 0; i + 2 < _BeamPath.Count; i++)
        {
            Gizmos.DrawLine(_BeamPath[i], _BeamPath[i + 1]);
            Gizmos.DrawWireSphere(_BeamPath[i + 1], 0.1f);
        }

        int last = _BeamPath.Count - 1;

        DrawUtils.DrawArrow(_BeamPath[last - 1], _BeamPath[last] - _BeamPath[last - 1]);
    }
}
