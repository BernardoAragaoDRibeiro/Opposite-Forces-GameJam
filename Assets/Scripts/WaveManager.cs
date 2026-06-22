using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Configuração")]
    public GameObject[] EnemyPrefabs;
    public Transform[]  SpawnPoints;
    public float TimeBeforeFirstWave  = 3f;
    public int   EnemiesOnFirstWave   = 5;
    public int   EnemiesAddedPerWave  = 3;
    public int   MaxEnemiesOnScreen   = 150;
    public float TimeBetweenSpawns    = 0.2f; // delay entre cada spawn para não spawnar tudo de uma vez

    private int   _currentWave    = 0;
    private int   _enemiesAlive   = 0;
    private int   _waveTarget     = 0; // quantos inimigos essa wave deve ter no máximo

    private void Start()
    {
        StartCoroutine(StartFirstWave());
    }

    private IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(TimeBeforeFirstWave);
        StartNextWave();
    }

    private void StartNextWave()
    {
        _currentWave++;
        _waveTarget = Mathf.Min(
            EnemiesOnFirstWave + (_currentWave - 1) * EnemiesAddedPerWave,
            MaxEnemiesOnScreen
        );

        // Spawna até o target atual
        int toSpawn = _waveTarget - _enemiesAlive;
        if (toSpawn > 0)
            StartCoroutine(SpawnBatch(toSpawn));
    }

    private IEnumerator SpawnBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(TimeBetweenSpawns);
        }
    }

    private void SpawnOne()
    {
        if (SpawnPoints.Length == 0 || EnemyPrefabs.Length == 0) return;

        Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        GameObject prefab    = EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        _enemiesAlive++;
    }

    // Chame isso no script do inimigo quando ele morrer
    public void OnEnemyDied()
    {
        _enemiesAlive--;

        // Se ainda não chegou ao target da wave atual, spawna outro imediatamente
        if (_enemiesAlive < _waveTarget)
        {
            SpawnOne();
        }
        // Se o target foi atingido e todos morreram, começa próxima wave
        else if (_enemiesAlive <= 0)
        {
            StartNextWave();
        }
    }
}