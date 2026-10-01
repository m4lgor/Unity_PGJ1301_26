using UnityEngine;

/// <summary>
/// YOUR Spaceship controller: the only file you have to write for the project, and it is
/// already attached to the Spaceship. It grows level after level, do not start over each time.
///
/// Rule: move the ship ONLY through RocketEngine.Thrust(float Power), never through
/// transform or AddForce.
///
/// Provided by the base class: _RocketEngines, TargetPosition, _Rigidbody, DisableAllEngines().
/// On a RocketEngine: GetPushDirection(), GetMaxPower(), EnablePropulsion(bool), Thrust(float).
///
/// TargetPosition is filled in for you on Levels 00, 01 and 02 (the Goal is auto-found there);
/// from Level 03 on it is not, so setting it becomes your job.
///
/// DO NOT DECLARE AN Update(): it would hide the base class one (which is not virtual),
/// TargetPosition would stop being updated, and no error would be raised. Write your code
/// in FixedUpdate() below.
/// </summary>
public class MyCustomSpaceshipController : SpaceshipControllerBase
{
    void FixedUpdate()
    {
        // Write your Update code here :)
    }

    /// <summary>
    /// Called when the Goal is reached; a new one then appears somewhere else.
    /// From Level 03 on, this is where you restart your detection.
    /// </summary>
    public override void OnGoalReached(GoalComponent goalComponent)
    {
        // ----------- TODO Students (Level 03+) ------------
        // This sections important, as you will need to find the GoalComponent yourself from now on (Level03+)
        // This method is called every time a GoalComponent is Reached.
        // --------------------------------------------------
    }
}
