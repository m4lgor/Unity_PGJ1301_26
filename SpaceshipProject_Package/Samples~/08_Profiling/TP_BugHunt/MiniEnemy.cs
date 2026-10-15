using UnityEngine;

// An enemy that walks back and forth. It turns around at a wall or at the edge of its ground.
// If the hero lands on it from above, it dies (+100) and the hero bounces. Any other contact kills the hero.
public class MiniEnemy : MonoBehaviour
{
    public float Speed = 2.0f;

    Rigidbody _body;
    int _wallMask;
    float _direction = -1.0f;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _wallMask = LayerMask.GetMask("Walls");
    }

    void FixedUpdate()
    {
        Vector3 position = _body.position;
        Vector3 ahead = position - Vector3.right * _direction * 0.5f;

        bool wallAhead = Physics.Raycast(position, Vector3.right * _direction, 0.5f, _wallMask, QueryTriggerInteraction.Ignore);
        bool groundAhead = Physics.Raycast(ahead, Vector3.down, 1.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (wallAhead || !groundAhead)
        {
            _direction = -_direction;
        }

        Vector3 velocity = _body.linearVelocity;
        _body.linearVelocity = new Vector3(_direction * Speed, velocity.y, 0.0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        Touch(collision.rigidbody);
    }

    public void Touch(Rigidbody other)
    {
        if (other == null)
        {
            return;
        }

        MiniHero hero = other.GetComponent<MiniHero>();
        if (hero == null)
        {
            return;
        }

        // The hero lands on the enemy : above it, and moving down.
        bool landedOnTop = other.position.y > _body.position.y + 0.45f && other.linearVelocity.y < -0.5f;
        if (landedOnTop)
        {
            MiniGame.Score += 100;
            other.linearVelocity = new Vector3(other.linearVelocity.x, 7.0f, 0.0f);
            Destroy(gameObject);
        }
        else
        {
            hero.Die();
        }
    }
}
