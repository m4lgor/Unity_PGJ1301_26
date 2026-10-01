using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// -----------------------
// Not for you to edit. This is the rig : it walks the sphere through the doorway and
// back, forever, counts what happens, and labels the door. It never touches the hinge -
// that is your job, in TODO_DoorMotor.
//
//     Everything happens in the XY plane, like the ship : Z is frozen, nothing has gravity.
//     The wall runs along Y, with a doorway in it. The sphere goes along X.
//     The door hangs from the TOP of the doorway, and swings in the plane you see.
// -----------------------

public class DoorCourse : MonoBehaviour
{
    [Header("Rig")]
    [Tooltip("Kinematic. The wall and its doorway. It never moves")]
    public Rigidbody Frame;

    [FormerlySerializedAs("DoorPointTransform")] [FormerlySerializedAs("HingePointTransform")] [Tooltip("The top of the doorway, where the door hangs. A child of Frame")]
    public Transform FramePointTransform;

    [Tooltip("Dynamic. Floats where it is until you give it a joint")]
    public Rigidbody Door;

    [FormerlySerializedAs("Door2PointTransform")] [FormerlySerializedAs("FramePointTransform")] [FormerlySerializedAs("HingeEdgeTransform")] [Tooltip("The top end of the door, the one that turns. A child of Door")]
    public Transform DoorPointTransform;

    [Tooltip("The visitor. Dynamic")]
    public Rigidbody Sphere;

    [Header("The visitor")]
    [Tooltip("How fast the sphere walks, in metres per second")]
    public float SphereSpeed = 3.0f;

    [Tooltip("How far from the doorway it goes on each side, in metres")]
    public float ShuttleDistance = 7.0f;

    [Tooltip("How long it waits at each end, in seconds")]
    public float PauseSeconds = 2.0f;

    [Tooltip("The most it pushes with, in m/s^2. A closed door that holds stops it")]
    public float SphereAcceleration = 10.0f;

    [Header("Keys")]
    [SerializeField] Key RestartKey = Key.R;

    [Header("Debug")]
    [Tooltip("Draws both anchors of the hinge, its axis, and the line between them")]
    [SerializeField] bool DrawJointGizmos = true;

    [Tooltip("Radius of the anchor spheres, in metres")]
    [SerializeField] float GizmoRadius = 0.08f;

    int _Direction = 1;
    float _PauseLeft;
    int _LastSide;

    public int Crossings { get; private set; }
    public int Bonks { get; private set; }

    /// <summary>The middle of the doorway. A point on the wall, halfway down the gap.</summary>
    public Vector3 Doorway_WorldSpace => Frame.position;

    /// <summary>Across the wall, pointing to +X. The sphere is on one side of the wall or the other.</summary>
    public Vector3 WallNormal_WorldSpace => Frame.rotation * Vector3.right;

    public void RegisterBonk()
    {
        Bonks++;
    }

    /// <summary>Back to the first step of a run.</summary>
    public void ResetRun()
    {
        _Direction = 1;
        _PauseLeft = 0.0f;
        _LastSide = 0;
        Crossings = 0;
        Bonks = 0;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[RestartKey].wasPressedThisFrame)
        {
            // This scene is not in the build settings, so a plain LoadScene would refuse it.
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
        }
    }

    void FixedUpdate()
    {
        WalkTheSphere();
        CountCrossings();
    }

    // Pushed, not teleported : the sphere has a limited push, so a door that holds shut
    // really does stop it.
    void WalkTheSphere()
    {
        Vector3 velocity = Sphere.linearVelocity;
        Vector3 position = Sphere.position;
        float endX = Frame.position.x + _Direction * ShuttleDistance;

        float desiredSpeed;
        if (_PauseLeft > 0.0f)
        {
            _PauseLeft -= Time.fixedDeltaTime;
            desiredSpeed = 0.0f;
        }
        else
        {
            bool arrived = _Direction > 0 ? position.x >= endX - 0.1f : position.x <= endX + 0.1f;
            if (arrived)
            {
                _Direction = -_Direction;
                _PauseLeft = PauseSeconds;
                desiredSpeed = 0.0f;
            }
            else
            {
                desiredSpeed = _Direction * SphereSpeed;
            }
        }

        float accelerationX = Mathf.Clamp((desiredSpeed - velocity.x) / 0.2f, -SphereAcceleration, SphereAcceleration);

        // And back onto its line, if something knocked it off.
        float offLine = position.y - Frame.position.y;
        float accelerationY = Mathf.Clamp(-offLine * 10.0f - velocity.y * 5.0f, -SphereAcceleration, SphereAcceleration);

        Sphere.AddForce(new Vector3(accelerationX, accelerationY, 0.0f) * Sphere.mass);
    }

    void CountCrossings()
    {
        float x = Sphere.position.x - Frame.position.x;
        int side = x > 0.6f ? 1 : x < -0.6f ? -1 : 0;
        if (side == 0)
        {
            return;
        }

        if (_LastSide != 0 && side != _LastSide)
        {
            Crossings++;
        }
        _LastSide = side;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;

        HingeJoint hinge = Door.GetComponent<HingeJoint>();
        string doorLine = hinge != null ? "door angle " + hinge.angle.ToString("0") + " deg" : "no hinge yet - the door is loose";

        GUI.Label(new Rect(10.0f, 10.0f, 700.0f, 26.0f), "crossings " + Crossings + "     bonks " + Bonks, style);
        GUI.Label(new Rect(10.0f, 36.0f, 700.0f, 26.0f), doorLine, style);
        GUI.Label(new Rect(10.0f, 62.0f, 700.0f, 26.0f), "R : restart", style);
    }

    void OnDrawGizmos()
    {
        if (DrawJointGizmos && Door != null)
        {
            JointGizmos.Draw(Door.GetComponent<Joint>(), GizmoRadius);
        }
    }
}
