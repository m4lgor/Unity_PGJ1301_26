using UnityEngine;

/// <summary>
/// Measures the impulse a body actually received in a collision, straight from the
/// physics engine, as J = m * (v_after - v_before).
///
/// That is the definition of impulse, so there is nothing to take on trust here: put one
/// of these on each of two colliding bodies and the two readings come out equal and
/// opposite. This is Newton's third law, measured rather than asserted.
///
/// The velocity is captured in FixedUpdate, which Unity runs BEFORE the physics step,
/// while OnCollisionEnter arrives AFTER it. So the two samples straddle the collision.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ImpactReporter : MonoBehaviour
{
    [System.NonSerialized] public Vector3 Impulse;
    [System.NonSerialized] public bool HasImpact;
    [System.NonSerialized] public float ImpactTime;

    Rigidbody _Body;
    Vector3 _VelocityBeforeStep;

    void Awake()
    {
        _Body = GetComponent<Rigidbody>();
    }

    public void Clear()
    {
        Impulse = Vector3.zero;
        HasImpact = false;
        ImpactTime = 0.0f;
    }

    void FixedUpdate()
    {
        _VelocityBeforeStep = _Body.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only the first hit of a run is of interest: that is the one the panel reports.
        if (HasImpact)
        {
            return;
        }

        Impulse = _Body.mass * (_Body.linearVelocity - _VelocityBeforeStep);
        HasImpact = true;
        ImpactTime = Time.time;
    }
}
