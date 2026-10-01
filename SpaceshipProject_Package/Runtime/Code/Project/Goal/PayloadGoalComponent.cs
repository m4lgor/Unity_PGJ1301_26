using UnityEngine;

public class PayloadGoalComponent : GoalComponentBase
{
    protected void OnTriggerEnter(Collider other)
    {
        if (IsThePayload(other))
        {
            EnterZone();
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (IsThePayload(other))
        {
            ExitZone();
        }
    }

    protected override void OnCompleted()
    {
        _spawner.OnGoalReached();
        _spaceship.OnPayloadGoalReached(this);
    }

    /// <summary>
    /// GetComponentInParent rather than GetComponent, so a payload whose collider sits on
    /// a child object is still recognised.
    /// </summary>
    bool IsThePayload(Collider other)
    {
        return other.GetComponentInParent<PayloadComponent>() != null;
    }
}
