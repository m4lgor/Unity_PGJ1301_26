using UnityEngine;

/// <summary>
/// SOLUTION - the hanging target.
///
/// The rope is already tied (a SpringJoint on the chest, anchored at the neck, to the hook) : find it.
///
/// Swing : nothing here. The hero pushes the bone it hits, the joints pass it to the body, and the
/// rope turns it into a swing.
///
/// Break : a joint breaks when the force it carries goes over its breakForce. A head shot gives it a
/// breakForce of nothing : at the next physics step the joint breaks - the physics engine lets go of
/// it, and Unity destroys the component. That is not the same as deleting the component ourselves :
/// the joint is torn by the physics.
/// </summary>
public class SOLUTION_RagdollTarget : MonoBehaviour, IShootable
{
    SpringJoint _Rope;

    void Awake()
    {
        _Rope = GetComponentInChildren<SpringJoint>();
    }

    public void OnShot(RaycastHit hit, Vector3 direction)
    {
        Debug.Log("Hit " + hit.collider.name);

        if (hit.collider.name.Contains("Head") && _Rope != null)
        {
            _Rope.breakForce = 0.0f;
        }
    }
}
