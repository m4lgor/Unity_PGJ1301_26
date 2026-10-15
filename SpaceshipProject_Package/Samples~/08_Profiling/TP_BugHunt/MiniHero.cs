using UnityEngine;
using UnityEngine.InputSystem;

// The hero : A / D (or the arrows) run, Space jumps when the hero stands on something.
// The Rigidbody is on this object ; the collider is on its child "Body".
// Falling into the pit, or touching an enemy from the side, sends the hero back to where it started.
public class MiniHero : MonoBehaviour
{
    public float Speed = 6.0f;
    public float Acceleration = 50.0f;
    public float JumpImpulse = 6.6f;
    public float KillHeight = -8.0f;

    Rigidbody _body;
    Vector3 _start;
    float _input;

    bool IsGrounded
    {
        get
        {
            // A short ray down from the middle of the capsule (half height 0.9).
            return Physics.Raycast(_body.position, Vector3.down, 1.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _start = transform.position;
    }

    void Update()
    {
        if (transform.position.y < KillHeight)
        {
            Die();
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool pressedLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool pressedRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

        if (pressedLeft)
        {
            _input = -1.0f;
        }
        else
        {
            _input = 0.0f;
        }

        if (pressedRight)
        {
            _input = 1.0f;
        }
        else
        {
            _input = 0.0f;
        }
    }

    void FixedUpdate()
    {
        // Run : speed up towards the speed the keys ask for.
        Vector3 velocity = _body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, _input * Speed, Acceleration * Time.fixedDeltaTime);
        _body.linearVelocity = velocity;

        // Jump : one impulse when Space goes down, if the hero is on the ground.
        Keyboard keyboard = Keyboard.current;
        bool grounded = IsGrounded;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && grounded)
        {
            _body.AddForce(Vector3.up * JumpImpulse, ForceMode.Impulse);
        }

        // On the ground, this is a safe place.
        if (grounded)
        {
            _start = _body.position;
        }
    }

    public void Die()
    {
        MiniGame.Say("Ouch !");
        transform.position = _start;
        _body.position = _start;
        _body.linearVelocity = Vector3.zero;
    }
}
