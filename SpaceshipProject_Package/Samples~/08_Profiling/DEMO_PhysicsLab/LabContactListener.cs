using UnityEngine;

// -----------------------
// Not for you to edit. A collision callback of the physics lab : OnCollisionEnter / OnCollisionExit.
// It runs after the simulation, under Physics.SendEvents > Physics.SendContactEvents.
// One component per kind of callback, so that each can be switched on alone and its cost seen in the Profiler.
// -----------------------

public class LabContactListener : MonoBehaviour
{
    public static int Enters;
    public static int Exits;

    void OnCollisionEnter(Collision collision)
    {
        ++Enters;
    }

    void OnCollisionExit(Collision collision)
    {
        ++Exits;
    }
}
