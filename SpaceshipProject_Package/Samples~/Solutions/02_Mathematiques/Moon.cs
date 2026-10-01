using UnityEngine;

/// <summary>
/// SOLUTION - "The Moon orbits the Earth, and the Earth always sees the same face".
///
/// Same orbit as Earth.cs, plus the "same face" part, written two ways that give the exact
/// same result:
///
/// ComposeOrbitRotation  turn on ourselves by the angle we swept around the centre.
/// LookAtWithOrbitAxis   LookAt the centre, with the ORBIT AXIS as up. Never Vector3.up: the
///                       orbit plane contains it, so it would flip twice per orbit.
/// </summary>
public class Moon : MonoBehaviour
{
    public enum SameFaceMethod
    {
        ComposeOrbitRotation,
        LookAtWithOrbitAxis,
    }

    [Tooltip("The Transform in the Center of our rotation")]
    [SerializeField] Transform CenterTransform;

    [Tooltip("The rotation speed around the Center, in degrees per second")]
    [SerializeField] float AngularSpeedDeg = 30.0f;

    [Tooltip("Always show the same face to the Center, the way the Moon does to the Earth")]
    [SerializeField] bool KeepSameFaceToCenter = true;

    [Tooltip("How we keep the same face. Both give the same result; the second one uses LookAt.")]
    [SerializeField] SameFaceMethod Method = SameFaceMethod.ComposeOrbitRotation;

    [SerializeField] bool EnableDebugDraw = false;

    Vector3 _PositionOffset;      // where we start, relative to the centre
    Vector3 _RotationAxis;        // the axis we turn around
    float _AngleDeg = 0.0f;

    Quaternion _RotationOffset;   // the orientation we start with
    Quaternion _LookCorrection;   // that same orientation, seen from the one LookAt gives us

    void Start()
    {
        if (!CenterTransform)
        {
            Debug.LogError("Moon - no CenterTransform assigned.", this);
            enabled = false;
            return;
        }

        _PositionOffset = transform.position - CenterTransform.position;
        _RotationOffset = transform.rotation;
        _RotationAxis = ComputeOrbitAxis(_PositionOffset);

        // LookAt aims +Z at the target and throws away the rotation authored in the scene, so
        // measure the authored one relative to it and re-apply that in Update.
        Vector3 toCentre = CenterTransform.position - transform.position;
        Quaternion aimedAtCentre = Quaternion.LookRotation(toCentre, _RotationAxis);
        _LookCorrection = Quaternion.Inverse(aimedAtCentre) * _RotationOffset;
    }

    /// <summary>
    /// Same helper as in Earth.cs, kept local so this script stands on its own. The axis has to
    /// be perpendicular to the offset, which is what a cross product gives; it collapses to zero
    /// on parallel inputs, hence the fallback reference.
    /// </summary>
    static Vector3 ComputeOrbitAxis(Vector3 positionOffset)
    {
        Vector3 offsetDirection = positionOffset.normalized;

        Vector3 reference = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(offsetDirection, reference)) > 0.99f)
        {
            reference = Vector3.right;
        }

        return Vector3.Cross(offsetDirection, reference).normalized;
    }

    void Update()
    {
        _AngleDeg += AngularSpeedDeg * Time.deltaTime;

        Quaternion orbit = Quaternion.AngleAxis(_AngleDeg, _RotationAxis);
        transform.position = CenterTransform.position + orbit * _PositionOffset;

        if (!KeepSameFaceToCenter)
        {
            return;
        }

        if ((CenterTransform.position - transform.position).sqrMagnitude < 0.000001f)
        {
            return; // sitting on the centre: no direction to aim along
        }

        switch (Method)
        {
            case SameFaceMethod.ComposeOrbitRotation:
                // The orbit rotation applied to ourselves as well: we turn by exactly the angle
                // we swept, so the face that pointed at the centre keeps pointing at it.
                transform.rotation = orbit * _RotationOffset;
                break;

            case SameFaceMethod.LookAtWithOrbitAxis:
                // LookAt writes straight into transform.rotation, so the correction goes on the
                // line after. Identical to the case above on every frame.
                transform.LookAt(CenterTransform, _RotationAxis);
                transform.rotation = transform.rotation * _LookCorrection;
                break;
        }
    }

    void OnDrawGizmos()
    {
        if (!EnableDebugDraw || !CenterTransform)
        {
            return;
        }

        Vector3 offset = Application.isPlaying
            ? _PositionOffset
            : transform.position - CenterTransform.position;

        if (Mathf.Approximately(offset.magnitude, 0.0f))
        {
            return;
        }

        Vector3 axis = Application.isPlaying ? _RotationAxis : ComputeOrbitAxis(offset);
        Vector3 axisDraw = axis * offset.magnitude;

        Gizmos.color = Color.white;
        Gizmos.DrawLine(CenterTransform.position - axisDraw, CenterTransform.position + axisDraw);

        // The locked face: this ray should keep pointing at the centre the whole way round.
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, (CenterTransform.position - transform.position).normalized);
    }
}
