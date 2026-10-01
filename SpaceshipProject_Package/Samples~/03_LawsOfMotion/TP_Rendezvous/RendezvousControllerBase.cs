using UnityEngine;

// -----------------------
// This file is not for you to edit. Inherit from it: that is what TODO_Rendezvous does.
// All it does is hand you the two things you need and stay out of the way.
// -----------------------

public class RendezvousControllerBase : MonoBehaviour
{
    protected Rigidbody _Rigidbody;

    RendezvousCourse _Course;

    /// <summary>
    /// Where the probe has to stop. The course moves it on once you have parked there.
    /// </summary>
    public Vector3 TargetPosition
    {
        get { return _Course != null ? _Course.CurrentTargetPosition : transform.position; }
    }

    protected virtual void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _Course = FindFirstObjectByType<RendezvousCourse>();

        if (_Rigidbody == null)
        {
            Debug.LogError("RendezvousControllerBase - no Rigidbody on the probe.", this);
        }

        if (_Course == null)
        {
            Debug.LogError("RendezvousControllerBase - no RendezvousCourse in the scene.", this);
        }
    }
}
