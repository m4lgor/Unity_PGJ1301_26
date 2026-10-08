using UnityEngine;

public class TODO_RagdollTarget : MonoBehaviour, IShootable
{
    SpringJoint _Rope;

    void Awake()
    {
        // The rope is on one of our bones : look for it in the children.
        _Rope = GetComponentInChildren<SpringJoint>();
    }

    public void OnShot(RaycastHit hit, Vector3 direction)
    {
        // The hero calls this when its shot hits one of our bones.
        //
        //   A shot to the HEAD breaks the rope. If the collider that was hit is the Head, break the
        //   joint
        //
        //   A joint breaks when the force it carries goes over its breakForce. Do not destroy the
        //   component : break the JOINT. Change its breakforce to a small value.
    }
}
