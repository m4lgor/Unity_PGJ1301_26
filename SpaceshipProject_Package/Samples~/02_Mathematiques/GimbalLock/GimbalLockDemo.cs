using UnityEngine;

/// <summary>
/// Gimbal lock: three nested rings (Yaw > Pitch > Roll > Payload), exactly how
/// transform.eulerAngles composes its rotations.
/// At Pitch = 90 degrees the Yaw and Roll axes merge into one line and a degree of
/// freedom is lost.
/// </summary>
[ExecuteAlways]
public class GimbalLockDemo : MonoBehaviour
{
    [Header("The three rings, outermost first")]
    [SerializeField] Transform YawRing;
    [SerializeField] Transform PitchRing;
    [SerializeField] Transform RollRing;

    [Tooltip("The object actually being oriented, at the end of the chain")]
    [SerializeField] Transform Payload;

    [Header("Euler angles, in degrees")]
    [SerializeField, Range(-180.0f, 180.0f)] float Yaw = 0.0f;

    [Tooltip("Set this to +90 or -90 to trigger the gimbal lock")]
    [SerializeField, Range(-180.0f, 180.0f)] float Pitch = 0.0f;

    [SerializeField, Range(-180.0f, 180.0f)] float Roll = 0.0f;

    [Header("Automatic demonstration")]
    [SerializeField] bool RunDemo = true;
    [SerializeField] float PhaseDuration = 4.0f;

    [SerializeField] bool DisplayGUI = true;

    const int PhaseCount = 3;

    static readonly string[] Captions =
    {
        "1. Raising Pitch to 90 degrees. The Roll axis lies down onto the Yaw axis.",
        "2. Turning YAW. The Payload rotates about this axis.",
        "3. Turning ROLL, the OTHER axis. The Payload rotates about the SAME axis: it is stuck on one single direction.",
    };

    int _Phase;
    float _PhaseTime;
    bool _CaptureAtEndOfFrame = true;
    Quaternion _PhaseStartRotation = Quaternion.identity;

    // The axis the Payload actually rotates about during the current step.
    Vector3 _MotionAxis = Vector3.zero;
    float _MotionAngle;

    GUIStyle _Panel, _Header, _Item, _Punch;

    void Update()
    {
        if (RunDemo && Application.isPlaying)
        {
            StepDemo();
        }

        ApplyAngles();
        TrackPayload();
    }

    void StepDemo()
    {
        _PhaseTime += Time.deltaTime;
        if (_PhaseTime >= PhaseDuration)
        {
            _PhaseTime = 0.0f;
            _Phase = (_Phase + 1) % PhaseCount;
            _CaptureAtEndOfFrame = true;
            _MotionAxis = Vector3.zero;
            _MotionAngle = 0.0f;
        }

        float u = Mathf.Clamp01(_PhaseTime / PhaseDuration);

        // Sweep 0 -> 90 -> 0, so each step ends where it started and there is no jump.
        float sweep = Mathf.PingPong(u * 2.0f, 1.0f) * 90.0f;

        switch (_Phase)
        {
            case 0: Pitch = u * 90.0f; Yaw = 0.0f;  Roll = 0.0f;  break;
            case 1: Pitch = 90.0f;     Yaw = sweep; Roll = 0.0f;  break;
            case 2: Pitch = 90.0f;     Yaw = 0.0f;  Roll = sweep; break;
        }
    }

    void ApplyAngles()
    {
        if (YawRing)
        {
            YawRing.localRotation = Quaternion.AngleAxis(Yaw, Vector3.up);
        }

        if (PitchRing)
        {
            PitchRing.localRotation = Quaternion.AngleAxis(Pitch, Vector3.right);
        }

        if (RollRing)
        {
            RollRing.localRotation = Quaternion.AngleAxis(Roll, Vector3.forward);
        }
    }

    /// <summary>
    /// The axis the Payload has actually rotated about since the step began.
    /// </summary>
    void TrackPayload()
    {
        if (!Payload)
        {
            return;
        }

        if (_CaptureAtEndOfFrame)
        {
            _PhaseStartRotation = Payload.rotation;
            _CaptureAtEndOfFrame = false;
        }

        Quaternion delta = Payload.rotation * Quaternion.Inverse(_PhaseStartRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180.0f)
        {
            angle = 360.0f - angle;
            axis = -axis;
        }

        // Below a few degrees the axis is just numerical noise, so keep the last good one.
        if (angle > 5.0f)
        {
            _MotionAxis = axis.normalized;
            _MotionAngle = angle;
        }
    }

    Vector3 YawAxisWorld()
    {
        Transform parent = YawRing ? YawRing.parent : null;
        return parent ? parent.rotation * Vector3.up : Vector3.up;
    }

    Vector3 RollAxisWorld()
    {
        Transform parent = RollRing ? RollRing.parent : null;
        return parent ? parent.rotation * Vector3.forward : Vector3.forward;
    }

    float AngleBetweenAxesDeg()
    {
        float cos = Mathf.Clamp(Vector3.Dot(YawAxisWorld().normalized, RollAxisWorld().normalized), -1.0f, 1.0f);
        return Mathf.Acos(cos) * Mathf.Rad2Deg;
    }

    float AxisAlignment()
    {
        return Mathf.Abs(Vector3.Dot(YawAxisWorld().normalized, RollAxisWorld().normalized));
    }

    void EnsureStyles()
    {
        if (_Panel != null)
        {
            return;
        }

        _Panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
        _Header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
        _Item = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        _Punch = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 16, fontStyle = FontStyle.Bold };
    }

    void OnGUI()
    {
        if (!DisplayGUI)
        {
            return;
        }

        EnsureStyles();

        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 440.0f, Screen.height - 20.0f));
        GUILayout.BeginVertical(_Panel);

        GUILayout.Label("Gimbal Lock", _Header);

        if (RunDemo && Application.isPlaying)
        {
            GUILayout.Label(Captions[_Phase], _Item);
            GUILayout.Space(6.0f);

            if (_MotionAxis == Vector3.zero)
            {
                GUILayout.Label("Payload motion axis: <b>-</b>", _Punch);
            }
            else
            {
                GUILayout.Label("Payload motion axis:\n<b>("
                                + _MotionAxis.x.ToString("0.00") + ", "
                                + _MotionAxis.y.ToString("0.00") + ", "
                                + _MotionAxis.z.ToString("0.00") + ")</b>", _Punch);
            }

            GUILayout.Space(6.0f);
        }

        GUILayout.Label("Yaw <b>" + Yaw.ToString("0") + "</b>   Pitch <b>" + Pitch.ToString("0")
                        + "</b>   Roll <b>" + Roll.ToString("0") + "</b>", _Item);
        GUILayout.Label("Angle between the Yaw and Roll axes: <b>"
                        + AngleBetweenAxesDeg().ToString("0.0") + " deg</b>", _Item);

        GUILayout.Space(6.0f);
        if (AxisAlignment() > 0.999f)
        {
            GUILayout.Label("<b>GIMBAL LOCK</b> - the Yaw and Roll axes are the same line. "
                            + "Only 2 degrees of freedom out of 3 are left.", _Item);
        }
        else
        {
            GUILayout.Label("The three axes are independent.", _Item);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (!YawRing || !RollRing)
        {
            return;
        }

        Vector3 centre = transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(centre, YawAxisWorld().normalized * 4.0f);
        Gizmos.DrawRay(centre, YawAxisWorld().normalized * -4.0f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(centre, RollAxisWorld().normalized * 3.0f);
        Gizmos.DrawRay(centre, RollAxisWorld().normalized * -3.0f);
    }
}
