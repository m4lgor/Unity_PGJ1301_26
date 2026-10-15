using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// -----------------------
// Not for you to edit. The debugger demo : six stations, one key each, one debugger feature each.
// Nothing happens until you press a key, so breakpoints can be placed first.
// The [BP-n] tags in the Dbg*.cs files mark the lines worth a breakpoint (see README_Debugger.txt).
//
//   1  Step Into / Step Over / Step Out         3  Watch (and changing a value live)
//   2  Conditional and hit-count breakpoints    4  Call stack, and an exception
//   5  Call stack of a recursion                6  "Who changed this value ?" (the managed answer to a data breakpoint)
// -----------------------

public class DebugDemo : MonoBehaviour
{
    const int MaxLogLines = 16;

    readonly List<string> m_LogLines = new List<string>();

    // Station 3 state : lives across frames so it can be watched while the game runs.
    DbgShip m_BurnShip;
    List<DbgAsteroid> m_Field;
    bool m_Burning;
    float m_MineTimer;
    int m_MineTick;

    void OnEnable()
    {
        Application.logMessageReceived += OnLogMessage;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessage;
    }

    void OnLogMessage(string message, string stackTrace, LogType type)
    {
        string prefix = type == LogType.Log ? "" : "[" + type + "] ";
        m_LogLines.Add(prefix + message);

        if (m_LogLines.Count > MaxLogLines)
        {
            m_LogLines.RemoveAt(0);
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) Station1_StepInto();
            if (keyboard.digit2Key.wasPressedThisFrame) Station2_Wave();
            if (keyboard.digit3Key.wasPressedThisFrame) Station3_StartBurn();
            if (keyboard.digit4Key.wasPressedThisFrame) Station4_Mission();
            if (keyboard.digit5Key.wasPressedThisFrame) Station5_Fleet();
            if (keyboard.digit6Key.wasPressedThisFrame) Station6_Shield();
            if (keyboard.cKey.wasPressedThisFrame) m_LogLines.Clear();
        }

