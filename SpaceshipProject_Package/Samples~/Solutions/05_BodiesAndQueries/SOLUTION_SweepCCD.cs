using UnityEngine;

/// <summary>
/// SOLUTION - lane 1c.
///
/// One query per step, fired BEFORE the step : "if I move where I am about to move, do
/// I cross anything ?". If yes, the ball is parked on the contact and its velocity is
/// spent there, so the step that follows moves it nowhere.
///
/// Sweeping the movement that ALREADY happened works too, but then the ball is inside
/// the wall for a frame before being pulled back, and you see it. Continuous does not
/// do that - it stops the body before the step, and so does this.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SOLUTION_SweepCCD : MonoBehaviour
{
    public float Radius = 0.25f;

    [Range(0.0f, 1.0f)]
    public float Restitution = 0.0f;

    [Tooltip("Gap left between the ball and the surface, in metres. Zero parks it exactly " +
             "touching, which the engine may then read as an overlap")]
    public float Skin = 0.01f;

    Rigidbody _Rigidbody;
    Collider _Collider;

    bool _HasHit;
    Vector3 _SweepFrom;
    Vector3 _SweepTo;
    Vector3 _HitPoint;
    Vector3 _HitNormal;

    void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _Collider = GetComponent<Collider>();
    }

    void FixedUpdate()
    {
        // FixedUpdate runs BEFORE the step, so this is the movement the engine is ABOUT
        // to make. Nothing else acts on this ball - no gravity, no drag - so
        // velocity * step is exactly where it is going.
        Vector3 from = _Rigidbody.position;
        Vector3 delta = _Rigidbody.linearVelocity * Time.fixedDeltaTime;

        float distance = delta.magnitude;

        _SweepFrom = from;
        _SweepTo = from + delta;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 direction = delta / distance;

        // The sweep starts where we are, so it starts inside our own collider : that
        // reports a hit at distance 0 with a zero normal. Switch it off for the query.
        if (_Collider != null)
        {
            _Collider.enabled = false;
        }

        RaycastHit hit;
        bool touched = Physics.SphereCast(from, Radius, direction, out hit, distance);

        if (_Collider != null)
        {
            _Collider.enabled = true;
        }

        if (!touched)
        {
            _HasHit = false;
            return;
        }

        // Where the CENTRE of the ball is at the moment of contact : hit.distance along
        // the sweep. Not hit.point, which is on the surface, a radius further on.
        Vector3 contactCentre = from + direction * hit.distance;

        _Rigidbody.position = contactCentre + hit.normal * Skin;

        // Spend the velocity here. At Restitution 0 the ball stops dead, so the step
        // that follows moves it nowhere and it is never seen inside the wall.
        _Rigidbody.linearVelocity = Vector3.Reflect(_Rigidbody.linearVelocity, hit.normal) * Restitution;

        _HasHit = true;
        _HitPoint = hit.point;
        _HitNormal = hit.normal;

        HudConsole.Log("SweepCCD : stopped " + hit.distance.ToString("0.###")
                     + " m into a " + distance.ToString("0.###") + " m step");
    }

    // No OnCourseTeleport here : this version never remembers a previous position, so a
    // teleport has nothing to corrupt. Predicting forward costs one less thing to break.

    void OnDrawGizmos()
    {
        Gizmos.color = Color.grey;
        Gizmos.DrawLine(_SweepFrom, _SweepTo);
        Gizmos.DrawWireSphere(_SweepTo, Radius);

        if (!_HasHit)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_HitPoint, 0.1f);

        Gizmos.color = Color.green;
        DrawUtils.DrawArrow(_HitPoint, _HitNormal * 2.0f);
    }
}
