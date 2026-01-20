using UnityEngine;
using System.Collections;

public class MushroomSpawner : MonoBehaviour
{
    [Header("Mushroom Prefabs")]
    public GameObject mushroomTypeAPrefab;
    public GameObject mushroomTypeBPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 3.0f;
    public float spawnHeightOffset = 0.5f; // Distance above top of screen
    public float horizontalSpawnRange = 0.7f; // 0-1 range of screen width to use

    [Header("Game Settings")]
    public int maxMushroomsOnScreen = 8;

    private Camera mainCamera;
    private int currentMushroomCount = 0;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentMushroomCount < maxMushroomsOnScreen)
            {
                SpawnMushroom();
            }
        }
    }

    private void SpawnMushroom()
    {
        if (mainCamera == null) return;

        // Randomly choose mushroom type
        bool spawnTypeA = Random.value > 0.5f;
        GameObject prefabToSpawn = spawnTypeA ? mushroomTypeAPrefab : mushroomTypeBPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Mushroom prefab not assigned!");
            return;
        }

        // Calculate spawn position at top of screen
        float randomX = Random.Range(0.5f - horizontalSpawnRange / 2f, 0.5f + horizontalSpawnRange / 2f);
        Vector3 viewportPos = new Vector3(randomX, 1.0f + spawnHeightOffset, 10f);
        Vector3 spawnPos = mainCamera.ViewportToWorldPoint(viewportPos);
        spawnPos.z = 0;

        // Spawn the mushroom
        GameObject mushroom = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        currentMushroomCount++;

        // Subscribe to destruction to update count
        MushroomTracker tracker = mushroom.AddComponent<MushroomTracker>();
        tracker.onDestroyed = () => currentMushroomCount--;
    }

    // Helper component to track when mushrooms are destroyed
    private class MushroomTracker : MonoBehaviour
    {
        public System.Action onDestroyed;

        private void OnDestroy()
        {
            onDestroyed?.Invoke();
        }
    }
}