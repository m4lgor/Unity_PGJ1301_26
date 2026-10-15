using UnityEngine;
using UnityEngine.InputSystem;

// -----------------------
// Not for you to edit. A small physics scene built from code, to showcase the Physics Debugger
// (Window > Analysis > Physics Debugger). Everything is made of primitives.
//
//   Static ground, ghost crate     -> static colliders, and a collider the queries skip (Ignore Raycast layer)
//   Stack of boxes + sphere        -> dynamic bodies, which fall asleep
//   Moving platform + rider box    -> kinematic body, FixedJoint
//   Invisible zone                 -> trigger collider (nothing to see without the debugger)
//   Door                           -> HingeJoint with limits and a spring
//   Pendulum                       -> SpringJoint
//   Sensor flying overhead         -> Raycast, SphereCast and OverlapSphere every physics step
//
// Space : fire a ball at the door.   B : fire a ball at the stack.   R : rebuild the scene.
// -----------------------

public class PhysicsDebuggerDemo : MonoBehaviour
{
    const int IgnoreRaycastLayer = 2;

    GameObject m_Root;
    PdSensor m_Sensor;

    void Start()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(1.0f, 5.0f, -17.0f);
            camera.transform.LookAt(new Vector3(1.0f, 3.5f, 0.0f));
        }

        GameObject lightObject = new GameObject("Light");
        lightObject.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
        lightObject.AddComponent<Light>().type = LightType.Directional;

        Build();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame) FireBall(new Vector3(-6.0f, 0.4f, 1.5f));
        if (keyboard.bKey.wasPressedThisFrame) FireBall(new Vector3(-11.5f, 1.0f, 0.0f));
        if (keyboard.rKey.wasPressedThisFrame) Build();
    }

    void Build()
    {
        if (m_Root != null)
        {
            Destroy(m_Root);
        }

        m_Root = new GameObject("PhysicsDebuggerScene");
        Transform root = m_Root.transform;

        // ---- Static ----
        Shape(PrimitiveType.Cube, "Ground", root, new Vector3(1.0f, -0.5f, 0.0f), new Vector3(28.0f, 1.0f, 12.0f), new Color(0.25f, 0.27f, 0.3f));

        GameObject ghost = Shape(PrimitiveType.Cube, "GhostCrate", root, new Vector3(-4.5f, 0.5f, 0.0f), Vector3.one, new Color(0.7f, 0.7f, 0.3f));
        ghost.layer = IgnoreRaycastLayer;

        // ---- Stack : dynamic bodies ----
        for (int i = 0; i < 3; i++)
        {
            GameObject box = Shape(PrimitiveType.Cube, "StackBox" + i, root, new Vector3(-8.0f, 0.5f + i, 0.0f), Vector3.one, new Color(0.9f, 0.45f, 0.2f));
            box.AddComponent<Rigidbody>();
        }

        GameObject topSphere = Shape(PrimitiveType.Sphere, "StackSphere", root, new Vector3(-8.0f, 3.5f, 0.0f), Vector3.one * 0.8f, new Color(0.9f, 0.45f, 0.2f));
        topSphere.AddComponent<Rigidbody>();

        // ---- Kinematic platform, and a box attached to it by a FixedJoint ----
        GameObject platform = Shape(PrimitiveType.Cube, "Platform", root, new Vector3(0.5f, 0.2f, 0.0f), new Vector3(2.0f, 0.4f, 2.0f), new Color(0.3f, 0.6f, 0.9f));
        Rigidbody platformBody = platform.AddComponent<Rigidbody>();
        platformBody.isKinematic = true;
        platform.AddComponent<PdMover>();

        GameObject rider = Shape(PrimitiveType.Cube, "RiderBox", root, new Vector3(0.5f, 0.7f, 0.0f), Vector3.one * 0.6f, new Color(0.3f, 0.9f, 0.6f));
        rider.AddComponent<Rigidbody>();
        FixedJoint fixedJoint = rider.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = platformBody;

        // ---- Trigger zone : no renderer, only a collider ----
        GameObject zone = new GameObject("TriggerZone");
        zone.transform.SetParent(root);
        zone.transform.position = new Vector3(4.0f, 1.5f, 0.0f);
        zone.transform.localScale = new Vector3(2.0f, 3.0f, 4.0f);
        zone.AddComponent<BoxCollider>().isTrigger = true;
        zone.AddComponent<PdTriggerLog>();

        // ---- Door : HingeJoint, hinge at z = 0, door extends to z = 3 ----
        GameObject door = Shape(PrimitiveType.Cube, "Door", root, new Vector3(6.5f, 1.5f, 1.5f), new Vector3(0.2f, 3.0f, 3.0f), new Color(0.8f, 0.2f, 0.3f));
        door.AddComponent<Rigidbody>();
        HingeJoint hinge = door.AddComponent<HingeJoint>();
        hinge.anchor = new Vector3(0.0f, 0.0f, -0.5f);          // Local space : -0.5 x 3 (scale) = 1.5 m from the centre
        hinge.axis = Vector3.up;
        hinge.useLimits = true;
        hinge.limits = new JointLimits { min = -90.0f, max = 90.0f };
        hinge.useSpring = true;
        hinge.spring = new JointSpring { spring = 8.0f, damper = 2.0f, targetPosition = 0.0f };

        // ---- Pendulum : SpringJoint to a kinematic anchor ----
        GameObject anchor = Shape(PrimitiveType.Sphere, "PendulumAnchor", root, new Vector3(11.0f, 6.0f, 1.5f), Vector3.one * 0.3f, Color.white);
        anchor.AddComponent<Rigidbody>().isKinematic = true;

        GameObject bob = Shape(PrimitiveType.Sphere, "PendulumBob", root, new Vector3(12.5f, 4.0f, 1.5f), Vector3.one * 0.8f, new Color(0.7f, 0.4f, 0.9f));
        bob.AddComponent<Rigidbody>();
        SpringJoint spring = bob.AddComponent<SpringJoint>();
        spring.connectedBody = anchor.GetComponent<Rigidbody>();
        spring.anchor = Vector3.zero;
        spring.spring = 30.0f;
        spring.damper = 0.5f;
        spring.minDistance = 0.0f;
        spring.maxDistance = 2.0f;

        // ---- Sensor : three queries per physics step ----
        GameObject sensorObject = Shape(PrimitiveType.Sphere, "Sensor", root, new Vector3(-11.0f, 8.0f, 0.0f), Vector3.one * 0.4f, Color.yellow);
        Destroy(sensorObject.GetComponent<Collider>());
        m_Sensor = sensorObject.AddComponent<PdSensor>();
    }

    // Creates a primitive with a colour, under a parent.
    static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.transform.SetParent(parent);
        shape.transform.position = position;
        shape.transform.localScale = scale;
        shape.GetComponent<Renderer>().material.color = color;
        return shape;
    }

    void FireBall(Vector3 position)
    {
        GameObject ball = Shape(PrimitiveType.Sphere, "Ball", m_Root.transform, position, Vector3.one * 0.8f, Color.white);
        Rigidbody body = ball.AddComponent<Rigidbody>();
        body.linearVelocity = Vector3.right * 9.0f;
        Destroy(ball, 20.0f);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 600.0f, 200.0f), GUI.skin.box);
        GUILayout.Label("PHYSICS DEBUGGER DEMO   Window > Analysis > Physics Debugger");
        GUILayout.Label("Space : ball at the door      B : ball at the stack      R : rebuild");

        if (m_Sensor != null)
        {
            GUILayout.Label("Raycast hits : " + m_Sensor.RayHit);
            GUILayout.Label("SphereCast hits : " + m_Sensor.SphereHit);
            GUILayout.Label("OverlapSphere : " + m_Sensor.OverlapCount + " collider(s)");
        }

        GUILayout.EndArea();
    }
}
