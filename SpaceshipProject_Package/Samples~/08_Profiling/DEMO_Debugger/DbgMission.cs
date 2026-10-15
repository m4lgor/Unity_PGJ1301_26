using System.Collections.Generic;
using UnityEngine;

// -----------------------
// Not for you to edit. Part of the debugger demo (README_Debugger.txt).
// Station 4 : a chain of calls, and an exception at the bottom of it.
// -----------------------

public class DbgRadar
{
    public List<Vector3> Contacts;      // Filled by Scan(). Null until then.

    public void Scan(IEnumerable<Vector3> contacts)
    {
        Contacts = new List<Vector3>(contacts);
    }

    public Vector3 Closest(Vector3 from)
    {
        float bestDistance = float.MaxValue;
        Vector3 best = from;

        foreach (Vector3 contact in Contacts)       // [BP-4] the exception is thrown here
        {
            float distance = Vector3.Distance(from, contact);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = contact;
            }
        }

        return best;
    }
}

public class DbgPilot
{
    public readonly string Name;
    public Vector3 Position;

    public DbgPilot(string name, Vector3 position)
    {
        Name = name;
        Position = position;
    }

    public void Engage(DbgRadar radar)
    {
        Vector3 target = PickTarget(radar);
        Debug.Log(Name + " engages a target at " + target);
    }

    Vector3 PickTarget(DbgRadar radar)
    {
        return radar.Closest(Position);
    }
}

public class DbgSquadron
{
    public readonly string Name;
    public readonly DbgRadar Radar = new DbgRadar();
    public readonly List<DbgPilot> Pilots = new List<DbgPilot>();

    public DbgSquadron(string name)
    {
        Name = name;
        Pilots.Add(new DbgPilot(name + "-1", new Vector3(0.0f, 0.0f, 0.0f)));
        Pilots.Add(new DbgPilot(name + "-2", new Vector3(5.0f, 0.0f, 0.0f)));
    }

    public void Launch()
    {
        Debug.Log("Squadron " + Name + " launches");

        foreach (DbgPilot pilot in Pilots)
        {
            pilot.Engage(Radar);
        }
    }
}

public class DbgMission
{
    public readonly List<DbgSquadron> Squadrons = new List<DbgSquadron>();

    public DbgMission()
    {
        DbgSquadron alpha = new DbgSquadron("Alpha");
        alpha.Radar.Scan(new[] { new Vector3(10.0f, 0.0f, 3.0f), new Vector3(-4.0f, 0.0f, 8.0f) });
        Squadrons.Add(alpha);

        DbgSquadron bravo = new DbgSquadron("Bravo");        // Nobody scanned for Bravo
        Squadrons.Add(bravo);
    }

    public void Run()
    {
        foreach (DbgSquadron squadron in Squadrons)
        {
            squadron.Launch();
        }
    }
}
