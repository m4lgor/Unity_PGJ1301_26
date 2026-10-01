using UnityEngine;

/// <summary>
/// The EARTH orbits the SUN.
///
/// This is the basic orbit: everything here is done in world space.
/// </summary>
public class TODO_Earth : MonoBehaviour
{
    [Tooltip("The centre we rotate around (Sun)")]
    [SerializeField] Transform CenterTransform;

    [Tooltip("Rotation speed around the centre, in degrees per second")]
    [SerializeField] float AngularSpeedDeg = 20.0f;

    void Start()
    {
        if (!CenterTransform)
        {
            Debug.LogError("TODO_Earth - CenterTransform is not assigned.", this);
            enabled = false;
            return;
        }

        // ----------- TODO Students ------------
        // 1. Store the vector that goes from the centre to you.
        //    Careful: this is a DIRECTION from the centre, not a position.
        //
        // 2. Pick a rotation axis.
        //    For the orbit to be a circle passing through your starting position, the
        //    axis has to be PERPENDICULAR to that vector. Which product gives you a
        //    vector perpendicular to two others?
        //    Trap: that product is zero when the two vectors are parallel.
        // --------------------------------------
    }

    void Update()
    {
        // ----------- TODO Students ------------
        // 1. Increase your angle using AngularSpeedDeg and Time.deltaTime.
        //
        // 2. Build the rotation with Quaternion.AngleAxis(angle, axis).
        //
        // 3. Apply it to the vector you stored in Start(), then ADD the centre's
        //    position: you rotate a direction, then you put it back on the centre.
        //
        // Allowed   : Quaternion.AngleAxis, Vector3.Cross, Vector3.Dot
        // Forbidden : Vector3.RotateAround, transform.RotateAround
        // --------------------------------------
    }
}
