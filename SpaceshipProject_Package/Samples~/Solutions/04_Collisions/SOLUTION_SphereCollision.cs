using UnityEngine;

/// <summary>
/// SOLUTION for TP_Manifold - sphere against sphere, detected and resolved by hand.
///
/// To use: replace TODO_SphereCollision on a lane with this, or copy the three bodies
/// across. A and B still have to be wired up in the inspector.
/// </summary>
public class SOLUTION_SphereCollision : MonoBehaviour
{
    public Rigidbody A;
    public Rigidbody B;

    public float RadiusA = 1.5f;
    public float RadiusB = 1.5f;

    [Range(0.0f, 1.0f)]
    public float Restitution = 1.0f;

    /// <summary>
    /// STEP 1. Looks, reports, changes nothing - which is what lets the gizmo call it in
    /// the editor with the game stopped.
    ///
    /// transform.position rather than Rigidbody.position for the same reason: the
    /// transform is readable outside play mode.
    /// </summary>
    Manifold Detect()
    {
        if (A == null || B == null)
        {
            return Manifold.None;
        }

        Vector3 aToB = B.transform.position - A.transform.position;
        float distance = aToB.magnitude;
        float touching = RadiusA + RadiusB;

        if (distance >= touching)
        {
            return Manifold.None;
        }

        // Dead centres. There is no direction to push along, so pick one rather than
        // hand back a zero normal that silently does nothing.
        Vector3 normal = distance > 0.0001f ? aToB / distance : Vector3.right;
        float penetration = touching - distance;

        return new Manifold
        {
            IsColliding = true,
            Normal = normal,
            Penetration = penetration,

            // Out to A's surface, then back half the overlap: the middle of the lens.
            // Comes out the same from either sphere, which the different radii in
            // lane A are there to prove.
            ContactPoint = A.transform.position + normal * (RadiusA - penetration * 0.5f),
        };
    }

    // STEP 2
    void OnDrawGizmos()
    {
        Manifold m = Detect();

        if (!m.IsColliding)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(m.ContactPoint, 0.15f);

        Gizmos.color = Color.red;
        DrawUtils.DrawArrow(m.ContactPoint, m.Normal * 2.0f);

        // How deep they are in, drawn along the normal from the contact point.
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(m.ContactPoint - m.Normal * (m.Penetration * 0.5f),
                        m.ContactPoint + m.Normal * (m.Penetration * 0.5f));
    }

    // STEP 3
    void FixedUpdate()
    {
        Manifold m = Detect();

        if (!m.IsColliding)
        {
            return;
        }

        // A kinematic body does not move and does not care: infinite mass, so zero
        // inverse mass. That also makes the sandbox lane safe to leave this on.
        float invMassA = A.isKinematic ? 0.0f : 1.0f / A.mass;
        float invMassB = B.isKinematic ? 0.0f : 1.0f / B.mass;
        float invSum = invMassA + invMassB;

        if (invSum <= 0.0f)
        {
            return;
        }

        // Unstick them first. Nothing stopped them interpenetrating, so without this
        // they can still be inside each other next step and collide again, for ever.
        // Shared by inverse mass, so the heavy one barely shifts.
        Vector3 correction = m.Normal * (m.Penetration / invSum);
        A.position -= correction * invMassA;
        B.position += correction * invMassB;

        // The part of the relative velocity that lies along the normal - the only part
        // a frictionless contact can change. Negative while they are closing, because
        // the normal runs from A to B.
        Vector3 relative = B.linearVelocity - A.linearVelocity;
        float alongNormal = Vector3.Dot(relative, m.Normal);

        // Overlapping but already separating. The correction above leaves them a hair
        // inside each other, so this case really does come up on the very next step -
        // and without this line the impulse comes out backwards and sucks them
        // together, which is what makes a naive solver buzz instead of bounce.
        if (alongNormal > 0.0f)
        {
            return;
        }

        float j = -(1.0f + Restitution) * alongNormal / invSum;

        A.AddForce(-j * m.Normal, ForceMode.Impulse);
        B.AddForce(j * m.Normal, ForceMode.Impulse);
    }
}
