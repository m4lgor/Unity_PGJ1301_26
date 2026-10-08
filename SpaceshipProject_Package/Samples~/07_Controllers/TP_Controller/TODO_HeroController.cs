using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The hero is a capsule with a CharacterController. Nothing moves it yet : this is where
/// the controller lives. The steps are in Update.
///
/// A controller turns what the player wants (keys) into what the character does (a Move).
/// Unity's CharacterController does not use forces : YOU decide the velocity, every frame,
/// and it only stops you at walls, slopes and steps.
///
/// The hero is on the Ignore Raycast layer : its own capsule never gets in the way.
///
/// Then play with the CharacterController in the Inspector (Slope Limit, Step Offset) and
/// with the numbers below, and see what the green, yellow and red stairs and ramps do.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TODO_HeroController : MonoBehaviour
{
    public float WalkSpeed = 4.0f;
    public float JumpHeight = 1.2f;
    public float Gravity = 20.0f;
    public float ShootRange = 100.0f;
    public float ShootImpulse = 120.0f;

    CharacterController _Controller;
    Transform _Weapon;

    void Awake()
    {
        _Controller = GetComponent<CharacterController>();
        _Weapon = transform.Find("Weapon");
    }

    void Update()
    {
        Vector3 direction = Vector3.zero;
        Keyboard keyboard = Keyboard.current;

        // Build it up in this order, and test on the playground after each step.

        // STEP 1 : change the direction with the input.
        //
        //   WASD -> a direction (x for A and D, z for W and S). Keep it from being longer than 1 when
        //   two keys are down : Vector3.ClampMagnitude.
        //
        //   if (keyboard.wKey.isPressed) direction.z += 1.0f;
        //   ...
        //
        //   Then rotate the direction to be the same as the camera's Yaw. The camera is turned by the
        //   mouse (OrbitCamera, ready-made) : its Yaw is Camera.main.transform.eulerAngles.y.
        //   Quaternion.Euler(0, yaw, 0) * direction
        //   So W is "away from the camera", wherever the camera looks.

        // STEP 2 : rotate our transform to be the same as the camera.
        //
        //   The same Yaw : transform.rotation = Quaternion.Euler(0, yaw, 0).

        // STEP 3 and 4 : add the vertical force and the jump force to the velocity.
        //
        //   The velocity so far is flat : direction * WalkSpeed.
        //
        //   The CharacterController has no gravity. Keep a vertical speed yourself (a field), and
        //   put it in velocity.y :
        //     - every frame it falls : vertical speed -= Gravity * Time.deltaTime
        //     - Space makes it jump, ONLY when _Controller.isGrounded
        //         the speed that reaches JumpHeight : Mathf.Sqrt(2 * Gravity * JumpHeight)
        //
        //   isGrounded is true when the last Move pushed the hero into the ground. Gravity does that:
        //   every frame it pulls the hero down a little, so standing on the ground the Move touches it.

        // THEN MOVE !
        //
        //   _Controller.Move(velocity * Time.deltaTime);
        //   One Move per frame, with the whole velocity : the CharacterController does the collision.

        // ---------------------------------------------------------------------------------------
        // The first part of the workshop stops here. Aiming and shooting are NOT part of it : they
        // come at the END of the class, in TP_Shooting. Skip the next steps for now.
        // ---------------------------------------------------------------------------------------

        // STEP 5 : aim. The weapon (the child named "Weapon") points at the cross in the middle of
        // the screen.
        //
        //   The viewport is the screen from 0 to 1 : (0.5, 0.5) is the middle, where the cross is.
        //   A screen point is a line in the world, a Ray from the camera :
        //       Ray centreRay = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        //
        //   The world point to aim at :
        //     - Physics.Raycast(centreRay, out hit, ShootRange) : hit.point is what is under the cross.
        //     - if nothing hit : centreRay.GetPoint(ShootRange), the point on the ray at that distance.
        //   The weapon looks at it : Quaternion.LookRotation(aimPoint - _Weapon.position).

        // STEP 6 : shoot. Left click, a Physics.Raycast from the weapon along its forward.
        //
        //   Log what it hits, and show it : ShotFeedback.Show(from, to, hit).
        //   Debug.DrawLine / DrawRay for the camera ray, the weapon's line and the shot : you cannot
        //   fix a ray you cannot see (Game view : turn Gizmos on).
        //
        //   The hero does not know what it hit : the thing that was hit decides what a shot does.
        //   Anything that reacts to a shot has the IShootable interface (IShootable.cs), and its one
        //   function, OnShot. Ask what was hit, and call it if it is there :
        //       IShootable target = hit.collider.GetComponentInParent<IShootable>();
        //       if (target != null) target.OnShot(hit, _Weapon.forward);
        //
        //   Then the shot pushes whatever has a Rigidbody, at the point it hits :
        //       hit.rigidbody.AddForceAtPosition(_Weapon.forward * ShootImpulse, hit.point, ForceMode.Impulse);
    }
}
