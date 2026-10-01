using UnityEngine;

/// <summary>
/// One star of the wall.
///
/// It has a Rigidbody and NO COLLIDER, on purpose. Unity moves it and nothing else: it
/// will not notice that it touches the ship, and it will not push anything apart. That
/// gap is the level. It is the same gap you filled by hand in TP_Manifold, except the
/// other sphere is now your ship, and this one is in your way.
///
/// The contact happens either way. The only question is whether YOU choose the impulse,
/// or the universe chooses it for you -- see HullHit below.
///
/// -----------------------
/// This file is not for you to edit.
/// -----------------------
/// </summary>
[RequireComponent(typeof(Rigidbody))]
// After the default order, so a controller that detects and deflects in ITS FixedUpdate
// is always heard before this one decides the star reached the hull. Without this the
// order between MonoBehaviours is undefined, and a correct deflection would arrive "late"
// on some runs and not others -- unexplainable to a student, and not their bug.
[DefaultExecutionOrder(100)]
public class StarComponent : MonoBehaviour
{
    [Tooltip("The largest deflection impulse this star accepts, in N.s. Ask for more and " +
             "it is clamped to this. You cannot stop a star dead by pushing harder: you " +
             "have to catch it off centre, where the normal does the sideways work for you.")]
    [SerializeField] float _MaxDeflectImpulse = 2.0f;

    [Tooltip("Slack allowed when checking that a contact is real, in metres. It exists " +
             "because at first touch the overlap is a single point.")]
    [SerializeField] float _ContactTolerance = 0.1f;

