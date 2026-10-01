using UnityEngine;

/// <summary>
/// The MOON orbits the EARTH, and the EARTH always sees the same face of the MOON.
///
/// The orbit itself is the same as the Earth's. The only new part here is the
/// "same face" requirement.
/// </summary>
public class TODO_Moon : MonoBehaviour
{
    [Tooltip("The centre we rotate around (Earth)")]
    [SerializeField] Transform CenterTransform;

    [Tooltip("Rotation speed around the centre, in degrees per second")]
    [SerializeField] float AngularSpeedDeg = 30.0f;

    [Tooltip("Always show the same face to the centre, the way the Moon does to the Earth")]
    [SerializeField] bool KeepSameFaceToCenter = true;

    void Start()
    {
        if (!CenterTransform)
        {
            Debug.LogError("TODO_Moon - CenterTransform is not assigned.", this);
            enabled = false;
            return;
        }

        // ----------- TODO Students ------------
        // 1. Store your starting offset from the centre.
        //
        // 2. Store your starting ORIENTATION as well: you will need it for the
        //    "same face" part.
        //
        // 3. Pick an axis perpendicular to the offset (same reasoning as the Earth).
        // --------------------------------------
    }

    void Update()
    {
        // ----------- TODO Students ------------
        // 1. Increase the angle and build the orbit rotation with Quaternion.AngleAxis.
        //
        // 2. Apply it to the offset, then add the centre's position.
        //
        // 3. For KeepSameFaceToCenter, two possible methods:
        //
        //    a) With a LookAt. You orient the Moon towards the centre every frame.
        //
        //       CAREFUL: the naive version, transform.LookAt(CenterTransform), BREAKS
        //       twice per orbit. LookAt needs an "up" vector and defaults to Vector3.up,
        //       but your orbit plane happens to contain the vertical. When the direction
        //       to the centre becomes parallel to up, the orientation is no longer
        //       defined and the Moon flips over in a single frame.
        //
        //       The fix: give it an up vector that can NEVER be parallel to that
        //       direction. Your orbit axis is exactly that, since it is perpendicular
        //       to the offset by construction:
        //
        //           Quaternion.LookRotation(directionToCentre, yourOrbitAxis)
        //
        //    b) Without any LookAt. Turn on yourself by exactly the same angle, around
        //       the same axis, as your orbit: the face that pointed at the centre at the
        //       start keeps pointing at it. So compose the SAME rotation with your
        //       starting orientation. No up vector, therefore no degenerate case.
        //
        // Allowed   : Quaternion.AngleAxis, Quaternion.LookRotation, Transform.LookAt,
        //             Vector3.Cross, Vector3.Dot
        // Forbidden : Vector3.RotateAround
        // --------------------------------------
    }
}
