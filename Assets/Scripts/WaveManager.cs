using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject[] EnemyPrefabs;
    public Transform[] SpawnPoints;
    public float TimeBeforeFirstWave = 3f;
    public int EnemiesOnFirstWave = 2;
    public int EnemiesAddedPerWave = 3;

    private int _currentWave = 0;
    private int _enemiesAlive = 0;

    private void Start()
    {
        StartCoroutine(StartFirstWave());
    }

    private IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(TimeBeforeFirstWave);
        SpawnWave();
    }

    private void SpawnWave()
    {
        _currentWave++;
        int enemiesToSpawn = EnemiesOnFirstWave + (_currentWave - 1) * EnemiesAddedPerWave;
        _enemiesAlive = enemiesToSpawn;

        Debug.Log($"Wave {_currentWave} — {enemiesToSpawn} enemies");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = SpawnPoints[i % SpawnPoints.Length];
            GameObject prefab = EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
            Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    public void OnEnemyDied()
    {
        _enemiesAlive--;
        if (_enemiesAlive <= 0)
            SpawnWave();
    }
}