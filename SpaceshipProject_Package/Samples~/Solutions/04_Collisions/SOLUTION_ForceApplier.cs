using UnityEngine;

/// <summary>
/// SOLUTION - the finished ForceApplier, with the three improvements in.
///
/// Drives all four stations of TP_ForceApplier from one component:
///
///   1  Impulse, once             Mode Impulse, ApplyOnStart
///   2  Force, every step         Mode Force,   ApplyEveryStep
///   3  Impulse at point, once    as 1, plus UseAnchor and LocalAnchor (0.5, 0.5, 0)
///   4  Force at point, step      as 2, plus the same anchor
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SOLUTION_ForceApplier : MonoBehaviour
{
    public Vector3 Force = new Vector3(0.0f, 100.0f, 0.0f);

    [Tooltip("Impulse = the whole push at once, like a hit. Force = spread over time")]
    public ForceMode Mode = ForceMode.Impulse;

    [Tooltip("Off, the push goes through the centre of mass and cannot spin the body")]
    public bool UseAnchor = false;

    [Tooltip("Where the push is applied, in the object's own space. " +
             "A Unity cube runs from -0.5 to +0.5, so its corner is (0.5, 0.5, 0)")]
    public Vector3 LocalAnchor = new Vector3(0.5f, 0.5f, 0.0f);

    public bool ApplyOnStart = false;

    [Tooltip("Applies it again every physics step")]
    public bool ApplyEveryStep = false;

    Rigidbody _Rigidbody;

    Vector3 _StartPosition;
    Quaternion _StartRotation;

    /// <summary>
    /// The world point the push is applied at. AddForceAtPosition wants world space, and
    /// TransformPoint is what turns the authored local point into it - rotation, scale
    /// and position included, so the anchor stays on its corner as the body moves.
    /// </summary>
    public Vector3 WorldAnchor
    {
        get { return transform.TransformPoint(UseAnchor ? LocalAnchor : Vector3.zero); }
    }

    void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();

        _StartPosition = transform.position;
        _StartRotation = transform.rotation;
    }

    void Start()
    {
        if (ApplyOnStart)
        {
            Apply();
        }
    }

    void FixedUpdate()
    {
        // FixedUpdate, not Update: this is the step the physics engine listens on.
        if (ApplyEveryStep)
        {
            Apply();
        }
    }

    [ContextMenu("Apply Force")]
    public void Apply()
    {
        if (UseAnchor)
        {
            // AddForceAtPosition ADDS the spin, it does not trade the push for it:
            //
            //     AddForce(Force)  +  AddTorque((anchor - centreOfMass) x Force)
            //
            // So this body travels exactly as fast as a centred one and turns as well.
            // Momentum does not care where a force is applied, only angular momentum
            // does. For a spin with no travel you need a couple - two opposite forces
            // at different points - or AddTorque.
            _Rigidbody.AddForceAtPosition(Force, WorldAnchor, Mode);
        }
        else
        {
            _Rigidbody.AddForce(Force, Mode);
        }
    }

    /// <summary>
    /// Back to the start, at rest, so a push can be fired again without leaving play mode.
    /// Not called Reset: that name is a Unity editor message and would fire on its own.
    /// </summary>
    [ContextMenu("Reset Body")]
    public void ResetBody()
    {
        _Rigidbody.linearVelocity = Vector3.zero;
        _Rigidbody.angularVelocity = Vector3.zero;

        transform.SetPositionAndRotation(_StartPosition, _StartRotation);
    }

    void OnDrawGizmos()
    {
        if (Force == Vector3.zero)
        {
            return;
        }

        Gizmos.color = Color.red;
        DrawUtils.DrawArrow(WorldAnchor, Force * 0.05f);

        // The lever arm, centre of mass to anchor. No gap, no spin - and only the part
        // of it across the force counts, which is why the height of the anchor makes no
        // difference to a straight-up push.
        if (UseAnchor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, WorldAnchor);
            Gizmos.DrawWireSphere(WorldAnchor, 0.08f);
        }
    }
}
