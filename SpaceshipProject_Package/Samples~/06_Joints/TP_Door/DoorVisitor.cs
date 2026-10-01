using UnityEngine;

// -----------------------
// Not for you to edit. This is the label on the sphere : "this is someone the door is
// for". A query hands you every collider it meets - the walls, the door itself - and this
// is how you pick the visitor out :
//
//     collider.TryGetComponent(out DoorVisitor visitor)
//
// It also tells the course when the sphere runs into the door.
// -----------------------

public class DoorVisitor : MonoBehaviour
{
    [SerializeField] DoorCourse Course;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody == Course.Door)
        {
            Course.RegisterBonk();
        }
    }
}
