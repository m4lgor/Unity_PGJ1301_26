using UnityEngine;

public class GoalComponent : GoalComponentBase
{
    protected void OnTriggerEnter(Collider other)
    {
        if (IsTheSpaceship(other))
        {
            EnterZone();
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (IsTheSpaceship(other))
        {
            ExitZone();
        }
    }

    protected override void OnCompleted()
    {
        _spawner.OnGoalReached();
        _spaceship.OnGoalReached(this);
    }

    /// <summary>
    /// Is this collider part of the spaceship the spawner told us about?
    ///
    /// Compares GAME OBJECTS and not controller components on purpose. Comparing the
    /// components breaks as soon as the ship carries more than one SpaceshipControllerBase
    /// (a student controller plus a solution controller, say): GetComponent returns the
    /// first one on the object, the spawner's FindObjectsByType may well have picked the
    /// other, and the two references then never match. Silent, and order dependent.
    ///
    /// IsChildOf also covers the collider being on a child of the ship rather than on
    /// its root, and it is true for the root itself.
    /// </summary>
    bool IsTheSpaceship(Collider other)
    {
        if (_spaceship == null)
        {
            return false;
        }

        return other.transform.IsChildOf(_spaceship.transform);
    }
}
