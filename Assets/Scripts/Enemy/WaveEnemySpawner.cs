using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// 씬 셋업
///   1. 빈 GameObject에 WaveEnemySpawner 컴포넌트 추가
///   2. enemyPrefab / spawnPointMarkerPrefab 연결
///   3. playerTarget 연결
///   4. spawnPoints 리스트에 스폰 위치 Transform 드래그
///      (또는 런타임에 RegisterSpawnPoint() 호출)
/// </summary>
public class WaveEnemySpawner : MonoBehaviour, IEnemyPool
{
    // ─── 인스펙터 ────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Transform playerTarget;

    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject spawnPointMarkerPrefab;

    [Header("Spawn Points")]
    [Tooltip("씬에 배치된 스폰 포인트 Transform 목록")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Pool Settings")]
    [SerializeField] private int initialEnemyPoolSize = 30;
    [SerializeField] private int initialMarkerPoolSize = 10;

    [Header("Safe Distance")]
    [Tooltip("플레이어로부터 이 거리 이내의 스폰 포인트는 사용하지 않음")]
    [SerializeField] private float minDistanceFromPlayer = 10f;

    // ─── 풀 ──────────────────────────────────────────────────────────
    private readonly Queue<GameObject> enemyPool = new Queue<GameObject>();
    private readonly Queue<GameObject> markerPool = new Queue<GameObject>();
    private readonly HashSet<GameObject> liveEnemies = new HashSet<GameObject>();

    // ─── 스폰 제어 ───────────────────────────────────────────────────
    private bool isSpawning;
    private Coroutine spawnLoopCoroutine;
    private RoundData roundData;

    // =================================================================
    //  Unity 생명주기
    // =================================================================
    private void Awake()
    {
        BuildPool(markerPool, spawnPointMarkerPrefab, initialMarkerPoolSize);
    }

    private void Start()
    {
        // Start()는 모든 오브젝트의 Awake()가 끝난 후 실행 보장
        // OnEnable()에서 하면 RoundManager.Instance가 null일 수 있음
        if (RoundManager.Instance == null)
        {
            Debug.LogError("[WaveEnemySpawner] RoundManager.Instance가 null! 씬에 RoundManager가 있는지 확인하세요.");
            return;
        }
        RoundManager.Instance.OnRoundStarted += OnRoundStarted;
        RoundManager.Instance.OnRoundTimeUp += OnRoundTimeUp;
        RoundManager.Instance.OnRoundCompleted += OnRoundCompleted;
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance == null) return;
        RoundManager.Instance.OnRoundStarted -= OnRoundStarted;
        RoundManager.Instance.OnRoundTimeUp -= OnRoundTimeUp;
        RoundManager.Instance.OnRoundCompleted -= OnRoundCompleted;
    }

    // =================================================================
    //  RoundManager 이벤트 핸들러
    // =================================================================
    private void OnRoundStarted(int index, RoundData data)
    {
        roundData = data;

        // 적 풀이 비어있으면 이 시점(NavMesh 베이크 완료 후)에 생성
        if (enemyPool.Count == 0)
            BuildPool(enemyPool, enemyPrefab, initialEnemyPoolSize);

        isSpawning = true;
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnRoundTimeUp(int index)
    {
        isSpawning = false;
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }
    }

    private void OnRoundCompleted(int index)
    {
        // 다음 라운드 전 잔존 적 강제 회수
        DeactivateAllEnemies();
    }

    // =================================================================
    //  스폰 루프
    // =================================================================
    private IEnumerator SpawnLoop()
    {
        while (isSpawning)
        {
            int available = roundData.maxEnemiesAlive - RoundManager.Instance.AliveEnemyCount;

            if (available > 0)
            {
                int count = Mathf.Min(roundData.spawnCountPerTick, available);
                for (int i = 0; i < count; i++)
                {
                    Transform pt = PickRandomSpawnPoint();
                    if (pt != null)
                        StartCoroutine(SpawnSequence(pt.position));
                }
            }

            yield return new WaitForSeconds(roundData.spawnInterval);
        }
    }

