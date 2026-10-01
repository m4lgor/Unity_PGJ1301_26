using UnityEngine;

/// <summary>
/// The door. A sphere walks through a doorway, back and forth, forever. The door hangs from
/// the top of the doorway, and swings in the plane you see.
///
///     Closed by default.
///     Opens for the sphere, in the direction that lets it pass - AWAY from it.
///     Closes again once the sphere is far enough.
///     If the sphere comes back, it opens again, the other way.
///
/// Right now nothing holds the door : it floats, and the sphere pushes it out of the way.
///
///     1. CreateHinge         hang the door from the top of the doorway.
///     2. ApplyMotor          a motor, to turn it.
///     3. SpeedToReachAngle   "go to that angle" : the same speed all the way, then stop.
///     4. ChooseTargetAngle   the brain. A query finds the sphere, and decides which way.
///
/// A bonk is the sphere hitting the door. The score is crossings, with no bonks.
/// </summary>
[RequireComponent(typeof(DoorCourse))]
public class TODO_DoorMotor : MonoBehaviour
{
    [Tooltip("The most torque the motor may push with. Too low and the sphere pushes the door open")]
    public float MotorForce = 50.0f;

    [Tooltip("How far the door opens, in degrees. 90 is flat against the wall")]
    public float OpenAngle_Degrees = 85.0f;

    [Tooltip("The sphere makes the door open when it comes closer than this, in metres")]
    public float DetectRadius = 4.0f;

    [Tooltip("The door closes when the sphere is further than this, in metres. Bigger than DetectRadius")]
    public float CloseRadius = 5.0f;

    [Tooltip("How fast the door turns, in degrees per second. Always the same speed")]
    public float DoorSpeed_DegreesPerSecond = 60.0f;

    [Tooltip("The door stops when it is this close to its angle, in degrees")]
    public float Tolerance_Degrees = 3.0f;

    DoorCourse _Course;
    HingeJoint _Hinge;

    // 0 : the door is shut.  +1 or -1 : the door is open, toward one side or the other.
    float _OpenSign;

    void Start()
    {
        _Course = GetComponent<DoorCourse>();

        CreateHinge();
    }

    void FixedUpdate()
    {
        if (_Hinge == null)
        {
            return;
        }

        float targetAngle_Degrees = ChooseTargetAngle();
        float speedToReachAngle = SpeedToReachAngle(targetAngle_Degrees);
        ApplyMotor(speedToReachAngle);
    }

    void CreateHinge()
    {
    // Same recipe as the joints of the last workshop. The door owns the joint, and hangs
    // from the frame :
    //
    // 1. AddComponent<HingeJoint>() on the DOOR (_Course.Door), connectedBody = the frame
    //    (_Course.Frame), autoConfigureConnectedAnchor = false.
    
    //    Use this to find the door :
    //    _Course.Door.gameObject.AddComponent<HingeJoint>();
    //
    // 2. anchor          on the door  - _Course.DoorPointTransform
    //    connectedAnchor on the frame - _Course.FramePointTransform
    //
    //    Those are world transforms. You need to change them to local space for the door and the frame.
    //    youDoorWorldTransform.InverseTransformPoint(yourAnchorWorldPosition);
    //
    // 3. axis : What is the rotation axis of the door ? Sphere moves along X. Wall is along Y. 
    //    _Hinge.axis = ???
    //
    // Stretch : useLimits, so that the door cannot go all the way round.
    }

    void ApplyMotor(float targetVelocity_DegreesPerSecond)
    {
        _Hinge.useMotor = true;

        // JointMotor is a struct : joint.motor is a copy, so it goes back in when it is changed.
        JointMotor motor = _Hinge.motor;
        motor.targetVelocity = targetVelocity_DegreesPerSecond;
        motor.force = MotorForce;
        _Hinge.motor = motor;
    }

    float SpeedToReachAngle(float targetAngle_Degrees)
    {
    // You can access the joint's current angle with : _Hinge.angle.
    // Compare this value with the targetAngle_Degrees in parameter, and decide of the
    // speed that should be returned. If we've reached the targetAngle_Degrees, we stop.
    //
    //     not there yet  : the same speed, DoorSpeed_DegreesPerSecond, toward the target
    //     there          : speed 0 - the motor holds the door where it is
    //
    // ! A motor does not answer at once - it takes a moment to reach the speed you asked
    // ! for. Too slow a door and the sphere gets there first.
    // Use the provided default values to prevent this :) 

        return 0.0f;
    }

    float ChooseTargetAngle()
    {
    // Returns the angle the door should be at. 0 is closed.

    // if the door is shut :   look around with DetectRadius
    // else :                  look around with CloseRadius        (bigger, so the door does not flap)
    //
    // colliders = Physics.OverlapSphere(_Course.Doorway_WorldSpace, radius)
    // DoorVisitor visitor = null;
    // for (int i = 0; i < count && visitor == null; i++)
    // {
    //    colliders[i].TryGetComponent(out visitor);
    // }
    //
    // if there is no visitor :
    //     _OpenSign = 0                                           (shut)
    //     return 0
    //
    // if the door is shut :                                       (_OpenSign == 0)
    //     side = Vector3.Dot(visitor position - doorway position, _Course.WallNormal_WorldSpace)
    //     open AWAY from the visitor :
    //         _OpenSign = +1 or -1, depending on the sign of side          (try it !)
    //
    // return _OpenSign * OpenAngle_Degrees

        return 0.0f;
    }

    void OnDrawGizmos()
    {
    // Draw the radius you query with. A query you cannot see is a query you cannot debug.
    }
}
