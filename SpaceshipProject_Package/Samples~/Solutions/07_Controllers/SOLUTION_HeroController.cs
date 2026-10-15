using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SOLUTION - the hero.
///
/// Horizontal : the keys give a direction, and WalkSpeed a speed. The direction is turned by the
/// camera's yaw, so W is "away from the camera", whichever way the camera looks. The hero always
/// faces where the camera faces - no smoothing.
///
/// Vertical : the CharacterController has no gravity, so the hero keeps its own vertical
/// speed, and gravity pulls it down every frame. Standing on the ground, that pushes the hero into it
/// each Move, so isGrounded stays true. A jump is the speed that reaches JumpHeight : v = sqrt(2 * g * h).
///
/// One Move per frame, with both : the CharacterController does the collision.
///
/// Aim : a ray from the middle of the screen finds the point under the cross, and the weapon
/// turns toward it. The shot is a second ray, from the weapon. The hero is on the Ignore Raycast
/// layer, so neither ray ever hits the hero itself.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SOLUTION_HeroController : MonoBehaviour
{
    public float WalkSpeed = 4.0f;
    public float JumpHeight = 1.2f;
    public float Gravity = 20.0f;
    public float ShootRange = 100.0f;
    public float ShootImpulse = 120.0f;

    CharacterController _Controller;
    Transform _Weapon;
    float _VerticalSpeed;

    void Awake()
    {
        _Controller = GetComponent<CharacterController>();
        _Weapon = transform.Find("Weapon");
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        // STEP 1 : the direction from the input, turned by the camera's yaw
        float cameraYaw = Camera.main.transform.eulerAngles.y;

        Vector3 direction = Vector3.zero;
        if (keyboard.wKey.isPressed) direction.z += 1.0f;
        if (keyboard.sKey.isPressed) direction.z -= 1.0f;
        if (keyboard.dKey.isPressed) direction.x += 1.0f;
        if (keyboard.aKey.isPressed) direction.x -= 1.0f;
        direction = Vector3.ClampMagnitude(direction, 1.0f);
        direction = Quaternion.Euler(0.0f, cameraYaw, 0.0f) * direction;

        // STEP 2 : face where the camera faces
        transform.rotation = Quaternion.Euler(0.0f, cameraYaw, 0.0f);

        Vector3 velocity = direction * WalkSpeed;

        // STEP 3 and 4 : the vertical force (gravity) and the jump force, in the velocity
        if (_Controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
        {
            _VerticalSpeed = Mathf.Sqrt(2.0f * Gravity * JumpHeight);
        }

        _VerticalSpeed -= Gravity * Time.deltaTime;
        velocity.y = _VerticalSpeed;

        // THEN MOVE
        _Controller.Move(velocity * Time.deltaTime);

        // STEP 5 : aim (end of the class : TP_Shooting - not part of the first part of the workshop) : the middle of the screen gives a point in the world, the weapon points at it
        Ray centreRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        Vector3 aimPoint = centreRay.GetPoint(ShootRange);

        if (Physics.Raycast(centreRay, out RaycastHit centreHit, ShootRange))
        {
            aimPoint = centreHit.point;
        }

        _Weapon.rotation = Quaternion.LookRotation(aimPoint - _Weapon.position);

        // Yellow : the camera ray through the cross. Green : where the weapon points.
        Debug.DrawLine(centreRay.origin, aimPoint, Color.yellow);
        Debug.DrawLine(_Weapon.position, aimPoint, Color.green);

        // STEP 6 : shoot (end of the class too) : a second ray, from the weapon this time. Something may stand between
        // the weapon and what the camera sees.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.DrawRay(_Weapon.position, _Weapon.forward * ShootRange, Color.red, 1.0f);

            if (Physics.Raycast(_Weapon.position, _Weapon.forward, out RaycastHit hit, ShootRange))
            {
                ShotFeedback.Show(_Weapon.position, hit.point, true);

                // A spike out of the surface that was hit.
                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.magenta, 1.0f);

                // Part 2 : the hero does not know what it hit. It asks.
                IShootable target = hit.collider.GetComponentInParent<IShootable>();
                target?.OnShot(hit, _Weapon.forward);

                // The shot pushes whatever it hits, at the point it hits.
                if (hit.rigidbody)
                {
                    hit.rigidbody.AddForceAtPosition(_Weapon.forward * ShootImpulse, hit.point, ForceMode.Impulse);
                }
            }
            else
            {
                ShotFeedback.Show(_Weapon.position, _Weapon.position + _Weapon.forward * ShootRange, false);
            }
        }
    }
}
