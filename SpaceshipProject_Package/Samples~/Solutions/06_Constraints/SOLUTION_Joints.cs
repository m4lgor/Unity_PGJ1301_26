using UnityEngine;

/// <summary>
/// SOLUTION - three joints.
///
/// Every joint here is built the same way : on the pyramid, connected to the platform,
/// with one anchor per body - the tip on the pyramid, the hang point on the platform -
/// each turned into the local space of the body it belongs to. Only what the joint
/// allows afterwards differs.
/// </summary>
[RequireComponent(typeof(JointsCourse))]
public class SOLUTION_Joints : MonoBehaviour
{
    [Tooltip("SpringJoint : stiffness once the distance limit is crossed")]
    public float Spring = 1000.0f;

    [Tooltip("SpringJoint : how fast the spring gives up its energy")]
    public float Damper = 10.0f;

    void Start()
    {
        JointsCourse course = GetComponent<JointsCourse>();

        CreateDistanceJoint(course.Distance);
        CreateHingeJoint(course.Hinge);
        CreateFixedJoint(course.Fixed);
    }

    static T Attach<T>(JointsCourse.Station station) where T : Joint
    {
        T joint = station.Pyramid.gameObject.AddComponent<T>();
        joint.connectedBody = station.Platform;

        // Off, or the engine rewrites connectedAnchor to keep the pyramid exactly where it
        // is now - which is not the same as hanging it from the hang point.
        joint.autoConfigureConnectedAnchor = false;

        // A Transform holds a position in BOTH spaces : .position is world space,
        // .localPosition is local to its parent. The joint wants neither - it wants each
        // point in the local space of the body that OWNS the anchor.
        Vector3 tipPosition_WorldSpace = station.TipTransform.position;
        Vector3 hangPointPosition_WorldSpace = station.HangPointTransform.position;

        Vector3 tipPosition_PyramidSpace = station.Pyramid.transform.InverseTransformPoint(tipPosition_WorldSpace);
        Vector3 hangPointPosition_PlatformSpace = station.Platform.transform.InverseTransformPoint(hangPointPosition_WorldSpace);

        joint.anchor = tipPosition_PyramidSpace;                    // on the joint's owner : the pyramid
        joint.connectedAnchor = hangPointPosition_PlatformSpace;    // on the connected body : the platform
        return joint;
    }

    void CreateDistanceJoint(JointsCourse.Station station)
    {
        SpringJoint joint = Attach<SpringJoint>(station);

        // The joint has no idea what the distance "should" be : minDistance and maxDistance
        // are plain numbers. Measure it where the pyramid starts, or it will fall until it
        // reaches whatever number you guessed. Both points are world-space positions here,
        // so the distance between them is a length in metres in any space.
        float startDistance = Vector3.Distance(station.TipTransform.position, station.HangPointTransform.position);

        // A rope : free inside the range, and a spring pulling back outside it.
        // Set min = max for a rod instead.
        joint.minDistance = 0.0f;
        joint.maxDistance = startDistance;
        joint.spring = Spring;
        joint.damper = Damper;

        // A dead zone past maxDistance (0.025 m by default) where the spring does not pull
        // yet. Zero it, or the pyramid settles a few centimetres under where it started.
        joint.tolerance = 0.0f;
    }

    void CreateHingeJoint(JointsCourse.Station station)
    {
        HingeJoint joint = Attach<HingeJoint>(station);

        // Local space of the pyramid, which starts unrotated - so this is the world Z the
        // camera looks along, and the pyramid swings in the plane you see. A hinge pinned
        // at the tip and turning about Z is a pendulum : the pyramid is stable hanging
        // straight down, and swings about that when the platform moves.
        joint.axis = Vector3.forward;
    }

    void CreateFixedJoint(JointsCourse.Station station)
    {
        Attach<FixedJoint>(station);
    }
}
