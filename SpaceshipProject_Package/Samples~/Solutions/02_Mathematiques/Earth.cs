using UnityEngine;

/// <summary>
/// SOLUTION - "The Earth orbits the Sun".
///
/// RotateStartOffset      what the TP asks for: rotate the starting offset, add the centre back.
/// TransformRotateAround  the shortcut the TP forbids. Same motion here, but incremental, and
///                        only correct because the Sun never moves.
/// </summary>
public class Earth : MonoBehaviour
{
    public enum OrbitMethod
    {
        RotateStartOffset,
        TransformRotateAround,
    }

    [Tooltip("The Transform in the Center of our rotation")]
    [SerializeField] Transform CenterTransform;

    [Tooltip("The rotation speed around the Center, in degrees per second")]
    [SerializeField] float AngularSpeedDeg = 20.0f;

    [Tooltip("How we travel along the orbit. Both are correct while the Center stays still.")]
    [SerializeField] OrbitMethod Method = OrbitMethod.RotateStartOffset;

    [SerializeField] bool EnableDebugDraw = false;

    Vector3 _PositionOffset;   // where we start, relative to the centre
    Vector3 _RotationAxis;     // the axis we turn around
    float _AngleDeg = 0.0f;    // how far we have travelled since Start

    void Start()
    {
        if (!CenterTransform)
        {
            Debug.LogError("Earth - no CenterTransform assigned.", this);
            enabled = false;
            return;
        }

        _PositionOffset = transform.position - CenterTransform.position;
        _RotationAxis = ComputeOrbitAxis(_PositionOffset);
    }

    /// <summary>
    /// For the orbit to be a circle through our starting point, the axis has to be
    /// PERPENDICULAR to the offset - exactly what a cross product gives. It collapses to zero
    /// when its inputs are parallel, hence the fallback reference.
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
        float deltaDeg = AngularSpeedDeg * Time.deltaTime;

        // Advanced in both cases, so switching Method while playing does not jump.
        _AngleDeg += deltaDeg;

        switch (Method)
        {
            case OrbitMethod.RotateStartOffset:
                // The offset is a DIRECTION from the centre, so rotate it and add the centre
                // back afterwards.
                Quaternion orbit = Quaternion.AngleAxis(_AngleDeg, _RotationAxis);
                transform.position = CenterTransform.position + orbit * _PositionOffset;
                break;

            case OrbitMethod.TransformRotateAround:
                // Internally the same lines, but on the CURRENT offset and this frame's angle.
                // So the radius is wherever we happen to be, the centre is only a pivot (move
                // it and we do not follow it), and our own rotation gets spun too.
                transform.RotateAround(CenterTransform.position, _RotationAxis, deltaDeg);
                break;
        }
    }

    void OnDrawGizmos()
    {
        if (!EnableDebugDraw || !CenterTransform)
        {
            return;
        }

        // Start() has not run in edit mode, so derive the values on the fly.
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

        // Both ends of the axis are equidistant from us: that is what makes it a circle.
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.3f);
        Gizmos.DrawLine(CenterTransform.position - axisDraw, transform.position);
        Gizmos.DrawLine(CenterTransform.position + axisDraw, transform.position);
    }
}
