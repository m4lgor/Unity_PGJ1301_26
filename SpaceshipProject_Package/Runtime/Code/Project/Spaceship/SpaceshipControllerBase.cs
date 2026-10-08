using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// -----------------------
// This file is not for you to Edit. You should inherit from it, and add your component to the Spaceship in the scene.
// The restriction is that you cannot influence the position or apply any forces to this object in your inherited class.
// It must be done through the RocketEngine.Thrust(float Power)
// -----------------------
// TargetPosition is the position that you should aim to reach with your Spaceship.
// It is set for you while the scene's Goal is flagged auto-found (Levels 00 to 04);
// from Level 05 on it is not, so your controller sets TargetPosition itself.
// -----------------------

public class SpaceshipControllerBase : MonoBehaviour
{
    protected List<RocketEngine> _RocketEngines = new List<RocketEngine>();
    protected Rigidbody _Rigidbody; // Cache Rigidbody component

    private void Awake()
    {
        // Cache the Rigidbody component
        _Rigidbody = GetComponent<Rigidbody>();

        // Cache all RocketEngine components in children
        _RocketEngines.AddRange(GetComponentsInChildren<RocketEngine>());
        
        // Initialize target position to the spaceship's current position
        TargetPosition = transform.position; 
    }

    protected void Update()
    {
        UpdateTarget();
    }

    /// <summary>
    /// Updates the target position by finding the first GoalComponent in the scene.
    /// </summary>
    void UpdateTarget()
    {
        var goalComponent = FindFirstObjectByType<GoalComponent>();
        if (!goalComponent)
        {
            return;
        }

        if(goalComponent.IsAutoFound)
        {
            TargetPosition = goalComponent.transform.position;
        }
    }

    /// <summary>
    /// The position the spaceship should aim to reach. Set automatically while the scene's Goal
    /// is flagged auto-found (Levels 00 to 04); from Level 05 on your controller sets it.
    /// </summary>
    public Vector3 TargetPosition { get; set; } = Vector3.zero;

    /// <summary>
    /// Override and return true if your controller sets TargetPosition itself and does
    /// NOT want the auto-found Goal to do it. This only silences the start-up warning
    /// about hiding Update(); it changes no behaviour.
    /// </summary>
    public virtual bool ManagesOwnTarget
    {
        get { return false; }
    }

    /// <summary>
    /// Utility function to disable all engines.
    /// </summary>
    protected void DisableAllEngines()
    {
        foreach (var rocketEngine in _RocketEngines)
        {
            if (rocketEngine)
            {
                rocketEngine.EnablePropulsion(false);
            }
        }
    }

    /// <summary>
    /// Event called when a goal is reached by this spaceship.
    /// </summary>
    /// <param name="goalComponent"></param>
    public virtual void OnGoalReached(GoalComponent goalComponent)
    {
        // You can override this method in your inherited class to handle goal reached events.
        // This base implementation does nothing.
    }
    
    /// <summary>
    /// Event called when a payload goal is reached by a payload.
    /// </summary>
    /// <param name="goalComponent"></param>
    public virtual void OnPayloadGoalReached(PayloadGoalComponent payloadGoalComponent)
    {
        // You can override this method in your inherited class to handle payload goal reached events.
        // This base implementation does nothing.
    }

    /// <summary>
    /// Event called when a goal is spawned and ready in the scene
    /// </summary>
    public virtual void OnGoalSpawned()
    {
        // Override this to know when a Goal is Spawned and ready
    }
    
    /// <summary>
    /// Event called when a goal is destroyed. It will still be valid until the end of the frame.
    /// </summary>
    public virtual void OnGoalDestroyed()
    {
        // Override this to know when a Goal is Destroyed.
    }

    /// <summary>
    /// Level 06. The solar wind announces a gust a few seconds before it blows. The direction
    /// is the one the ship will be PUSHED toward, and it does not change during the gust.
    /// Anything that has to be in place before the wind arrives - a tether, for instance -
    /// can be set up here.
    /// </summary>
    /// <param name="direction">Unit vector, on the Z = 0 plane : where the wind pushes.</param>
    /// <param name="force">The force the gust will apply to the ship, in newtons.</param>
    /// <param name="secondsUntilGust">How long until it starts blowing.</param>
    public virtual void OnGustWarning(Vector3 direction, float force, float secondsUntilGust)
    {
        // Override this to know a gust is coming.
    }

    /// <summary>
    /// Level 06. The gust has started blowing.
    /// </summary>
    public virtual void OnGustStarted()
    {
        // Override this to know a gust has started.
    }

    /// <summary>
    /// Level 06. The gust is over.
    /// </summary>
    public virtual void OnGustEnded()
    {
        // Override this to know a gust is over.
    }

    /// <summary>
    /// Final level. The ship hit a wall or a star hard enough, and was put back at the last
    /// checkpoint, at rest. Whatever you were holding on to (a tether, a plan) is gone : the
    /// level removed the joints. Start again from here.
    /// </summary>
    /// <param name="checkpoint">Where the ship is now.</param>
    public virtual void OnSentBackToCheckpoint(Vector3 checkpoint)
    {
        // Override this to forget what no longer holds.
    }

    protected void OnDrawGizmos()
    {
        if (!GlobalSettings.Instance)
            return;

        Gizmos.color = Color.white;

        Gizmos.DrawRay(TargetPosition - GlobalSettings.Instance.WorldRight, GlobalSettings.Instance.WorldRight * 2.0f);
        Gizmos.DrawRay(TargetPosition - GlobalSettings.Instance.WorldUp, GlobalSettings.Instance.WorldUp * 2.0f);
    }
}
