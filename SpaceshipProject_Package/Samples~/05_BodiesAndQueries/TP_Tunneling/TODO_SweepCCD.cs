using UnityEngine;

/// <summary>
/// Lane 1c - write Continuous yourself.
///
///     Discrete   moves, then asks "am I touching something HERE ?"
///     Continuous asks "will I cross something ON THE WAY ?", and stops before it does
///
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TODO_SweepCCD : MonoBehaviour
{
    public float Radius = 0.25f;

    [Tooltip("1 = perfect bounce. 0 = stops dead on the wall")]
    [Range(0.0f, 1.0f)]
    public float Restitution = 0.0f;

    void FixedUpdate()
    {
    // 1. FixedUpdate runs BEFORE the physics step, so the movement it is about to make
    //    is still yours to inspect : your velocity times the step.
    //
    // 2. Sweep that movement with Physics.SphereCast, from where you are now.
    //    It starts inside YOUR OWN collider, which answers at distance 0.
    //    ! Disable your collider during the sweep.. !
    //
    // 3. On a hit : move to the contact, and spend your velocity there. At Restitution
    //    0 it stops dead, so the step that follows takes you nowhere.
    //    Careful, hit.point is on the SURFACE. That is not where your centre goes.
    //
    // 4. Leave a millimetre of gap off the surface, or the engine reads it as an
    //    overlap and pushes you out itself.
    }

    void OnDrawGizmos()
    {
    // Draw the segment you swept, and what you hit.
    // A query you cannot see is a query you cannot debug.
    }
}