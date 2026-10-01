using UnityEngine;
using UnityEngine.InputSystem;

public class LockedCameraOrtho : MonoBehaviour
{
    [Header("Smoothing")]
    public float _SmoothSpeed = 5f;

    [SerializeField] float _Size = 5;


    private Transform _target1;
    private Transform _target2;
    private Vector3 _offset = new Vector3(0f, 0f, -10f); // Default 2D-style offset

    void FixedUpdate()
    {
        UpdateTargets();

        Vector3 desiredPosition;
        Vector3 deltaTargets = Vector3.zero;
        float orthoSize = _Size;
        if ( _target1 != null && _target2 != null)
        {
            // Midpoint between the two targets
            Vector3 midpoint = (_target1.position + _target2.position) / 2f;
            deltaTargets = _target2.position - _target1.position;
            orthoSize = Mathf.Max(_Size, deltaTargets.magnitude);

            desiredPosition = midpoint;
        }
        else if(_target1 != null)
        {
            desiredPosition = _target1.position;
        }
        else if(_target2 != null)
        {
            desiredPosition = _target2.position;
        }
        else
        {
            Debug.LogError("No targets found for LockedCameraOrtho.");
            desiredPosition = Vector3.zero;
            return;
        }

        // Desired position based on target + offset
        desiredPosition += _offset;

        // Smoothly interpolate from current to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, _SmoothSpeed * Time.deltaTime);

        // Always look forward along the Z axis
        transform.rotation = Quaternion.LookRotation(Vector3.forward);

        var cameraComponent = GetComponent<Camera>();
        if(cameraComponent)
        {
            cameraComponent.orthographicSize = Mathf.Lerp(cameraComponent.orthographicSize, orthoSize, Time.deltaTime);
        }
    }

    void UpdateTargets()
    {
        var targetObject1 = GameObject.FindFirstObjectByType<SpaceshipControllerBase>();
        if(!targetObject1)
        {
            Debug.LogError("Can't find the Spaceship controller in this scene. Add one that inherits from SpaceshipControllerBase");
        }
        else
        {
            _target1 = targetObject1.transform;
        }

        var targetObject2 = GameObject.FindFirstObjectByType<GoalComponentBase>();
        if (!targetObject2)
        {
            Debug.LogError("Can't find a GoalComponent in this scene. Contact your favorite teacher.");
        }
        else
        {
            _target2 = targetObject2.transform;
        }
    }
}
