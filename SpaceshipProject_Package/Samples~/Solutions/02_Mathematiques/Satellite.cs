using UnityEngine;

/// <summary>
/// SOLUTION - The Satellite sits ON the perpendicular to the (Sun, Earth, Moon) plane, at a
/// fixed distance from the Earth.
///
/// Three points define a plane, and the cross product of two of its edges is its normal. Here
/// that normal IS the Satellite's position, so swapping the two operands flips it to the other
/// side. The Earth and the Moon keep moving, so the plane keeps tilting and the Satellite keeps
/// moving with it - no accumulated angle needed, the geometry alone drives it.
/// </summary>
public class Satellite : MonoBehaviour
{
    [Tooltip("Start of the first vector (Sun)")]
    [SerializeField] Transform Transform0;

    [Tooltip("Common point of the two vectors, and the point we keep a fixed distance from (Earth)")]
    [SerializeField] Transform CenterTransform;

    [Tooltip("End of the second vector (Moon)")]
    [SerializeField] Transform Transform1;

    [Tooltip("How far we stay from CenterTransform")]
    [SerializeField] float FixedDistance = 5.0f;

    [Header("Bonus - colour by the phase of the Lune")]
    [Tooltip("Lune is on the Soleil side of Terre (dot >= 0)")]
    [SerializeField] Color FacingColor = Color.green;

    [Tooltip("Lune is on the far side of Terre (dot < 0)")]
    [SerializeField] Color AwayColor = Color.red;

    [Tooltip("Enable debug draws")]
    [SerializeField] bool EnableDebugDraw = false;

    // Held for the frames where the three bodies line up: there is no plane then, and no normal.
    Vector3 _LastNormal = Vector3.zero;

    Renderer _Renderer;

    void Start()
    {
        if (!Transform0 || !CenterTransform || !Transform1)
        {
            Debug.LogError("Satellite - Transform0, CenterTransform and Transform1 are all required.", this);
            enabled = false;
            return;
        }

        _Renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        Vector3 normal;
        if (TryComputePlaneNormal(out normal))
        {
            _LastNormal = normal;
        }
        else
        {
            // Aligned right now: hold the last valid normal rather than snapping somewhere.
            normal = _LastNormal;
            if (normal == Vector3.zero)
            {
                return; // still aligned since the very first frame
            }
        }

        // Only the POSITION comes from the cross product; the rotation is deliberately never
        // touched, and the bonus below depends on where Lune is, not on how we are turned.
        transform.position = CenterTransform.position + normal * FixedDistance;

        UpdateColor();
    }

    /// <summary>
    /// The vector perpendicular to both edges of the plane, i.e. a cross product. Both edges are
    /// written head-minus-tail and chained the same way (Soleil -> Terre, then Terre -> Lune) so
    /// the side we end up on is deliberate: the cross product is anti-commutative.
    /// </summary>
    bool TryComputePlaneNormal(out Vector3 outNormal)
    {
        outNormal = Vector3.zero;

        if (!Transform0 || !CenterTransform || !Transform1)
        {
            return false;
        }

        Vector3 soleilVersTerre = CenterTransform.position - Transform0.position;
        Vector3 terreVersLune = Transform1.position - CenterTransform.position;

        Vector3 perpendicular = Vector3.Cross(soleilVersTerre, terreVersLune);

        // |a x b| = |a| |b| sin(angle), so the length collapses to zero exactly when the two
        // edges are parallel - the "three points are aligned" case, no dot product needed.
        float length = perpendicular.magnitude;
        if (length < 0.0001f)
        {
            return false;
        }

        outNormal = perpendicular / length;
        return true;
    }

    /// <summary>
    /// Bonus. The dot product of two unit vectors is the cosine of the angle between them, so its
    /// SIGN alone says which side of Terre the Lune is on relative to Soleil: the phase.
    ///
    /// Both vectors start FROM Terre, so the dot compares the two directions as seen from there:
    /// positive means Lune is on the same side as Soleil.
    ///
    /// Note we do NOT compare our own normal with Soleil: that normal is perpendicular to
    /// Soleil->Terre by construction, so its dot never changes sign.
    /// </summary>
    void UpdateColor()
    {
        if (!_Renderer)
        {
            return;
        }

        Vector3 terreVersSoleil = Transform0.position - CenterTransform.position;
        Vector3 terreVersLune = Transform1.position - CenterTransform.position;

        float soleilLength = terreVersSoleil.magnitude;
        float luneLength = terreVersLune.magnitude;
        if (soleilLength < 0.0001f || luneLength < 0.0001f)
        {
            return;
        }

        float cosAngle = Vector3.Dot(terreVersSoleil / soleilLength, terreVersLune / luneLength);
        _Renderer.material.color = cosAngle >= 0.0f ? FacingColor : AwayColor;
    }

    void OnDrawGizmos()
    {
        if (!EnableDebugDraw || !Transform0 || !CenterTransform || !Transform1)
        {
            return;
        }

        // Recomputed here so the gizmos also work in edit mode, where Update has not run.
        if (!TryComputePlaneNormal(out Vector3 normal))
        {
            return;
        }

        // The two edges the normal is perpendicular to.
        Gizmos.color = new Color(0.0f, 0.0f, 0.0f, 0.4f);
        Gizmos.DrawLine(Transform0.position, CenterTransform.position);
        Gizmos.DrawLine(CenterTransform.position, Transform1.position);

        Gizmos.color = Color.black;
        Gizmos.DrawLine(CenterTransform.position, CenterTransform.position + normal * FixedDistance);

        // Same length on the other side, to show the sign is a choice.
        Gizmos.color = new Color(0.0f, 0.0f, 0.0f, 0.25f);
        Gizmos.DrawLine(CenterTransform.position, CenterTransform.position - normal * FixedDistance);
    }
}