    [Tooltip("Bounciness used when a star is NOT deflected and reaches the hull. Springy " +
             "on purpose: it bounces the star clear instead of letting it grind along " +
             "the hull, and it makes barging through a wall cost more attitude.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] float _HullRestitution = 0.6f;

    Rigidbody _Rigidbody;
    StarField _Field;
    Rigidbody _ShipBody;
    float _ShipRadius;

    // Resolved against the hull already. A star gets to hit you once.
    bool _Spent;

    // One accepted deflection per physics step. Without this, calling Deflect in a loop
    // multiplies the impulse by however many times you call it, and the cap means nothing.
    float _LastDeflectTime = -1.0f;

    Vector3 _PreviousPosition;
    Vector3 _PreviousShipPosition;
    bool _HasPrevious;

    bool _WarnedContactPoint;

    /// <summary>
    /// World radius. A Unity sphere is 1 unit across, so the radius is half the scale --
    /// the same rule as the spheres in TP_Manifold.
    /// </summary>
    public float Radius
    {
        get { return transform.localScale.x * 0.5f; }
    }

    public Rigidbody Body
    {
        get { return _Rigidbody; }
    }

    public Vector3 Velocity
    {
        get { return _Rigidbody != null ? _Rigidbody.linearVelocity : Vector3.zero; }
    }

    /// <summary>Read it rather than hardcoding 2.0 -- a level may cap deflections lower.</summary>
    public float MaxDeflectImpulse
    {
        get { return _MaxDeflectImpulse; }
    }

    /// <summary>True once this star has reached the hull. It can no longer be deflected.</summary>
    public bool IsSpent
    {
        get { return _Spent; }
    }

    void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _Rigidbody.useGravity = false;
    }

    public void Init(StarField field, Rigidbody shipBody, float shipRadius, float mass, Vector3 velocity)
    {
        _Field = field;
        _ShipBody = shipBody;
        _ShipRadius = shipRadius;

        _Rigidbody.mass = mass;
        _Rigidbody.linearVelocity = velocity;

        // With no collider Unity has no shape to compute an inertia tensor from and falls
        // back to (1,1,1), which makes a big star spin like a small one. A solid sphere is
        // (2/5) m r^2, so an off-centre hit reads the way it should.
        float inertia = 0.4f * mass * Radius * Radius;
        _Rigidbody.automaticInertiaTensor = false;
        _Rigidbody.inertiaTensor = Vector3.one * Mathf.Max(inertia, 0.001f);

        _PreviousPosition = transform.position;
        _PreviousShipPosition = shipBody != null ? shipBody.position : Vector3.zero;
        _HasPrevious = true;
    }

    /// <summary>
    /// Push this star away from the ship, and take the equal and opposite push yourself.
    ///
    /// This is the only way to touch a star, and it is deliberately narrow:
    ///
    ///   - it is refused unless the ship REALLY overlaps this star. No detection, no
    ///     deflection. That is what makes the sphere-sphere test worth writing.
    ///   - the impulse is projected onto the contact normal. A contact pushes; it cannot
    ///     pull, and it cannot steer sideways. A correct solution is already along the
    ///     normal, so this only ever bites a shortcut.
    ///   - the magnitude is clamped to MaxDeflectImpulse.
    ///   - it applies -impulse to the ship, at the same point. Third law, and not optional.
    ///
    /// Returns true if the deflection was accepted. When it is refused it says why in the
    /// HUD console, because a silent no is impossible to debug.
    /// </summary>
    public bool Deflect(Vector3 impulse, Vector3 worldContactPoint)
    {
        if (_ShipBody == null)
        {
            return false;
        }

        if (_Spent)
        {
            return Refuse("cette etoile a deja touche la coque");
        }

        if (_LastDeflectTime == Time.fixedTime)
        {
            return Refuse("une seule deviation par pas de physique");
        }

        // The closest the two got during the step that just ran, NOT where they are now.
        // A small fast star can be through and past the ship by the time your FixedUpdate
        // runs, and refusing that would punish exactly the swept test this level asks for.
        // The contact really happened; it just happened between two frames.
        Vector3 closest = ClosestApproach();
        float distance = closest.magnitude;
        float touching = Radius + _ShipRadius + _ContactTolerance;

        if (distance > touching)
        {
            return Refuse("contact requis : au plus pres " + distance.ToString("F2")
                          + " m, il en faut moins de " + touching.ToString("F2"));
        }

        if (distance < 0.0001f)
        {
            return Refuse("centres confondus, pas de normale");
        }

        Vector3 normal = closest / distance;
        float along = Vector3.Dot(impulse, normal);

        if (along <= 0.0f)
        {
            return Refuse("impulsion dirigee vers le vaisseau, un contact pousse");
        }

        Vector3 applied = normal * Mathf.Min(along, _MaxDeflectImpulse);
        Vector3 contact = ValidateContactPoint(worldContactPoint,
                                               _ShipBody.position + closest, normal);

        _Rigidbody.AddForceAtPosition(applied, contact, ForceMode.Impulse);
        _ShipBody.AddForceAtPosition(-applied, contact, ForceMode.Impulse);

        _LastDeflectTime = Time.fixedTime;

        if (_Field != null)
        {
            _Field.RegisterDeflection();
        }

        return true;
    }

    /// <summary>
    /// The contact point has to lie in the overlap of the two spheres -- inside this star
    /// AND inside the ship. A point that is in neither is a wrong manifold, and since it
    /// is the lever arm of the push it would spin both bodies the wrong way. Said once,
    /// then clamped onto the surface so the level keeps running.
    /// </summary>
    Vector3 ValidateContactPoint(Vector3 point, Vector3 starAtContact, Vector3 normal)
    {
        bool insideStar = (point - starAtContact).magnitude <= Radius + _ContactTolerance;
        bool insideShip = (point - _ShipBody.position).magnitude <= _ShipRadius + _ContactTolerance;

        if (insideStar && insideShip)
        {
            return point;
        }

        if (!_WarnedContactPoint)
        {
            _WarnedContactPoint = true;
            HudConsole.Log("Point de contact hors du recouvrement, relis ton Manifold");
        }

        return starAtContact - normal * Radius;
    }

    bool Refuse(string reason)
    {
        HudConsole.Log("Deviation refusee : " + reason);
        return false;
    }

    void FixedUpdate()
    {
        if (_ShipBody == null)
        {
            return;
        }

        if (!_Spent && _HasPrevious && SweptHullHit())
        {
            HullHit();
        }

        _PreviousPosition = transform.position;
        _PreviousShipPosition = _ShipBody.position;
        _HasPrevious = true;
    }

    /// <summary>
    /// Did this star's core reach the hull during the step that just ran?
    ///
    /// Swept, not a plain distance test: a fast star jumps metres between two physics
    /// steps, and a test that only looks at where things are NOW walks straight past it.
    /// That is the same hole your own detection has, and it is the reason a Rigidbody has
    /// a Collision Detection mode. Measured in the ship's frame, so the ship's own
    /// movement counts too.
    /// </summary>
    bool SweptHullHit()
    {
        return ClosestApproach().magnitude < _ShipRadius;
    }

    /// <summary>
    /// Where this star was, relative to the ship, at their closest point during the step
    /// that just ran. Working in the ship's frame is what lets one segment answer for two
    /// moving bodies: the ship sits at the origin and only the difference matters.
    /// </summary>
    Vector3 ClosestApproach()
    {
        Vector3 to = transform.position - _ShipBody.position;

        if (!_HasPrevious)
        {
            return to;
        }

        Vector3 from = _PreviousPosition - _PreviousShipPosition;

        Vector3 segment = to - from;
        float lengthSquared = segment.sqrMagnitude;

        float t = 0.0f;
        if (lengthSquared > 0.0001f)
        {
            t = Mathf.Clamp01(Vector3.Dot(-from, segment) / lengthSquared);
        }

        return from + segment * t;
    }

    /// <summary>
    /// Nobody deflected it, so the collision resolves itself -- the same impulse you were
    /// asked to compute, with the real masses and nothing capped. This is what a hit costs.
    /// </summary>
    void HullHit()
    {
        _Spent = true;

        // Taken at the closest approach and not at the current position: by now the star
        // may already be on the far side, which would flip the bounce.
        Vector3 closest = ClosestApproach();
        float distance = closest.magnitude;

        Vector3 normal = distance > 0.0001f
            ? closest / distance
            : (Velocity.sqrMagnitude > 0.0001f ? -Velocity.normalized : Vector3.up);

        float invShip = 1.0f / _ShipBody.mass;
        float invStar = 1.0f / _Rigidbody.mass;

        Vector3 relative = _Rigidbody.linearVelocity - _ShipBody.linearVelocity;
        float along = Vector3.Dot(relative, normal);

        if (along < 0.0f)
        {
            float j = -(1.0f + _HullRestitution) * along / (invShip + invStar);

            // At the contact point, not through the centres. A small star can never push
            // a ship far -- momentum will not allow it -- but an off-centre hit SPINS it,
            // and that is the real cost of ignoring one. Rotation has been unlocked since
            // Level 02 and nothing damps it, so a ship that barges through a wall comes
            // out the far side tumbling, unable to aim an engine or hold a zone.
            Vector3 contact = _ShipBody.position + closest - normal * Radius;

            _Rigidbody.AddForceAtPosition(j * normal, contact, ForceMode.Impulse);
            _ShipBody.AddForceAtPosition(-j * normal, contact, ForceMode.Impulse);
        }

        if (_Field != null)
        {
            _Field.RegisterHullHit();
        }

        HudConsole.Log("IMPACT COQUE : etoile non deviee a "
                       + relative.magnitude.ToString("F1") + " m/s");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _Spent ? Color.gray : new Color(1.0f, 0.85f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, Radius);

        if (Application.isPlaying && Velocity.sqrMagnitude > 0.0001f)
        {
            DrawUtils.DrawArrow(transform.position, Velocity * 0.3f);
        }
    }
}
