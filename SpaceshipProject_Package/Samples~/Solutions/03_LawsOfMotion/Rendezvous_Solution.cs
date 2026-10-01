using UnityEngine;

/// <summary>
/// Solution for TP_Rendezvous.
///
/// Two versions are here, picked by UseVelocityProfile.
///
///   false - the straightforward answer, exactly what the TODO steps describe. Full
///           force towards the target until the braking distance runs out, then full
///           force the other way. It passes the course, but it chatters around each
///           target because MaxForce is far too much for the last centimetres.
///
///   true  - the tidy answer. Instead of asking "should I brake now", it asks "how fast
///           am I allowed to be at this distance", which is sqrt(2 a d), and pushes to
///           correct the difference. It brakes at the right moment for free, throttles
///           down on its own near the target, and settles without chattering.
///
/// Both derive the acceleration from F = m a rather than hard coding it, and both stay
/// inside the force budget this component sets for itself.
///
/// To use: replace TODO_Rendezvous with this component on the Probe, or copy the body of
/// FixedUpdate() across.
/// </summary>
public class Rendezvous_Solution : RendezvousControllerBase
{
    public float Acceleration = 4f;

    void FixedUpdate()
    {
        Vector3 toTarget = TargetPosition - transform.position;
        Vector3 velocity = _Rigidbody.linearVelocity;

        // braking distance = v * v / (2 * a)                                                                                                                                                                                                                                                                    
        float brakingDistance = velocity.sqrMagnitude / (2f * Acceleration);

        // Enough room left to stop? push towards the target. Otherwise push against                                                                                                                                                                                                                             
        // the way we are moving.                                                                                                                                                                                                                                                                                
        Vector3 push = brakingDistance < toTarget.magnitude
            ? toTarget.normalized
            : -velocity.normalized;

        _Rigidbody.AddForce(push * Acceleration * _Rigidbody.mass, ForceMode.Force);
    }
}
