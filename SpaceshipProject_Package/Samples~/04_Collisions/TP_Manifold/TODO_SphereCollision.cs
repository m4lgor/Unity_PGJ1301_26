using UnityEngine;

/// <summary>
/// TP Manifold - detect and resolve a sphere against a sphere, yourself.
///
/// These spheres have NO COLLIDERS. Unity moves them and nothing else: it will not
/// notice they touch, and it will not push them apart. That gap is the workshop.
///
/// A and B are already wired up in the inspector. One of these sits on each lane, so
/// you never have to go looking for pairs - finding what might be touching is a
/// different problem (broadphase) and not today's.
///
/// Work in order. Each step runs and shows you something before the next one.
/// </summary>
public class TODO_SphereCollision : MonoBehaviour
{
    public Rigidbody A;
    public Rigidbody B;

    [Tooltip("These spheres are scaled 3 or 5, and a Unity sphere is 1 unit across, " +
             "so the radius is half the scale")]
    public float RadiusA = 1.5f;
    public float RadiusB = 1.5f;

    [Tooltip("1 = a perfect bounce, nothing lost. 0 = they stop dead against each other")]
    [Range(0.0f, 1.0f)]
    public float Restitution = 1.0f;

    /// STEP 1 - Detect the collision
    Manifold Detect()
    {
    // Use A.transform.position and B.transform.position
    // Use RadiusA and RadiusB
    //
    // Everything you need comes out of one subtraction:
    //
    //     Vector3 aToB    = B.transform.position - A.transform.position;
    //     float   distance = aToB.magnitude;
    //
    // There is contact when distance is less than RadiusA + RadiusB.
    
    // If they are not touching, return Manifold.None.
    //
    // When they are touching, fill in and return a Manifold with:
    //
    //     IsColliding   true
    //     Normal        aToB, normalised. Watch the direction - it goes from A to B.
    //     Penetration   RadiusA + RadiusB - distance     (how deep they are in)
    //     ContactPoint  A.transform.position + Normal * (RadiusA - Penetration * 0.5f)
    //
    // Careful: distance can be 0 (Invalid) if the two centres land exactly on top of each other.
    // normalized returns a zero vector there rather than exploding, which quietly gives
    // you a collision with no direction. Guard it however you like.
    //
    // -----------------------------------------------
        return Manifold.None;
    }

    /// STEP 2 - Show your detection result
    void OnDrawGizmos()
    {
    // Draw your Detection. This will help you debug.
    //
    // Bail out if A or B is null, then:
    //
    //     Manifold m = Detect();
    //     if (!m.IsColliding) return;
    //
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawSphere(m.ContactPoint, 0.15f);
    //
    //     Gizmos.color = Color.red;
    //     DrawUtils.DrawArrow(m.ContactPoint, m.Normal * 2.0f);
    //
    // -----------------------------------------------
    }

    /// <summary>
    ///  Step 3 - Resolve the collision
    /// </summary>
    void FixedUpdate()
    {
    //     Manifold m = Detect();
    //     if (!m.IsColliding) return;
    //
    // Then two jobs, in this order.
    //
    // FIRST, unstick/uncollide them. 
    // Before applying an impulse, you will need to move them exactly appart to not collide again in the next FixedUpdate.
    //  
    //     Pseudo Code : 
    //     invMassA = 1.0f / A.mass;          
    //     invMassB = 1.0f / B.mass;        
    //     correction = m.Normal * (m.Penetration / (invA + invB));
    //     positionA -= correction * invA;
    //     positionB += correction * invB;
    //
    // SECOND, the bounce, which is one impulse on each body
    //
    //     Actual Code (This is getting complicated) : 
    //     Vector3 relative = B.linearVelocity - A.linearVelocity;
    //     float   alongNormal    = Vector3.Dot(relative, m.Normal);
    //
    //     If their velocities are opposite we don't need to do anything 
    //     if (alongNormal > 0.0f) return;      // already flying apart, leave them alone
    //
    //     float j = -(1.0f + Restitution) * alongNormal / (invMassA + invMassB);
    //
    //     A.AddForce(-j * m.Normal, ForceMode.Impulse);
    //     B.AddForce( j * m.Normal, ForceMode.Impulse);
    //
    // -----------------------------------------------
    }
}