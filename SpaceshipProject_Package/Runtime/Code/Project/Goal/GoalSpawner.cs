using UnityEngine;

public class GoalSpawner : MonoBehaviour
{
    [SerializeField] GameObject _GoalPrefab;

    [SerializeField] float _minDistanceFromShip = 3f;    // distance mini au vaisseau

    /// <summary>
    /// Level 00 only: always spawn the goal at _FixedSpawnPosition instead of picking a
    /// random quadrant. Level 00 needs the goal on a single engine's push axis, so that
    /// reaching it takes ONE engine and not a mix of two -- mixing is Level 01.
    /// </summary>
    [SerializeField] bool _UseFixedSpawn = false;
    [SerializeField] Vector3 _FixedSpawnPosition = new Vector3(0.0f, 4.0f, 0.0f);

    /// <summary>
    /// How long the ship has to STAY in the goal for it to count, in seconds. Reaching a
    /// goal at full speed is not reaching it: this is what forces a clean arrival.
    ///
    /// Leave it negative and the goal prefab's own value is used, which keeps the level
    /// exactly as it was. Set it to 0 or more and it overrides the prefab, so every level
    /// can have its own dwell time while sharing one goal prefab.
    /// </summary>
    [SerializeField] float _RequiredTimeInZone = -1.0f;

    private GameObject _currentGoal;
    private Vector2 _xLimits = new Vector2(-10f, 10f);   // [minX, maxX]
    private Vector2 _yLimits = new Vector2(-10f, 10f);   // [minY, maxY]

    private SpaceshipControllerBase _spaceshipComponent;
    private Vector3 _spawnOffset = new Vector3(5.0f, 0.0f, 0.0f);

    private void Awake()
    {
        SpaceshipControllerBase[] spaceshipComponents = FindObjectsByType<SpaceshipControllerBase>(FindObjectsSortMode.None);  
        if(spaceshipComponents.Length == 0)
        {
            Debug.LogError("No spaceship controller found in the scene.");
            enabled = false;
            return;
        }

        if(spaceshipComponents.Length > 1)
        {
            // Worth naming them: FindObjectsByType with SortMode.None gives no ordering
            // guarantee, so "the first one found" can change between runs. Everything
            // keyed on this reference - TargetPosition, OnGoalReached - then goes to a
            // controller that may not be the one the student is writing.
            string names = "";
            foreach (SpaceshipControllerBase controller in spaceshipComponents)
            {
                names += "\n  - " + controller.GetType().Name + " on '" + controller.name + "'";
            }

            Debug.LogWarning("GoalSpawner - " + spaceshipComponents.Length
                             + " spaceship controllers found, and the choice between them "
                             + "is not stable. Keep exactly one:" + names,
                             spaceshipComponents[0]);
        }

        //Find Spaceship controller
        _spaceshipComponent = spaceshipComponents[0];
        enabled = _spaceshipComponent != null;    
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!enabled)
            return;

        if (_spaceshipComponent == null)  
            return;

        Vector2 arenaSizeWithMargin = GlobalSettings.Instance.ArenaSize - new Vector2(2.0f, 2.0f);
        _xLimits = new Vector2(-arenaSizeWithMargin.x * 0.5f, arenaSizeWithMargin.x * 0.5f);
        _yLimits = new Vector2(-arenaSizeWithMargin.y * 0.5f, arenaSizeWithMargin.y * 0.5f);

