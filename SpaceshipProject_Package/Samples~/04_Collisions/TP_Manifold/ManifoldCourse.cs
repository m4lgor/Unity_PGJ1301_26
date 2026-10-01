using UnityEngine;
using UnityEngine.InputSystem;

// -----------------------
// This file is not for you to edit. It only launches the pairs and puts them back when
// you press R, because a Rigidbody has no "starting velocity" field in the inspector.
// It never detects anything and it never resolves anything. That part is yours.
// -----------------------

public class ManifoldCourse : MonoBehaviour
{
    [System.Serializable]
    public class Pair
    {
        public string Label;

        public Rigidbody A;
        public Rigidbody B;

        [Tooltip("Velocity given to A at the start of a run, in m/s")]
        public Vector3 VelocityA;

        [Tooltip("Velocity given to B at the start of a run, in m/s")]
        public Vector3 VelocityB;
    }

    [SerializeField] Pair[] Pairs;

    [SerializeField] Key RestartKey = Key.R;

    [Tooltip("The run restarts by itself after this many seconds. Zero never does")]
    [SerializeField] float AutoRestartAfter = 8.0f;

    float _RunTime;

    // Where the scene author left everything, so a restart is exact.
    Vector3[] _StartA;
    Vector3[] _StartB;

    void Awake()
    {
        if (Pairs == null)
        {
            return;
        }

        _StartA = new Vector3[Pairs.Length];
        _StartB = new Vector3[Pairs.Length];

        for (int i = 0; i < Pairs.Length; i++)
        {
            if (Pairs[i].A != null)
            {
                _StartA[i] = Pairs[i].A.transform.position;
            }

            if (Pairs[i].B != null)
            {
                _StartB[i] = Pairs[i].B.transform.position;
            }
        }
    }

    void Start()
    {
        Launch();
    }

    [ContextMenu("Launch")]
    public void Launch()
    {
        _RunTime = 0.0f;

        for (int i = 0; i < Pairs.Length; i++)
        {
            Place(Pairs[i].A, _StartA[i], Pairs[i].VelocityA);
            Place(Pairs[i].B, _StartB[i], Pairs[i].VelocityB);
        }
    }

    static void Place(Rigidbody body, Vector3 position, Vector3 velocity)
    {
        if (body == null)
        {
            return;
        }

        body.position = position;
        body.transform.position = position;

        // A kinematic body is one of the sandbox spheres: it is there to be dragged
        // around, not launched, and assigning a velocity to it only earns a warning.
        if (body.isKinematic)
        {
            return;
        }

        body.linearVelocity = velocity;
        body.angularVelocity = Vector3.zero;
    }

    void Update()
    {
        _RunTime += Time.deltaTime;

        Keyboard keyboard = Keyboard.current;
        bool restartAsked = keyboard != null && keyboard[RestartKey].wasPressedThisFrame;

        if (restartAsked || (AutoRestartAfter > 0.0f && _RunTime >= AutoRestartAfter))
        {
            Launch();
        }
    }
}
