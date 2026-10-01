using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MouseInteractionController : MonoBehaviour
{
    public enum Mode { Push, Constraint }
    public Mode currentMode = Mode.Push;

    public float _PushForce = 10f;
    public float _SpringStrength = 50f;
    public float _Damping = 5f;
    public float _RayDistance = 100f;

    private Camera _cam;
    private Rigidbody _selectedRigidbody;
    private SpringJoint _springJoint;
    private LineRenderer _lineRenderer;

    private Vector2 _mouseScreenPosition;
    private float _dragDistanceFromCamera;

    private InputAction _clickAction;

    private void Awake()
    {
        _cam = Camera.main;

        // Line renderer setup
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.02f;
        _lineRenderer.endWidth = 0.02f;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = Color.red;
        _lineRenderer.endColor = Color.red;
        _lineRenderer.positionCount = 0;
    }

    private void OnEnable()
    {
        _clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        _clickAction.performed += OnClickPerformed;
        _clickAction.canceled += OnClickReleased;
        _clickAction.Enable();
    }

    private void OnDisable()
    {
        _clickAction.performed -= OnClickPerformed;
        _clickAction.canceled -= OnClickReleased;
        _clickAction.Disable();
    }

    private void Update()
    {
        if (Mouse.current != null)
            _mouseScreenPosition = Mouse.current.position.ReadValue();

        if (currentMode == Mode.Constraint && _springJoint != null && _selectedRigidbody != null)
        {
            Vector3 worldTarget = GetMouseWorldPoint();
            _springJoint.connectedAnchor = worldTarget;

            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, _selectedRigidbody.transform.TransformPoint(_springJoint.anchor));
            _lineRenderer.SetPosition(1, _springJoint.connectedAnchor);
        }
        else
        {
            _lineRenderer.positionCount = 0;
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = BuildRayFromScreen(_cam, _mouseScreenPosition);
        float rayDistance = (_RayDistance > 0f ? _RayDistance : Mathf.Infinity);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null) return;

            if (currentMode == Mode.Push)
            {
                rb.AddForce(ray.direction * _PushForce, ForceMode.Impulse);
            }
            else if (currentMode == Mode.Constraint)
            {
                _springJoint = rb.gameObject.AddComponent<SpringJoint>();
                _springJoint.autoConfigureConnectedAnchor = false;
                _springJoint.connectedAnchor = hit.point;
                _springJoint.anchor = rb.transform.InverseTransformPoint(hit.point);
                _springJoint.spring = _SpringStrength;
                _springJoint.damper = _Damping;
                _springJoint.maxDistance = 0.0f;

                _selectedRigidbody = rb;
                _dragDistanceFromCamera = (hit.point - _cam.transform.position).magnitude;
            }
        }
    }

    private void OnClickReleased(InputAction.CallbackContext context)
    {
        if (_springJoint != null)
        {
            Destroy(_springJoint);
            _springJoint = null;
            _selectedRigidbody = null;
        }

        _lineRenderer.positionCount = 0;
    }

    private Vector3 GetMouseWorldPoint()
    {
        Ray ray = _cam.ScreenPointToRay(_mouseScreenPosition);
        return ray.GetPoint(_dragDistanceFromCamera); // 5 units from the camera
    }

    private static Ray BuildRayFromScreen(Camera cam, Vector2 screenPos)
    {
        if (!cam.orthographic)
        {
            // Perspective: built-in is perfect.
            return cam.ScreenPointToRay(screenPos);
        }

        // Orthographic:
        // 1) Get the world position on the near clip plane under the cursor
        // 2) Shoot forward along camera forward
        var near = cam.nearClipPlane;
        Vector3 worldOnNear =
            cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, near));
        Vector3 dir = cam.transform.forward; // already normalized

        return new Ray(worldOnNear, dir);
    }
}
