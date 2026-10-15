using System;
using UnityEngine;

// -----------------------
// Not for you to edit. Part of the debugger demo (README_Debugger.txt).
// -----------------------

public class DbgAsteroid
{
    public readonly int Id;
    public float Hp = 100.0f;
    public float Armor = 0.25f;
    public float Density = 2.0f;
    public float Radius = 1.0f;
    public Vector3 Position;

    public event Action<DbgAsteroid> Destroyed;

    public DbgAsteroid(int id)
    {
        Id = id;
    }

    public bool IsAlive => Hp > 0.0f;

    // Volume of a sphere times density
    public float Mass => Density * (4.0f / 3.0f) * Mathf.PI * Radius * Radius * Radius;

    public void TakeDamage(float amount)
    {
        bool wasAlive = IsAlive;
        Hp = Mathf.Max(0.0f, Hp - amount);

        if (wasAlive && !IsAlive)
        {
            Destroyed?.Invoke(this);
        }
    }

    // A hit pushes the asteroid along +X : the lighter it is, the faster it goes.
    public void Push(float momentum)
    {
        float speed = momentum / Mass;      // [BP-2] conditional breakpoint : float.IsInfinity(speed)
        Position += Vector3.right * speed;
    }
}
