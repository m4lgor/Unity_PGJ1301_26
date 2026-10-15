using UnityEngine;

// -----------------------
// Not for you to edit. The trigger zone of the physics lab : OnTriggerEnter / Stay / Exit.
// Under Physics.SendEvents > Physics.SendTriggerEvents.
// -----------------------

public class LabTriggerZone : MonoBehaviour
{
    public static int Inside;
    public static int Enters;
    public static int Stays;

    void OnTriggerEnter(Collider other)
    {
        ++Enters;
        ++Inside;
    }

    void OnTriggerStay(Collider other)
    {
        ++Stays;
    }

    void OnTriggerExit(Collider other)
    {
        --Inside;
    }
}
