using UnityEngine;

/// <summary>
/// SOLUTION - the door.
///
/// Hinge : on the door, connected to the frame, one anchor per body (the top end of the
/// door, the top of the doorway), the axis along Z - the line the camera looks along, so
/// the door swings in the plane you see.
///
/// Motor : a speed, and a torque limit. The door always turns at the same speed, toward its
/// angle, and stops when it gets there.
///
/// Brain : a query finds the sphere. The side it is on decides the direction, once, while
/// the door is shut. Two radii - a small one to open, a big one to stay open - keep the
/// door from flapping.
/// </summary>
[RequireComponent(typeof(DoorCourse))]
public class SOLUTION_DoorMotor : MonoBehaviour
{
    public float MotorForce = 50.0f;
    public float OpenAngle_Degrees = 85.0f;
    public float DetectRadius = 4.0f;
    public float CloseRadius = 5.0f;

    [Tooltip("How fast the door turns, in degrees per second. Always the same speed")]
    public float DoorSpeed_DegreesPerSecond = 60.0f;

    [Tooltip("The door stops when it is this close to its angle, in degrees")]
    public float Tolerance_Degrees = 3.0f;

    [Tooltip("Off : the door opens TOWARD the sphere. Watch the bonks - the closer the sphere is " +
             "when the door is told, the worse (DetectRadius 4 or less)")]
    public bool OpenAwayFromVisitor = true;

    DoorCourse _Course;
    HingeJoint _Hinge;

    // 0 : shut. Otherwise the sign of the angle the door is open at.
    float _OpenAngleSign;

    readonly Collider[] _Colliders = new Collider[16];

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
        ApplyMotor(SpeedToReachAngle(targetAngle_Degrees));
    }

    void CreateHinge()
    {
        _Hinge = _Course.Door.gameObject.AddComponent<HingeJoint>();
        _Hinge.connectedBody = _Course.Frame;

        // Off, or the engine rewrites connectedAnchor to keep the door where it is now.
        _Hinge.autoConfigureConnectedAnchor = false;

        Vector3 doorPointPosition_WorldSpace = _Course.DoorPointTransform.position;
        Vector3 framePointPosition_WorldSpace = _Course.FramePointTransform.position;

        Vector3 doorPointPosition_DoorSpace = _Course.Door.transform.InverseTransformPoint(doorPointPosition_WorldSpace);
        Vector3 framePointPosition_FrameSpace = _Course.Frame.transform.InverseTransformPoint(framePointPosition_WorldSpace);

        _Hinge.anchor = doorPointPosition_DoorSpace;            // on the joint's owner : the door
        _Hinge.connectedAnchor = framePointPosition_FrameSpace; // on the connected body : the frame

        // Local space of the door, which starts unrotated - so this is the world Z the
        // camera looks along, and the door turns in the plane you see.
        _Hinge.axis = Vector3.forward;

        // Without limits a push could take the door all the way round.
        _Hinge.useLimits = true;
        _Hinge.limits = new JointLimits { min = -100.0f, max = 100.0f };
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
        // The same speed all the way, and stop once there : no acceleration, no easing.
        float remaining_Degrees = targetAngle_Degrees - _Hinge.angle;
        if (Mathf.Abs(remaining_Degrees) <= Tolerance_Degrees)
        {
            return 0.0f;
        }

        return Mathf.Sign(remaining_Degrees) * DoorSpeed_DegreesPerSecond;
    }

    float ChooseTargetAngle()
    {
        Vector3 doorway_WorldSpace = _Course.Doorway_WorldSpace;

        // 1. Who is around ? The small radius opens the door, the big one keeps it open.
        float radius = _OpenAngleSign == 0.0f ? DetectRadius : CloseRadius;
        int count = Physics.OverlapSphereNonAlloc(doorway_WorldSpace, radius, _Colliders,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        DoorVisitor visitor = null;
        for (int i = 0; i < count && visitor == null; i++)
        {
            _Colliders[i].TryGetComponent(out visitor);
        }

        // 2. Nobody : shut.
        if (visitor == null)
        {
            _OpenAngleSign = 0.0f;
            return 0.0f;
        }

        // 3. Somebody, and the door is shut : choose the direction NOW, and keep it. Changing
        //    it while the door is open would swing it through the sphere.
        if (_OpenAngleSign == 0.0f)
        {
            Vector3 visitorPosition_WorldSpace = visitor.transform.position;
            float side = Mathf.Sign(Vector3.Dot(visitorPosition_WorldSpace - doorway_WorldSpace, _Course.WallNormal_WorldSpace));

            // With this axis a positive angle swings the door toward +X (measured).
            // The sphere on the -X side (side < 0) wants the door to go to +X : positive.
            _OpenAngleSign = OpenAwayFromVisitor ? -side : side;
        }

        return _OpenAngleSign * OpenAngle_Degrees;
    }

    void OnDrawGizmos()
    {
        if (_Course == null)
        {
            return;
        }

        Gizmos.color = _OpenAngleSign == 0.0f ? Color.green : Color.red;
        Gizmos.DrawWireSphere(_Course.Doorway_WorldSpace, _OpenAngleSign == 0.0f ? DetectRadius : CloseRadius);
    }
}
