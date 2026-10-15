using Unity.Profiling;
using UnityEngine;

// -----------------------
// Not for you to edit. A collision callback of the physics lab : OnCollisionStay, called on every fixed
// step for every pair that stays in contact. Under Physics.SendEvents > Physics.SendContactEvents.
// -----------------------

public class LabStayListener : MonoBehaviour
{
    public static int Stays;

    // How much arithmetic each call burns. A real OnCollisionStay reads the contacts, plays a sound...
    public static int WorkIterations;

    static readonly ProfilerMarker StayMarker = new ProfilerMarker("Lab.OnCollisionStay");

    void OnCollisionStay(Collision collision)
    {
        ++Stays;
        if (WorkIterations <= 0)
        {
            return;
        }

        using (StayMarker.Auto())
        {
            float x = 1.0f;
            for (int i = 0; i < WorkIterations; ++i)
            {
                x = Mathf.Sqrt(x + i);
            }

            if (x < 0.0f)
            {
                Debug.Log(x);
            }
        }
    }
}
