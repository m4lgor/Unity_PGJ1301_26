using UnityEngine;

// A side view : follows the hero along the level and never turns.
public class MiniCamera : MonoBehaviour
{
    public Transform Target;
    public Vector3 Offset = new Vector3(0.0f, 3.0f, -14.0f);
    public float Smooth = 6.0f;

    void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        transform.position = Vector3.Lerp(transform.position, Target.position + Offset, Smooth * Time.deltaTime);
    }
}
