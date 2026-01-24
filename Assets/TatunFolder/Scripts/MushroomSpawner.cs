
using UnityEngine;
using System.Collections;

public class MushroomSpawner : MonoBehaviour
{
    [Header("Mushroom Prefabs")]
    public GameObject mushroomTypeAPrefab;
    public GameObject mushroomTypeBPrefab;
    public GameObject mushroomTypeCPrefab; // evil shroom

    [Header("Spawn Settings")]
    public float spawnInterval = 3.0f;
    public float spawnIntervalVariance = 0.5f;
    [Range(0f, 1f)]
    public float evilSpawnChance = 0.05f;

    [Header("Game Settings")]
    public int maxMushroomsSpawned = 15; // interpreted as total number this spawner will create for the level

    private Camera mainCamera;
    private int currentMushroomCount = 0; // active from this spawner
    private int spawnedCount = 0;
    private bool hasFinished = false;
    private bool isSpawning = false;
    private Coroutine spawnRoutine;

    // Events / callbacks
    public System.Action<MushroomController> onMushroomSpawned;
    public System.Action<MushroomSpawner> onSpawnerFinished;

    private GameManager gameManager;
    private int gameLevel = 1;

    private void Awake()
    {
        mainCamera = Camera.main;
        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
    }

    private void Start()
    {
        // Don't auto-start spawning - wait for GameManager to call StartSpawning()
    }

    public void SetGameLevel(int level)
    {
        gameLevel = level;
    }

    // Called by GameManager to begin spawning (after level transition)
    public void StartSpawning()
    {
        if (isSpawning) return;

        isSpawning = true;
        spawnedCount = 0;
        hasFinished = false;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    // Called by GameManager to stop spawning (level complete or game over)
    public void StopSpawning()
    {
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // Spawn until we have spawned maxMushroomsSpawned total
        while (spawnedCount < maxMushroomsSpawned && isSpawning)
        {
            SpawnMushroom();
            yield return new WaitForSeconds(spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance));
        }

        // finished spawning
        hasFinished = true;
        isSpawning = false;
        onSpawnerFinished?.Invoke(this);

        // GameManager listens to onSpawnerFinished via subscription in InitializeLevel
    }

    private void SpawnMushroom()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Randomly choose mushroom type
        GameObject prefabToSpawn = null;
        // rare evil shroom
        if (mushroomTypeCPrefab != null && Random.value < evilSpawnChance)
        {
            prefabToSpawn = mushroomTypeCPrefab;
        }
        else
        {
            bool spawnTypeA = Random.value > 0.5f;
            prefabToSpawn = spawnTypeA ? mushroomTypeAPrefab : mushroomTypeBPrefab;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Mushroom prefab not found!");
            return;
        }

        var spawnPos = transform.position;

        // Spawn the mushroom
        GameObject mushroom = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        spawnedCount++;
        currentMushroomCount++;

        MushroomController mc = mushroom.GetComponent<MushroomController>();
        if (mc != null)
        {
            mc.originSpawner = this;
        }

        // Notify listeners about spawned mushroom (GameManager subscribes via event in InitializeLevel)
        onMushroomSpawned?.Invoke(mc);

        // Make sure GameManager knows about this mushroom (use authoritative API)
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterMushroom(mc);

        // Attach tracker so spawner knows when its spawned mushroom was destroyed (so active count decreases)
        MushroomTracker tracker = mushroom.AddComponent<MushroomTracker>();
        // capture mc in closure so we can unregister specific mushroom when destroyed
        tracker.onDestroyed = () =>
        {
            currentMushroomCount = Mathf.Max(0, currentMushroomCount - 1);
            if (GameManager.Instance != null)
                GameManager.Instance.UnregisterMushroom(mc);
        };

        // Possibly make this mushroom "erratic" based on level
        if (mc != null)
        {
            float erraticChance = 0f;
            if (gameLevel >= 3)
            {
                erraticChance = Mathf.Clamp01(0.1f + (gameLevel - 3) * 0.05f);
            }

            bool isErratic = Random.value < erraticChance;
            mc.isErratic = isErratic;

            if (isErratic)
            {
                // randomize erratic movement parameters
                mc.erraticSpeedMultiplier = 1.5f;
                mc.zigzagFrequency = Random.Range(2f, 10f);
                mc.zigzagAmplitude = Random.Range(0.5f, 3f);
            }
        }
    }

    // Called when a mushroom from this spawner is placed into a sorting area
    public void NotifyMushroomPlaced(MushroomController mc)
    {
        currentMushroomCount = Mathf.Max(0, currentMushroomCount - 1);
    }

    private class MushroomTracker : MonoBehaviour
    {
        public System.Action onDestroyed;

        private void OnDestroy()
        {
            onDestroyed?.Invoke();
        }
    }

    // Optional public helpers
    public bool HasFinished => hasFinished;
    public int SpawnedCount => spawnedCount;
    public int CurrentActiveCount => currentMushroomCount;
}