        UpdateBurn();
    }

    // ---- Station 1 : Breakpoint, Step Over / Step Into / Step Out, Smart Step Into, Run to Cursor ----

    void Station1_StepInto()
    {
        DbgShip ship = new DbgShip("Aurora");
        DbgAsteroid target = new DbgAsteroid(1);

        for (int shot = 0; shot < 3; shot++)
        {
            float remaining = ship.Fire(target);        // [BP-1] Step Into here
            Debug.Log("Asteroid has " + remaining + " Hp left");
        }
    }

    // ---- Station 2 : Conditional breakpoint, Hit count, Evaluate and log (no suspend) ----

    void Station2_Wave()
    {
        List<DbgAsteroid> wave = new List<DbgAsteroid>();

        for (int i = 0; i < 300; i++)
        {
            DbgAsteroid asteroid = new DbgAsteroid(i);
            asteroid.Radius = 0.5f + (i % 7) * 0.25f;
            asteroid.Density = (i % 97 == 96) ? 0.0f : 1.5f + (i % 5) * 0.5f;      // 96, 193 and 290 have no density
            wave.Add(asteroid);
        }

        foreach (DbgAsteroid asteroid in wave)
        {
            asteroid.Push(50.0f);               // [BP-2] a plain breakpoint here stops 300 times
        }

        int lost = 0;
        foreach (DbgAsteroid asteroid in wave)
        {
            if (float.IsInfinity(asteroid.Position.x) || float.IsNaN(asteroid.Position.x))
            {
                lost++;
            }
        }

        Debug.LogWarning(lost + " asteroids left the map at infinite speed");
    }

    // ---- Station 3 : Watch, Evaluate Expression, Set Value ----

    void Station3_StartBurn()
    {
        m_BurnShip = new DbgShip("Aurora");
        m_Field = new List<DbgAsteroid>();

        for (int i = 0; i < 4; i++)
        {
            DbgAsteroid asteroid = new DbgAsteroid(i);
            asteroid.Hp = 40.0f;
            m_Field.Add(asteroid);
        }

        m_Burning = true;
        m_MineTimer = 0.0f;
        m_MineTick = 0;
        Debug.Log("Burn started : " + m_BurnShip.Fuel + " fuel");
    }

    void UpdateBurn()
    {
        if (!m_Burning)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        m_BurnShip.Burn(deltaTime);                 // [BP-3] conditional breakpoint : m_BurnShip.Fuel < 5
        m_MineTimer += deltaTime;

        if (m_MineTimer >= 0.5f)
        {
            m_MineTimer -= 0.5f;
            MineNextAsteroid();
        }

        if (m_BurnShip.Fuel <= 0.0f)
        {
            m_Burning = false;
            Debug.Log("Out of fuel. Cargo : " + m_BurnShip.Cargo.Count + " kind(s), speed " + m_BurnShip.Velocity.magnitude);
        }
    }

    void MineNextAsteroid()
    {
        DbgAsteroid target = m_Field.Find(a => a.IsAlive);

        if (target == null)
        {
            return;
        }

        m_BurnShip.Fire(target);
        m_MineTick++;

        if (!target.IsAlive)
        {
            m_BurnShip.AddCargo("ore", 5);
        }
    }

    // ---- Station 4 : Call stack ----

    void Station4_Mission()
    {
        DbgMission mission = new DbgMission();
        mission.Run();
    }

    // ---- Station 5 : Call stack of a recursion, Conditional breakpoint ----

    void Station5_Fleet()
    {
        DbgFleetNode fleet = new DbgFleetNode("Fleet", 5);
        DbgFleetNode lastShip = null;

        for (int s = 0; s < 3; s++)
        {
            DbgFleetNode squadron = fleet.Add(new DbgFleetNode("Squadron " + s, 2));

            for (int n = 0; n < 4; n++)
            {
                lastShip = squadron.Add(new DbgFleetNode("Ship " + s + "." + n, 10));
            }
        }

        Debug.Log("Crew in the fleet : " + fleet.CountCrew(0));        // [BP-5] works : 131

        lastShip.Add(fleet);                // The reinforcement is attached... under one of its own ships
        Debug.Log("Crew after the reinforcement : " + fleet.CountCrew(0));
    }

    // ---- Station 6 : "Who changed this?" (conditional breakpoint in a setter, instead of an address breakpoint), Mark Object ----

    void Station6_Shield()
    {
        DbgShip ship = new DbgShip("Aurora");
        DbgShip backup = ship;              // Meant to be a snapshot to restore later. Is it ?
        DbgReactor reactor = new DbgReactor();

        for (int tick = 0; tick < 10; tick++)
        {
            ship.Regenerate();
            ship.FireWeapons();
            reactor.Tick(ship, tick);
        }

        Debug.Log("Shield : " + ship.Shield + ", backup says " + backup.Shield + ", same object : " + ReferenceEquals(ship, backup));
    }

    // ---- On-screen panel ----

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 760.0f, 600.0f), GUI.skin.box);
        GUILayout.Label("DEBUGGER DEMO   (Attach the debugger, place breakpoints, then press a key)   frame " + Time.frameCount);
        GUILayout.Label("1  Step Into : 3 shots at an asteroid");
        GUILayout.Label("2  Conditional breakpoint : a wave of 300 asteroids, 3 are broken");
        GUILayout.Label("3  Watch : a 5 second burn (change Fuel from the Watch window)");
        GUILayout.Label("4  Call stack : two squadrons launch, one crashes");
        GUILayout.Label("5  Call stack of a recursion : crew count");
        GUILayout.Label("6  Who changed the shield ?        C  clear the log");
        GUILayout.Space(8.0f);

        foreach (string line in m_LogLines)
        {
            GUILayout.Label(line);
        }

        GUILayout.EndArea();
    }
}
