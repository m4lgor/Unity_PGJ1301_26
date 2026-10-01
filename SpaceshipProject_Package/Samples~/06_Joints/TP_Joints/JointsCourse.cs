using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// -----------------------
// Not for you to edit. This is the rig : it sways the three platforms, and labels each
// station with what hangs from it. It never creates a joint - that is your job.
// -----------------------

public class JointsCourse : MonoBehaviour
{
    [System.Serializable]
    public class Station
    {
        public string Label;

        [Tooltip("Kinematic. Sways left and right, and does not care what hangs from it")]
        public Rigidbody Platform;

        [Tooltip("Where the pyramid is meant to hang from. A child of Platform")]
        public Transform HangPointTransform;

        [Tooltip("Dynamic. Falls with gravity until you give it a joint")]
        public Rigidbody Pyramid;

        [Tooltip("The apex of the pyramid. A child of Pyramid, with no scale anywhere")]
        public Transform TipTransform;

        [HideInInspector] public Vector3 PlatformStartPosition_WorldSpace;
        [HideInInspector] public bool HadJoint;
    }

    public Station Distance;
    public Station Hinge;
    public Station Fixed;

    [Tooltip("How far the platforms sway to each side, in metres. Zero keeps them still")]
    public float SwayAmplitude = 1.5f;

    [Tooltip("Seconds for one full sway")]
    public float SwayPeriod = 4.0f;

    [SerializeField] Key RestartKey = Key.R;

    [Tooltip("Draws both anchors of every joint, and the line between them")]
    [SerializeField] bool DrawJointGizmos = true;

    [Tooltip("Radius of the anchor spheres, in metres")]
    [SerializeField] float GizmoRadius = 0.08f;

    Station[] _All;

    public Station[] All => _All ??= new[] { Distance, Hinge, Fixed };

    public Vector3 SwayOffset_WorldSpace(float time)
    {
        return Vector3.right * (SwayAmplitude * Mathf.Sin(2.0f * Mathf.PI * time / SwayPeriod));
    }

    void Awake()
    {
        foreach (Station station in All)
        {
            station.PlatformStartPosition_WorldSpace = station.Platform.position;
        }
    }

    void FixedUpdate()
    {
        // A kinematic body moves through MovePosition, not through the transform : the
        // engine then knows its velocity, and hands it to whatever hangs from it.
        foreach (Station station in All)
        {
            station.Platform.MovePosition(station.PlatformStartPosition_WorldSpace + SwayOffset_WorldSpace(Time.fixedTime));
        }
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

    void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;

        foreach (Station station in All)
        {
            Joint joint = FindJoint(station);

            string status;
            if (joint != null)
            {
                station.HadJoint = true;
                status = joint.GetType().Name + "  " + joint.currentForce.magnitude.ToString("0.0") + " N";
            }
            else
            {
                status = station.HadJoint ? "joint broke !" : "no joint yet";
            }

            Vector3 screen = cam.WorldToScreenPoint(station.PlatformStartPosition_WorldSpace + Vector3.up * 0.7f);
            if (screen.z < 0.0f)
            {
                continue;
            }

            GUI.Label(new Rect(screen.x - 150.0f, Screen.height - screen.y - 20.0f, 300.0f, 40.0f),
                station.Label + "\n" + status, style);
        }

        GUI.Label(new Rect(10.0f, 10.0f, 400.0f, 24.0f), "R : restart", style);
    }

    static Joint FindJoint(Station station)
    {
        Joint joint = station.Pyramid.GetComponent<Joint>();
        return joint != null ? joint : station.Platform.GetComponent<Joint>();
    }

    // What each joint says about itself : see JointGizmos. Nothing is drawn until the
    // joint exists, so this never shows where an anchor SHOULD have been.
    void OnDrawGizmos()
    {
        if (!DrawJointGizmos)
        {
            return;
        }

        // Not All : that one is cached, and the fields can change while editing.
        foreach (Station station in new[] { Distance, Hinge, Fixed })
        {
            if (station == null || station.Pyramid == null || station.Platform == null)
            {
                continue;
            }

            JointGizmos.Draw(FindJoint(station), GizmoRadius);
        }
    }
}
