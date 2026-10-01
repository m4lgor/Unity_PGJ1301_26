using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [SerializeField] private float _RotationSpeed = 100.0f; // Degrees per second
    [SerializeField] private bool _DrawDebug = false; // Degrees per second
    
    // Update is called once per frame
    void Update()
    {
        var rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0.0f, 0.0f, _RotationSpeed * Time.deltaTime));
        }
        else 
        {
            transform.Rotate(new Vector3(0.0f, 0.0f, 1.0f) * _RotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_DrawDebug)
        {
            return;
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 100.0f);
    }
}
