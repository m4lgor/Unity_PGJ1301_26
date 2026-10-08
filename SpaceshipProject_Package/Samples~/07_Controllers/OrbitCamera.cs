using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Not for you to edit. An over-the-shoulder camera : the mouse turns it around the hero
/// (left and right) and tilts it (up and down), and a cross marks the middle of the screen -
/// what the hero will shoot at.
///
/// The camera sits behind the hero and a little to its right, and is pulled in when something
/// stands between it and the hero. The hero reads the camera's yaw :
///     Camera.main.transform.eulerAngles.y
///
/// The cursor is locked to the window while this plays. Escape frees it, a click locks it again.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    public Transform Target;
    public Vector3 PivotOffset = new Vector3(0.0f, 0.8f, 0.0f);
    public float Distance = 4.0f;
    public float ShoulderOffset = 0.6f;
    public float Sensitivity_DegreesPerPixel = 0.1f;
    public float MinPitch_Degrees = -35.0f;
    public float MaxPitch_Degrees = 60.0f;

    // Pitch : positive looks down.
    public float Yaw_Degrees;
    public float Pitch_Degrees = 10.0f;

    void OnEnable()
    {
        Lock(true);
    }

    void OnDisable()
    {
        Lock(false);
    }

    static void Lock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Lock(false);
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Lock(true);
        }

        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            Yaw_Degrees += delta.x * Sensitivity_DegreesPerPixel;
            Pitch_Degrees = Mathf.Clamp(Pitch_Degrees - delta.y * Sensitivity_DegreesPerPixel, MinPitch_Degrees, MaxPitch_Degrees);
        }

        Apply();
    }

    // Puts the camera where the yaw and the pitch say. Public so that a check can place it without a mouse.
    public void Apply()
    {
        Vector3 pivot = Target.position + PivotOffset;
        Quaternion rotation = Quaternion.Euler(Pitch_Degrees, Yaw_Degrees, 0.0f);

        // Behind the pivot, and to its right.
        Vector3 toCamera = rotation * new Vector3(ShoulderOffset, 0.0f, -Distance);
        float distance = toCamera.magnitude;
        Vector3 direction = toCamera / distance;

        // Something in the way : come closer. The hero is on the Ignore Raycast layer, so it is not "something".
        if (Physics.SphereCast(pivot, 0.2f, direction, out RaycastHit hit, distance))
        {
            distance = hit.distance;
        }

        transform.SetPositionAndRotation(pivot + direction * distance, rotation);
    }

    void OnGUI()
    {
        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;

        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x - 1.0f, y - 9.0f, 2.0f, 18.0f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 9.0f, y - 1.0f, 18.0f, 2.0f), Texture2D.whiteTexture);
    }
}
