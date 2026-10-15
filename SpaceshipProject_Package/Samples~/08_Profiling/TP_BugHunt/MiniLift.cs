using System.Collections.Generic;
using UnityEngine;

// A lift that goes up to HighY, waits, goes down to LowY, waits, and so on, BUT ONLY WHILE THE HERO STANDS ON IT.
// Without the hero on top it stays where it is. It never crushes anything : going down, it waits for whatever
// (the hero, an enemy) is underneath, touching it, to go away.
// It is a kinematic Rigidbody : the bodies that touch it are found with the contacts' normals.
public class MiniLift : MonoBehaviour
{
    public float LowY = -0.15f;
    public float HighY = 5.85f;
    public float Speed = 3.0f;
    public float PauseSeconds = 1.0f;

    Rigidbody _body;
    float _y;
    float _target;
    float _waitUntil;

    // What touches the lift from underneath.
    readonly HashSet<Rigidbody> _below = new HashSet<Rigidbody>();

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _y = _body.position.y;
        _target = HighY;
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody other = collision.rigidbody;
        if (other == null)
        {
            return;
        }

        // The normal points from the other body towards the lift : up for a body that touches it from underneath.
        if (collision.GetContact(0).normal.y > 0.5f)
        {
            _below.Add(other);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        Rigidbody other = collision.rigidbody;
        if (other != null)
        {
            _below.Remove(other);
        }
    }

    // The hero is on top when a body overlaps the strip just above the platform.
    bool HeroOnTop()
    {
        Vector3 centre = _body.position + Vector3.up * 0.3f;
        Collider[] colliders = Physics.OverlapBox(centre, new Vector3(1.4f, 0.4f, 0.9f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider collider in colliders)
        {
            if (collider.attachedRigidbody != null)
            {
                return true;
            }
        }

        return false;
    }

    bool SomethingBelow()
    {
        _below.RemoveWhere(body => body == null);
        return _below.Count > 0;
    }

    void FixedUpdate()
    {
        if (Time.time < _waitUntil || !HeroOnTop())
        {
            return;
        }

        float direction = Mathf.Sign(_target - _y);   // +1 : the target is above, -1 : below
        bool goingDown = direction > 0.0f;
        if (goingDown && SomethingBelow())
        {
            return;
        }

        // Move towards the target, never past it.
        _y = Mathf.MoveTowards(_y, _target, Speed * Time.fixedDeltaTime);
        _body.MovePosition(new Vector3(_body.position.x, _y, _body.position.z));

        // Arrived : wait, then head for the other end.
        if (Mathf.Approximately(_y, _target))
        {
            _waitUntil = Time.time + PauseSeconds;
            _target = (_target == HighY) ? LowY : HighY;
        }
    }
}
