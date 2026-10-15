using UnityEngine;

// -----------------------
// Not for you to edit. Small behaviours used by PhysicsDebuggerDemo (README_PhysicsDebugger.txt).
// They are added from code, never by hand in the scene.
// -----------------------

// A kinematic platform sliding left and right : the right way to move something that carries bodies.
public class PdMover : MonoBehaviour
{
    public float Range = 1.5f;
    public float Speed = 0.5f;

    Rigidbody m_Body;
    Vector3 m_Start;

    void Awake()
    {
        m_Body = GetComponent<Rigidbody>();
        m_Start = transform.position;
    }

    void FixedUpdate()
    {
        float offset = Mathf.Sin(Time.time * Speed * Mathf.PI * 2.0f) * Range;
        m_Body.MovePosition(m_Start + Vector3.right * offset);
    }
}

// Flies along the top of the scene and casts three queries straight down, from FixedUpdate.
// In the Physics Debugger, the Queries section draws each one.
public class PdSensor : MonoBehaviour
{
    public float MinX = -11.0f;
    public float MaxX = 13.0f;
    public float Speed = 3.0f;
    public float RayLength = 20.0f;
    public float SphereRadius = 0.4f;
    public float OverlapRadius = 1.5f;

    public string RayHit { get; private set; } = "nothing";
    public string SphereHit { get; private set; } = "nothing";
    public int OverlapCount { get; private set; }

    readonly Collider[] m_Overlaps = new Collider[16];

    void FixedUpdate()
    {
        float x = MinX + Mathf.PingPong(Time.time * Speed, MaxX - MinX);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        Vector3 origin = transform.position;
        Vector3 hitPoint = origin + Vector3.down * RayLength;

        // Default layer mask : the "Ignore Raycast" layer is skipped, triggers are hit.
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit rayHit, RayLength))
        {
            RayHit = rayHit.collider.name + " (layer " + LayerMask.LayerToName(rayHit.collider.gameObject.layer) + ")";
            hitPoint = rayHit.point;
        }
        else
        {
            RayHit = "nothing";
        }

        if (Physics.SphereCast(origin + Vector3.forward * 0.5f, SphereRadius, Vector3.down, out RaycastHit sphereHit, RayLength))
        {
            SphereHit = sphereHit.collider.name;
        }
        else
        {
            SphereHit = "nothing";
        }

        OverlapCount = Physics.OverlapSphereNonAlloc(hitPoint, OverlapRadius, m_Overlaps);

        Debug.DrawLine(origin, hitPoint, Color.yellow);
    }
}

// Reports what enters the invisible zone.
public class PdTriggerLog : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name + " entered " + name);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log(other.name + " left " + name);
    }
}
