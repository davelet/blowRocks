using UnityEngine;

/// <summary>
/// Spawns asteroids at random positions around the screen edges.
/// Difficulty ramps up over time.
/// </summary>
public class AsteroidSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] asteroidPrefabs; // large, medium, small
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float spawnIntervalDecrease = 0.05f; // per wave
    [SerializeField] private float spawnDistance = 12f;           // from screen center

    [Header("Wave Settings")]
    [SerializeField] private int asteroidsPerWave = 1;
    [SerializeField] private int maxAsteroidsPerWave = 5;

    private Camera mainCamera;
    private float nextSpawnTime;
    private bool isSpawning = false;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!isSpawning) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnWave();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void StartSpawning()
    {
        isSpawning = true;
        nextSpawnTime = Time.time + spawnInterval;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    public void ResetDifficulty()
    {
        spawnInterval = 2f;
        asteroidsPerWave = 1;
    }

    private void SpawnWave()
    {
        for (int i = 0; i < asteroidsPerWave; i++)
        {
            SpawnAsteroid();
        }

        // Ramp difficulty
        spawnInterval = Mathf.Max(minSpawnInterval, spawnInterval - spawnIntervalDecrease);
        if (asteroidsPerWave < maxAsteroidsPerWave)
        {
            asteroidsPerWave++;
        }
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0) return;

        // Pick a random asteroid size (index 0 = largest)
        int index = Random.Range(0, asteroidPrefabs.Length);
        if (asteroidPrefabs[index] == null) return;

        // Spawn at random edge position
        Vector2 spawnPos = GetRandomEdgePosition();
        Instantiate(asteroidPrefabs[index], spawnPos, Quaternion.identity);
    }

    private Vector2 GetRandomEdgePosition()
    {
        // Random angle around the screen
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(
            Mathf.Cos(angle) * spawnDistance,
            Mathf.Sin(angle) * spawnDistance
        );
    }
}
