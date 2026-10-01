using UnityEngine;

/// <summary>
/// The plate in front of the Goal. Touch it and the Goal is taken away : a new one
/// appears elsewhere, with its own plate, and the approach you were halfway through
/// simply no longer exists.
///
/// Same rule as the stars of Level 04, and for the same reason : an approach you can
/// bounce your way through is not an approach.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalObstacle : MonoBehaviour
{
    [Tooltip("Impacts slower than this are free, in m/s. Parked inside the Goal the hull " +
             "sits less than a metre from the plate, and a slow nudge there should not " +
             "cost a whole approach. Zero means no mercy")]
    [SerializeField] float _MinimumImpactSpeed = 1.0f;

    [SerializeField] bool _ShowCounter = true;

    // The plate dies with the Goal it belongs to, so the count cannot live on it.
    static int _HullHits;

    SpaceshipControllerBase _Spaceship;
    GoalSpawner _Spawner;

    bool _RespawnPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCounter()
    {
        _HullHits = 0;
    }

    void Awake()
    {
        _Spaceship = FindFirstObjectByType<SpaceshipControllerBase>();
        _Spawner = FindFirstObjectByType<GoalSpawner>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Compares GAME OBJECTS, like GoalComponent does : the ship may carry more than
        // one controller, and comparing components then depends on which one was found.
        if (_Spaceship == null || !collision.collider.transform.IsChildOf(_Spaceship.transform))
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < _MinimumImpactSpeed)
        {
            return;
        }

        _HullHits++;

        // Only a flag. One impact lands several contacts in the same step, and each of
        // them taking the Goal away would move it several times in a single frame.
        _RespawnPending = true;
    }

    void Update()
    {
        if (!_RespawnPending)
        {
            return;
        }

        _RespawnPending = false;

        HudConsole.Log("Impact : le Goal se deplace, l'approche est a refaire");

        if (_Spawner != null)
        {
            _Spawner.RespawnGoal();
        }
    }

    /// Bottom left : the HUD console owns the top left and the briefing the right edge.
    void OnGUI()
    {
        if (!_ShowCounter)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10.0f, Screen.height - 40.0f, 260.0f, 30.0f));
        GUILayout.Label("Impacts coque : " + _HullHits);
        GUILayout.EndArea();
    }
}
