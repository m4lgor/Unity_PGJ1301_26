using System.Collections.Generic;
using UnityEngine;

// -----------------------
// Not for you to edit. Part of the debugger demo (README_Debugger.txt).
// -----------------------

public class DbgShip
{
    public string Name;
    public float Attack = 20.0f;
    public float CritChance = 0.25f;

    public float Fuel = 10.0f;
    public float MaxFuel = 10.0f;
    public float BurnRate = 2.0f;
    public float Thrust = 3.0f;
    public Vector3 Velocity;
    public readonly Dictionary<string, int> Cargo = new Dictionary<string, int>();

    float m_Shield = 100.0f;

    // Seeded : the same crits happen at every run, so the demo is repeatable.
    readonly System.Random m_Rng = new System.Random(42);

    public DbgShip(string name)
    {
        Name = name;
    }

    // [BP-6] Every write to the shield goes through this setter : a conditional breakpoint on it
    // (value < 0) stops at the exact moment the shield goes bad, and the call stack says WHO did it.
    public float Shield
    {
        get => m_Shield;
        set => m_Shield = value;
    }

    // ---- Station 1 : step into ----

    public float Fire(DbgAsteroid target)
    {
        // [BP-1] Two calls on one line : Step Into goes to ComputeDamage first, Smart Step Into lets you choose.
        target.TakeDamage(ComputeDamage(target));
        return target.Hp;
    }

    float ComputeDamage(DbgAsteroid target)
    {
        float damage = Attack;
        damage = ApplyArmor(damage, target.Armor);
        damage = ApplyCritical(damage);
        Debug.Log(Name + " hits asteroid " + target.Id + " for " + damage);    // Step Over this one : it is Unity's code
        return damage;
    }

    static float ApplyArmor(float damage, float armor) => damage * (1.0f - armor);

    float ApplyCritical(float damage)
    {
        bool isCritical = m_Rng.NextDouble() < CritChance;
        return isCritical ? damage * 2.0f : damage;
    }

    // ---- Station 3 : watch ----

    public void Burn(float deltaTime)
    {
        if (Fuel <= 0.0f)
        {
            return;
        }

        Fuel = Mathf.Max(0.0f, Fuel - BurnRate * deltaTime);
        Velocity += Vector3.forward * (Thrust * deltaTime);
    }

    public void AddCargo(string item, int amount)
    {
        Cargo.TryGetValue(item, out int current);
        Cargo[item] = current + amount;
    }

    // ---- Station 6 : who touched my shield ----

    public void Regenerate()
    {
        Shield += 5.0f;
    }

    public void FireWeapons()
    {
        Shield -= 8.0f;
    }
}

// The reactor draws more and more power from the shield as the mission goes on.
public class DbgReactor
{
    public void Tick(DbgShip ship, int tick)
    {
        if (tick >= 5)
        {
            ship.Shield -= SurgeFor(tick);
        }
    }

    static float SurgeFor(int tick) => 10.0f * (tick - 4);
}