    /// <summary>마커 등장 → latency 대기 → 실제 적 출현</summary>
    private IEnumerator SpawnSequence(Vector3 worldPosition)
    {
        // 마커 표시
        GameObject marker = Pull(markerPool, spawnPointMarkerPrefab);
        marker.transform.position = worldPosition;
        marker.SetActive(true);

        yield return new WaitForSeconds(roundData.spawnLatency);

        marker.SetActive(false);
        markerPool.Enqueue(marker);

        // 딜레이 중 라운드 종료 시 적 스폰 취소
        if (!isSpawning) yield break;

        // 실제 적 출현
        GameObject enemy = Pull(enemyPool, enemyPrefab);
        enemy.transform.position = worldPosition;
        enemy.GetComponent<EnemyFSM>().Setup(playerTarget, this);
        enemy.SetActive(true);

        //enemy.GetComponent<EnemyFSM>().Setup(playerTarget, this);

        liveEnemies.Add(enemy);
        RoundManager.Instance.RegisterEnemySpawned();
    }

    // =================================================================
    //  IEnemyPool 구현
    // =================================================================
    public void DeactivateEnemy(GameObject enemy)
    {
        if (!liveEnemies.Contains(enemy)) return;

        liveEnemies.Remove(enemy);
        enemy.SetActive(false);
        enemyPool.Enqueue(enemy);

        RoundManager.Instance.RegisterEnemyDeactivated();
    }

    // =================================================================
    //  스폰 포인트 동적 관리
    // =================================================================
    public void RegisterSpawnPoint(Transform point) => spawnPoints.Add(point);
    public void UnregisterSpawnPoint(Transform point) => spawnPoints.Remove(point);

    // =================================================================
    //  내부 유틸
    // =================================================================
    private void BuildPool(Queue<GameObject> pool, GameObject prefab, int size)
    {
        if (prefab == null) return;
        for (int i = 0; i < size; i++)
        {
            GameObject o = Instantiate(prefab, transform);
            o.SetActive(false);
            pool.Enqueue(o);
        }
    }

    /// <summary>풀에 없으면 자동 확장 (예비 방어)</summary>
    private GameObject Pull(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count > 0) return pool.Dequeue();
        GameObject o = Instantiate(prefab, transform);
        o.SetActive(false);
        return o;
    }

    public void ReplaceSpawnPoints(List<Transform> newPoints)
    {
        spawnPoints.Clear();
        spawnPoints.AddRange(newPoints);
        Debug.Log($"[WaveEnemySpawner] 스폰포인트 {spawnPoints.Count}개로 교체됨");
    }

    private Transform PickRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[WaveEnemySpawner] 스폰 포인트가 없습니다!");
            return null;
        }

        // 플레이어와 충분히 먼 스폰 포인트만 추림
        List<Transform> validPoints = new List<Transform>();
        foreach (Transform pt in spawnPoints)
        {
            if (pt == null) continue;
            float dist = Vector3.Distance(pt.position, playerTarget.position);
            if (dist >= minDistanceFromPlayer)
                validPoints.Add(pt);
        }

        // 유효 포인트가 없으면 가장 먼 포인트 강제 선택
        if (validPoints.Count == 0)
        {
            Transform farthest = spawnPoints[0];
            float maxDist = 0f;
            foreach (Transform pt in spawnPoints)
            {
                if (pt == null) continue;
                float d = Vector3.Distance(pt.position, playerTarget.position);
                if (d > maxDist) { maxDist = d; farthest = pt; }
            }
            return farthest;
        }

        return validPoints[Random.Range(0, validPoints.Count)];
    }

    public void DeactivateAllEnemies()
    {
        foreach (GameObject e in liveEnemies)
        {
            if (e == null) continue;
            e.SetActive(false);
            enemyPool.Enqueue(e);
            RoundManager.Instance.RegisterEnemyDeactivated();
        }
        liveEnemies.Clear();
    }
}