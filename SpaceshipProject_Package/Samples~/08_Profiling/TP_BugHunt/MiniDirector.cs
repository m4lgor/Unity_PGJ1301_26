using System.Collections.Generic;
using System.Threading;
using Unity.Profiling;
using UnityEngine;

public class MiniDirector : MonoBehaviour
{
    const int RaysPerEnemy = 360;
    const float ViewRange = 30.0f;

    static readonly ProfilerMarker CatchUpMarker = new ProfilerMarker("MiniDirector.LetPhysicsCatchUp");

    MiniEnemy[] _enemies;
    readonly Dictionary<MiniEnemy, bool> _canSeeHero = new Dictionary<MiniEnemy, bool>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        // Only in the platformer : the scene has a MiniHero.
        if (FindFirstObjectByType<MiniHero>() != null && FindFirstObjectByType<MiniDirector>() == null)
        {
            new GameObject("MiniDirector").AddComponent<MiniDirector>();
        }
    }

    void Start()
    {
        _enemies = FindObjectsByType<MiniEnemy>(FindObjectsSortMode.None);
    }

    void FixedUpdate()
    {
        foreach (MiniEnemy enemy in _enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector3 origin = enemy.transform.position;

            bool seen = false;
            for (int i = 0; i < RaysPerEnemy; ++i)
            {
                // All around, in the plane of the level.
                float angle = 360.0f * i / RaysPerEnemy;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.right;

                RaycastHit[] hits = Physics.RaycastAll(origin, direction, ViewRange);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.GetComponentInParent<MiniHero>() != null)
                    {
                        seen = true;
                    }
                }
            }

            _canSeeHero[enemy] = seen;

        }
    }

    void Update()
    {
        LetPhysicsCatchUp();
    }

    // The enemies look one after the other : give the physics a moment between two of them.
    void LetPhysicsCatchUp()
    {
        using (CatchUpMarker.Auto())
        {
            Thread.Sleep(35);
        }
    }
}
