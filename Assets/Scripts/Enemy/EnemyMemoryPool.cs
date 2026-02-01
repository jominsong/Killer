using System.Collections;
using UnityEngine;

public class EnemyMemoryPool : MonoBehaviour
{
    [Header("Enemy Spawn")]
    [SerializeField]
    private Transform target;  // 적의 목표 (플레이어)
    [SerializeField]
    private GameObject enemySpawnPointPrefab;  // 적이 등장하기 전 적의 등장 위치를 알려주는 프리팹
    [SerializeField]
    private GameObject enemyPrefab;  // 생성되는 적 프리팹
    [SerializeField]
    private float enemySpawnTime = 1;  // 적 생성 주기
    [SerializeField]
    private float enemySpawnLatency = 1;  // 타일 생성 후 적이 등장하기까지 대기 시간

    [Header("Spawn Limit")]
    [SerializeField]
    private int maxEnemyCount = 30;  // 월드에 존재할 수 있는 최대 적 숫자
    private int currentEnemyCount = 0;  // 현재 활성화 된 적 숫자

    private MemoryPool spawnPointMemoryPool;  // 적 등장 위치를 알려주는 오브젝트 생성, 활성/비활성 관리
    private MemoryPool enemyMemoryPool;  // 적 생성, 활성/비활성 관리

    private int numberOfEnemiesSpawnedAtOnce = 1;  // 동시에 생성되는 적의 숫자
    private Vector2Int mapSize = new Vector2Int(100, 100);  // 맵 크기

    private void Awake()
    {
        spawnPointMemoryPool = new MemoryPool(enemySpawnPointPrefab);
        enemyMemoryPool = new MemoryPool(enemyPrefab);

        StartCoroutine("SpawnTile");
    }

    private IEnumerator SpawnTile()
    {
        int currentNumber = 0;
        int maximumNumber = 30;

        while (true)
        {
            // 현재 적 숫자가 최대치보다 적을 때만 스폰 시도
            if (currentEnemyCount < maxEnemyCount)
            {
                for (int i = 0; i < numberOfEnemiesSpawnedAtOnce; ++i)
                {
                    // 루프 도중에도 최대치에 도달하면 중단
                    if (currentEnemyCount >= maxEnemyCount) break;

                    GameObject item = spawnPointMemoryPool.ActivatePoolItem();
                    item.transform.position = new Vector3(Random.Range(-mapSize.x * 0.49f, mapSize.x * 0.49f),
                                            1, Random.Range(-mapSize.y * 0.49f, mapSize.y * 0.49f));

                    StartCoroutine("SpawnEnemy", item);

                    // 스폰 예정인 상태도 카운트에 포함 (중복 스폰 방지)
                    currentEnemyCount++;
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

        // 적 오브젝트를 생성하고, 적의 위치를 point의 위치로 설정
        GameObject item = enemyMemoryPool.ActivatePoolItem();
        item.transform.position = point.transform.position;

        item.GetComponent<EnemyFSM>().Setup(target,this);

        // 타일 오브젝트를 비활성화
        spawnPointMemoryPool.DeactivatePoolItem(point);
    }

    public void DeactivateEnemy(GameObject enemy)
    {
        enemyMemoryPool.DeactivatePoolItem(enemy);

        currentEnemyCount--;

        if (currentEnemyCount < 0) currentEnemyCount = 0;
    }
}