        SpawnGoalPrefab();
    }

    // --- Spawn in a quadrant different from the ship's, with min distance ---
    void SpawnGoalPrefab()
    {
        if (_GoalPrefab == null) { Debug.LogError("Goal prefab is not assigned."); return; }
        if (_spaceshipComponent == null) { Debug.LogError("Spaceship reference is missing."); return; }

        if (_UseFixedSpawn)
        {
            SpawnAt(_FixedSpawnPosition);
            return;
        }

        Vector2 ship = _spaceshipComponent.transform.position; // (x,y)
        float cx = (_xLimits.x + _xLimits.y) * 0.5f;
        float cy = (_yLimits.x + _yLimits.y) * 0.5f;

        // Player quadrant: 0=BL, 1=BR, 2=TL, 3=TR (w.r.t. midlines cx,cy)
        int qx = ship.x < cx ? 0 : 1;
        int qy = ship.y < cy ? 0 : 1;
        int playerQ = qy * 2 + qx;

        // Other quadrants (3 of them), iterate from a random start so choice is varied
        int[] others = new int[3];
        { int idx = 0; for (int i = 0; i < 4; i++) if (i != playerQ) others[idx++] = i; }
        int start = Random.Range(0, 3);

        Vector3? chosen = null;
        for (int k = 0; k < 3 && !chosen.HasValue; k++)
        {
            int q = others[(start + k) % 3];
            if (TrySampleInCell(q, ship, cx, cy, out Vector3 p))
                chosen = p;
        }

        // Fallback: farthest corner among "other" quadrants, then enforce min distance
        if (!chosen.HasValue)
        {
            float best = float.NegativeInfinity;
            Vector3 bestP = Vector3.zero;

            for (int k = 0; k < 3; k++)
            {
                int q = others[k];
                foreach (var corner in CellCorners(q, cx, cy))
                {
                    float d2 = ((Vector2)corner - ship).sqrMagnitude;
                    if (d2 > best) { best = d2; bestP = corner; }
                }
            }

            // Ensure >= _minDistanceFromShip along the direction from ship to that corner
            Vector2 dir = ((Vector2)bestP - ship).normalized;
            Vector2 p2 = ship + dir * _minDistanceFromShip;
            bestP.x = Mathf.Clamp(p2.x, _xLimits.x, _xLimits.y);
            bestP.y = Mathf.Clamp(p2.y, _yLimits.x, _yLimits.y);
            bestP.z = 0f;
            chosen = bestP;
        }

        SpawnAt(chosen.Value);
    }

    /// <summary>
    /// Instantiates the goal at a position and wires it to the spaceship. Shared by the
    /// random-quadrant path and the fixed Level 00 path so the two cannot drift apart.
    /// </summary>
    void SpawnAt(Vector3 position)
    {
        // (Re)spawn and wire the behaviour
        if (_currentGoal != null) Destroy(_currentGoal);
        _currentGoal = Instantiate(_GoalPrefab, position, Quaternion.identity);

        var collider = _currentGoal.GetComponent<Collider>();    
        if (collider != null)
        {
            collider.isTrigger = true; // Ensure it's a trigger 
        }

        var behaviour = _currentGoal.GetComponent<GoalComponentBase>();
        if (behaviour == null) behaviour = _currentGoal.AddComponent<GoalComponentBase>();
        behaviour.Init(this, _spaceshipComponent, _RequiredTimeInZone);
        behaviour.OnReady = () =>
        {
            _spaceshipComponent?.OnGoalSpawned();
        };
        behaviour.OnEnd = () =>
        {
            _spaceshipComponent?.OnGoalDestroyed();
        };

        Vector3 desiredDirection = (_spaceshipComponent.transform.position - _currentGoal.transform.position).normalized;
        Vector3 obstacleOffset = Vector3.up; // This is bad hardcoded
        Quaternion delta = Quaternion.FromToRotation(obstacleOffset, desiredDirection);

        _currentGoal.transform.rotation = delta * _currentGoal.transform.rotation;
    }

    public void OnGoalReached()
    {
        RespawnGoal();
    }

    /// <summary>
    /// Takes the current goal away and puts a new one somewhere else.
    ///
    /// Separate from OnGoalReached because a goal does not only move when it is EARNED: a
    /// level can also take it back. Level 04 does exactly that on a hull impact, and
    /// calling something named "reached" to punish a student would be a lie in the log.
    /// </summary>
    public void RespawnGoal()
    {
        if (_currentGoal != null)
        {
            Destroy(_currentGoal);
            _currentGoal = null;
        }

        SpawnGoalPrefab();
    }

    bool TrySampleInCell(int quad, Vector2 ship, float cx, float cy, out Vector3 point)
    {
        GetCellBounds(quad, cx, cy, out float xmin, out float xmax, out float ymin, out float ymax);

        const int maxTries = 20;
        for (int i = 0; i < maxTries; i++)
        {
            float x = Random.Range(xmin, xmax);
            float y = Random.Range(ymin, ymax);
            if (Vector2.Distance(new Vector2(x, y), ship) >= _minDistanceFromShip)
            {
                point = new Vector3(x, y, 0f);
                return true;
            }
        }
        point = default;
        return false;
    }

    void GetCellBounds(int quad, float cx, float cy, out float xmin, out float xmax, out float ymin, out float ymax)
    {
        int qx = quad % 2;  // 0=left, 1=right
        int qy = quad / 2;  // 0=bottom, 1=top

        xmin = (qx == 0) ? _xLimits.x : cx;
        xmax = (qx == 0) ? cx : _xLimits.y;
        ymin = (qy == 0) ? _yLimits.x : cy;
        ymax = (qy == 0) ? cy : _yLimits.y;
    }

    System.Collections.Generic.IEnumerable<Vector3> CellCorners(int quad, float cx, float cy)
    {
        GetCellBounds(quad, cx, cy, out float xmin, out float xmax, out float ymin, out float ymax);
        yield return new Vector3(xmin, ymin, 0f);
        yield return new Vector3(xmin, ymax, 0f);
        yield return new Vector3(xmax, ymin, 0f);
        yield return new Vector3(xmax, ymax, 0f);
    }
}
