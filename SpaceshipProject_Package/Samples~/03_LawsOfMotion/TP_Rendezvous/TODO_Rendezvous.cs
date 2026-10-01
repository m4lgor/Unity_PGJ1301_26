using UnityEngine;

/// <summary>
/// TP Rendezvous - park the probe on each target in turn.
///
/// Nothing slows you down out here (first law), so whatever you speed up, you have to
/// slow down yourself. The interesting question is when to turn round.
///
/// Provided by the base class: _Rigidbody and TargetPosition.
/// </summary>
public class TODO_Rendezvous : RendezvousControllerBase
{
    public float Acceleration = 4f;
    
    void FixedUpdate()
    {
    
    // ----------- TODO Students ------------
    //
    // Stop the probe on TargetPosition. Then it moves to the next one, and so on.
    // TargetPosition is updated automatically. Don't worry about it.
    //
    // You push the probe with:
    //
    //     _Rigidbody.AddForce(someDirection * someForce, ForceMode.Force);
    //     _Rigidbody.AddForce(someDirection * someAcceleration, ForceMode.Acceleration);
    //
    // Called from FixedUpdate that pushes for one physics step, so call it every
    // FixedUpdate for as long as you want to push. Stop calling it and the probe
    // carries on at constant speed because we are in space !
    //
    // You might need : _Rigidbody.linearVelocity, _Rigidbody.mass.
    //
    // Make it work however you like. There is no force limit, so feel free to
    // experiment.
    //
    // Big Hint for accurate behaviour :
    //
    //      Use the BrakingDistance which can be determine from a = F / m, I'll give you a break down of the maths
    //      behind if you're interested
    //      
    //      BrakingDistance = yourVelocity_f * yourVelocity_f / (2 * Acceleration) 
    //
    //      Acceleration is the vector that you plan to apply in the AddForce. Any value between 1 and 20 should work.
    //
    //      If the distance between your target and your object is smaller than BrakingDistance,
    //      you need to invert your Force to slow yourself down !
    //
    // --------------------------------------
    }
}
