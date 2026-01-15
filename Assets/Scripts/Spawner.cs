using UnityEngine;

public class Spawner : MonoBehaviour {
    public GameObject prefabToSpawn; // Drag your prefab here in the Inspector
    public float spawnInterval = 2.0f; // Seconds between spawns

    void Start() {
        // Repeats the "SpawnObject" function every 'spawnInterval' seconds
        InvokeRepeating("SpawnObject", 0f, spawnInterval);
    }

    void SpawnObject() {
        // Creates the object at the spawner's current position and rotation
        Instantiate(prefabToSpawn, transform.position, transform.rotation);
    }
}

