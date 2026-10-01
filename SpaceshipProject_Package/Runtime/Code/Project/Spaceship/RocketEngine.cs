using UnityEngine;

public class RocketEngine : MonoBehaviour
{
    [SerializeField] private ForceMode _ForceMode = ForceMode.Force;
    [SerializeField] private float _MaxPower = 10.0f;

    private Rigidbody _OwnerBody;
    private bool _PropulsionEnabled = false;
    private float _ThrustPower = 0.0f;

    public float GetMaxPower()
    {
        return _MaxPower;
    }

    /// <summary>
    /// Current thrust as a 0..1 fraction of MaxPower.
    /// Returns 0 when the propulsion is disabled.
    /// Use this to drive any feedback (flame, light, sound) so what you see
    /// matches the force that is actually being applied.
    /// </summary>
    public float GetThrustRatio()
    {
        if (!_PropulsionEnabled || _MaxPower <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp01(_ThrustPower / _MaxPower);
    }

    private void Awake()
    {
        // Search starting from the parent, so the engine can be nested anywhere under the
        // spaceship (an "Engines" group, a gimbal pivot, ...) and still find the body it pushes.
        _OwnerBody = transform.parent ? transform.parent.GetComponentInParent<Rigidbody>() : null;

        if (!_OwnerBody)
        {
            Debug.LogWarning("RocketEngine - I can't find a Rigidbody in my parents. I won't be able to push anything.", this);
        }
    }

    /// <summary>
    /// Fixed Update to have a stable step for physics operations
    /// </summary>
    private void FixedUpdate()
    {
        if (!_PropulsionEnabled)
        {
            return;
        }

        if (_ThrustPower == 0.0f)
        {
            return;
        }

        if (!_OwnerBody)
        {
            return;
        }

        // Apply the force to the owner.
        var forceDirection = GetPushDirection();
        _OwnerBody.AddForceAtPosition(forceDirection * _ThrustPower, transform.position, _ForceMode);
    }

    /// <summary>
    /// Accessor to know if the propulsion is enabled or not.
    /// </summary>  
    public bool IsPropulsionEnabled()
    {
        return _PropulsionEnabled;
    }

    /// <summary>
    /// Returns the direction in which this engine applies force.
    /// This Vector3 is in world space.
    /// This Vector3 is normalized.
    /// </summary>
    public Vector3 GetPushDirection()
    {
        // Use the -transform.right direction of the GameObject as the force direction
        return -transform.right; 
    }

    /// <summary>
    /// Enable or disable the propulsion of this engine.
    /// The engine will not apply any force if disabled.
    /// </summary>
    public void EnablePropulsion(bool InEnablePropulsion)
    {
        _PropulsionEnabled = InEnablePropulsion;

        // A disabled engine must not keep pushing with the throttle it was last given.
        if (!_PropulsionEnabled)
        {
            _ThrustPower = 0.0f;
        }
    }

    /// <summary>
    /// Thrust the spaceship by applying a force at the engine's position.
    /// Value will be clamped between 0 and MaxPower.
    /// </summary>
    public void Thrust(float Power)
    {
        if(!IsPropulsionEnabled())
        {
            Debug.Log("RocketEngine - I can't push the spaceship if I am not enabled.");
            return;
        }

        if (!_OwnerBody)
        {
            Debug.Log("RocketEngine - I Can't find my spaceship'body. Cancelling Thrust.");
            return;
        }

        if(Power < 0)
        {
            Debug.LogWarning("RocketEngine - Power is negative ! I can't do this ! Clamping to 0 power");
            Power = 0;
        }

        if (Power > _MaxPower)
        {
            Debug.LogWarning("RocketEngine - Power is too high ! Using my maximum power Propulsion power !");
            Power = _MaxPower;
        }

        _ThrustPower = Power;
    }

    protected void OnDrawGizmos()
    {
        var forceDirection = GetPushDirection(); // Ensure the force direction is updated to the current forward direction
        Gizmos.color = IsPropulsionEnabled() ? UnityEngine.Color.green : UnityEngine.Color.red;

        Gizmos.DrawRay(transform.position - forceDirection, forceDirection);

        // Draw head
        Vector3 right = Quaternion.LookRotation(forceDirection) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(forceDirection) * Quaternion.Euler(0, -150, 0) * Vector3.forward;

        Gizmos.DrawRay(transform.position, right * 0.25f * forceDirection.magnitude);
        Gizmos.DrawRay(transform.position, left * 0.25f * forceDirection.magnitude);
    }
}
