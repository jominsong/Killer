using System.Collections;
using UnityEngine;

/// <summary>
/// 기존 EnemyMemoryPool  IEnemyPool 인터페이스 구현 추가.
/// WaveEnemySpawner를 사용하지 않고 기존 방식을 유지할 경우 사용.
/// </summary>
public class EnemyMemoryPool : MonoBehaviour, IEnemyPool
{
    [Header("Enemy Spawn")]
    [SerializeField] private Transform target;
    [SerializeField] private GameObject enemySpawnPointPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float enemySpawnTime = 1f;
    [SerializeField] private float enemySpawnLatency = 1f;

    [Header("Spawn Limit")]
    [SerializeField] private int maxEnemyCount = 30;

    private MemoryPool spawnPointMemoryPool;
    private MemoryPool enemyMemoryPool;

    private int currentEnemyCount = 0;
    private int numberOfEnemiesSpawnedAtOnce = 1;
    private readonly Vector2Int mapSize = new Vector2Int(100, 100);

    private void Awake()
    {
        spawnPointMemoryPool = new MemoryPool(enemySpawnPointPrefab);
        enemyMemoryPool = new MemoryPool(enemyPrefab);

        StartCoroutine(SpawnTile());
    }

    private IEnumerator SpawnTile()
    {
        int currentNumber = 0;
        const int maximumNumber = 30;

        while (true)
        {
            if (currentEnemyCount < maxEnemyCount)
            {
                for (int i = 0; i < numberOfEnemiesSpawnedAtOnce; i++)
                {
                    if (currentEnemyCount >= maxEnemyCount) break;

                    GameObject item = spawnPointMemoryPool.ActivatePoolItem();
                    item.transform.position = new Vector3(
                        Random.Range(-mapSize.x * 0.49f, mapSize.x * 0.49f),
                        1f,
                        Random.Range(-mapSize.y * 0.49f, mapSize.y * 0.49f)
                    );

                    StartCoroutine(SpawnEnemy(item));
                    currentEnemyCount++;  // 스폰 예약 포함 카운트
                }
            }

            currentNumber++;
            if (currentNumber >= maximumNumber)
            {
                currentNumber = 0;
                numberOfEnemiesSpawnedAtOnce++;
            }

            yield return new WaitForSeconds(enemySpawnTime);
        }
    }

    private IEnumerator SpawnEnemy(GameObject point)
    {
        yield return new WaitForSeconds(enemySpawnLatency);

        GameObject item = enemyMemoryPool.ActivatePoolItem();
        item.transform.position = point.transform.position;
        item.GetComponent<EnemyFSM>().Setup(target, this);

        spawnPointMemoryPool.DeactivatePoolItem(point);
    }

    // IEnemyPool 구현
    public void DeactivateEnemy(GameObject enemy)
    {
        enemyMemoryPool.DeactivatePoolItem(enemy);
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
    }
}