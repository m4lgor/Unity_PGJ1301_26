using UnityEngine;

/// <summary>
/// EXAMPLE - pushes the Rigidbody it sits on. That is all it does.
///
/// Useful because you can fire a force BY HAND while the game runs: right click the
/// component header in the inspector and pick "Apply Force". No test code, no waiting
/// for a bug to happen on its own.
///
/// Write your own version. You will be using it across the course.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EXAMPLE_ForceApplier : MonoBehaviour
{
    public Vector3 Force = new Vector3(0.0f, 100.0f, 0.0f);

    [Tooltip("Impulse = the whole push at once, like a hit. Force = spread over time")]
    public ForceMode Mode = ForceMode.Impulse;

    public bool ApplyOnStart = false;

    Rigidbody _Rigidbody;

    void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Todo
    }

    [ContextMenu("Apply Force")]
    public void Apply()
    {
        // Todo
    }

    void FixedUpdate()
    {
        // Todo
    }
}

// =====================================================================================
// MAKE IT YOURS - three things to add
// =====================================================================================
//
// 1. AddForce/Impulse
//
//      Start/FixedUpdate
//
// 2. Add Force OFF CENTRE
//    Add a Vector3 LocalAnchor, and push there instead:
//
//        _Rigidbody.AddForceAtPosition(Force, transform.TransformPoint(LocalAnchor), Mode);
//
//    In Local Space ?
//
// =====================================================================================
