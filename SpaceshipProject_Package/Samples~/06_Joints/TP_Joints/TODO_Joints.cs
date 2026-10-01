using UnityEngine;

/// <summary>
/// Three pairs, three joints. Each pair is a kinematic platform that sways, and an
/// asymmetrical pyramid that falls with gravity.
///
/// Right now nothing holds the pyramids, so they drop to the floor. Give each one a joint,
/// in Start, so that it hangs from its platform BY ITS TIP.
///
///     Distance : SpringJoint  (the distance joint - Unity has no other)
///     Hinge    : HingeJoint
///     Fixed    : FixedJoint
///
/// The pyramid is tall and pointed on purpose : its tip is a long way from its centre. A
/// joint that grabs the centre (or the base) will hang the pyramid from the wrong place,
/// and you will see it.
/// </summary>
[RequireComponent(typeof(JointsCourse))]
public class TODO_Joints : MonoBehaviour
{
    void Start()
    {
        JointsCourse course = GetComponent<JointsCourse>();

        CreateDistanceJoint(course.Distance);
        CreateHingeJoint(course.Hinge);
        CreateFixedJoint(course.Fixed);
    }

    void CreateDistanceJoint(JointsCourse.Station station)
    {
    // 1. Put the joint on the pyramid, and point it at the platform :
    //        station.Pyramid.gameObject.AddComponent<SpringJoint>()
    //    then set connectedBody.
    //
    // 2. Two anchors, and each one lives in the LOCAL space of ITS OWN body :
    //        anchor           on the pyramid  - the tip
    //        connectedAnchor  on the platform - the hang point
    //    Transform.InverseTransformPoint turns a world position into local space.
    //
    //    Name every Vector3 after the space it is in - it is the one thing that keeps
    //    this straight :
    //        tipPosition_WorldSpace     what station.TipTransform.position gives you
    //        tipPosition_PyramidSpace   the same point, seen from the pyramid
    //    and the same two for the hang point, with PlatformSpace instead.
    //    A Transform is in neither : it holds both a world and a local position.
    //
    //    ! Set autoConfigureConnectedAnchor to false first. While it is true the engine
    //    ! overwrites connectedAnchor with whatever keeps the pyramid where it is now.
    //
    // 3. SpringJoint is a distance joint : it only acts once the anchors are further
    //    apart than maxDistance (or closer than minDistance). Inside that range the
    //    pyramid falls freely.
    //    minDistance and maxDistance are plain numbers in metres : the joint does not
    //    look at where the pyramid starts. The pyramid starts a little below its hang
    //    point - measure that gap, or it will fall until it reaches your guess.
    //    The limit is enforced BY the spring : with spring = 0 the joint does nothing.
    //    Try a large spring (hundreds) with a little damper.
    //    Try  min = max  (a rod),  then  min = 0  (a rope).
    }

    void CreateHingeJoint(JointsCourse.Station station)
    {
    // Same two anchors as above. Then the axis - the line the pyramid may turn about,
    // in the local space of the body that owns the joint.
    //
    // The camera looks along Z. Which axis makes the pyramid swing in the plane you see ?
    // And what does Vector3.up do ?
    //
    // Stretch : useLimits, and limits.
    }

    void CreateFixedJoint(JointsCourse.Station station)
    {
    // Nothing to configure but the two bodies and the anchors : the pyramid is welded to
    // the platform, and goes wherever it goes.
    //
    // Stretch : breakForce, and OnJointBreak (a message the engine sends the object that
    // owns the joint). Then hit the platform hard enough to snap it.
    }
}

// Note that you never wrote a "the pyramid is 3 metres below the platform". A joint
// remembers a POINT on each body, not a position in the world - so when the platform
// sways, the pyramid follows, and the tip stays exactly where you pinned it.
