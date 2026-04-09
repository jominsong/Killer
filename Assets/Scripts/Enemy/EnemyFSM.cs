using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { None = -1, Idle = 0, Wander, Pursuit, Attack }

public class EnemyFSM : MonoBehaviour
{
    [Header("Pursuit")]
    [SerializeField] private float targetRecognitionRange = 8f;
    [SerializeField] private float pursuitLimitRange = 10f;

    [Header("Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackRate = 1f;

    [Header("Drop")]
    [SerializeField] private GameObject[] dropItemPrefabs;
    [Range(0f, 1f)]
    [SerializeField] private float dropProbability = 0.3f;
    [SerializeField] private GameObject coinPrefab;

    private GameObject assignedDropPrefab;

    // ── 상태 ──────────────────────────────────────────────────────────
    private EnemyState enemyState = EnemyState.None;
    private float lastAttackTime;

    // ── 컴포넌트 참조 ─────────────────────────────────────────────────
    private Status status;
    private NavMeshAgent navMeshAgent;
    private Transform target;
    private IEnemyPool enemyPool;

    // ── 코루틴 참조 ───────────────────────────────────────────────────
    private Coroutine stateCoroutine;
    private Coroutine autoWanderCoroutine;

    // Setup() 완료 여부  OnEnable이 Setup보다 먼저 실행되는 타이밍 문제 차단
    private bool isSetupDone = false;

    // =================================================================
    //  초기화
    // =================================================================
    public void Setup(Transform target, IEnemyPool pool)
    {
        status = GetComponent<Status>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        this.target = target;
        this.enemyPool = pool;

        if (navMeshAgent != null)
            navMeshAgent.updateRotation = false;

        // 스폰 시점에 드롭 아이템 랜덤 배정
        AssignRandomDrop();

        isSetupDone = true;
    }

    private void AssignRandomDrop()
    {
        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0)
        {
            assignedDropPrefab = null;
            return;
        }

        // 확률 체크 먼저 드롭 안 하는 적은 null 배정
        if (Random.value > dropProbability)
        {
            assignedDropPrefab = null;
            return;
        }

        // 랜덤으로 하나 배정
        assignedDropPrefab = dropItemPrefabs[Random.Range(0, dropItemPrefabs.Length)];
    }

    private void OnEnable()
    {
        // SetActive(true) 이후 이 시점은 항상 활성 상태 → 코루틴 정상 시작
        if (isSetupDone)
            ChangeState(EnemyState.Idle);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        stateCoroutine = null;
        autoWanderCoroutine = null;
        enemyState = EnemyState.None;

        // 풀 반환 시 경로 초기화 (재활성화 때 이전 경로 잔류 방지)
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();

        // 재활성화 시 새로 배정되도록 초기화
        assignedDropPrefab = null;
    }

    // =================================================================
    //  상태 전이
    // =================================================================
    public void ChangeState(EnemyState newState)
    {
        if (enemyState == newState) return;

        if (stateCoroutine != null)
        {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }

        enemyState = newState;

        stateCoroutine = newState switch
        {
            EnemyState.Idle => StartCoroutine(Idle()),
            EnemyState.Wander => StartCoroutine(Wander()),
            EnemyState.Pursuit => StartCoroutine(Pursuit()),
            EnemyState.Attack => StartCoroutine(Attack()),
            _ => null
        };
    }

    // =================================================================
    //  데미지 / 사망
    // =================================================================
    public void TakeDamage(float damage)
    {
        if (status == null) return;

        bool isDead = status.DecreaseHP(damage);
        if (!isDead) return;

        TryDropItem();
        TryDropCoin();
        enemyPool?.DeactivateEnemy(gameObject);
    }

    private void TryDropItem()
    {
        // 스폰 시 배정된 아이템이 없으면 드롭 안 함
        if (assignedDropPrefab == null) return;

        GameObject dropped = Instantiate(
            assignedDropPrefab,
            transform.position + Vector3.up * 0.5f,
            Quaternion.identity);


        // 드롭 아이템의 weaponType 기준으로 해당 무기군 파츠만 주입
        ItemWeapon weaponItem = dropped.GetComponent<ItemWeapon>();
        if (weaponItem != null && GunSmithManager.Instance != null)
        {
            weaponItem.SetInGameAttachments(
                GunSmithManager.Instance.GetCurrentAttachments(weaponItem.weaponType));
        }
    }

    private void TryDropCoin()
    {
        if (coinPrefab == null || target == null) return;
        GameObject coin = Instantiate(coinPrefab, transform.position + Vector3.up, Quaternion.identity);
        coin.GetComponent<ItemCoin>()?.SetTarget(target);
    }

    // =================================================================
    //  상태 코루틴
    // =================================================================
    private IEnumerator Idle()
    {
        yield return null;
        autoWanderCoroutine = StartCoroutine(AutoChangeFromIdleToWander());

        while (true)
        {
            CalculateDistanceToTargetAndSelectState();
            yield return null;
        }
    }

    private IEnumerator AutoChangeFromIdleToWander()
    {
        yield return new WaitForSeconds(Random.Range(1f, 4f));
        ChangeState(EnemyState.Wander);
    }

    private IEnumerator Wander()
    {
        yield return null;
        if (navMeshAgent == null || status == null) yield break;

        float elapsed = 0f;
        const float maxTime = 10f;

        navMeshAgent.speed = status.WalkSpeed;
        navMeshAgent.SetDestination(CalculateWanderPosition());

        Vector3 dir = navMeshAgent.destination - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        while (true)
        {
            elapsed += Time.deltaTime;

            Vector3 toDestination = navMeshAgent.destination - transform.position;
            toDestination.y = 0f;

            if (toDestination.sqrMagnitude < 0.01f || elapsed >= maxTime)
            {
                ChangeState(EnemyState.Idle);
                yield break;
            }

            CalculateDistanceToTargetAndSelectState();
            yield return null;
        }
    }

    private IEnumerator Pursuit()
    {
        if (navMeshAgent == null || status == null) yield break;

        navMeshAgent.speed = status.RunSpeed;

        while (true)
        {
            if (target != null)
            {
                navMeshAgent.SetDestination(target.position);
                LookRotationToTarget();
            }

            CalculateDistanceToTargetAndSelectState();
            yield return null;
        }
    }

    private IEnumerator Attack()
    {
        if (navMeshAgent == null) yield break;

        if (navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();

        while (true)
        {
            LookRotationToTarget();
            CalculateDistanceToTargetAndSelectState();

            if (Time.time - lastAttackTime > attackRate)
            {
                lastAttackTime = Time.time;

                if (projectilePrefab != null && projectileSpawnPoint != null && target != null)
                {
                    GameObject clone = Instantiate(projectilePrefab,
                        projectileSpawnPoint.position,
                        projectileSpawnPoint.rotation);
                    clone.GetComponent<EnemyProjectile>()?.Setup(target.position);
                }
            }

            yield return null;
        }
    }

    // =================================================================
    //  유틸
    // =================================================================
    private void LookRotationToTarget()
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void CalculateDistanceToTargetAndSelectState()
    {
        if (target == null) return;

        float distance = Vector3.Distance(target.position, transform.position);

        if (distance <= attackRange)
            ChangeState(EnemyState.Attack);
        else if (distance <= targetRecognitionRange)
            ChangeState(EnemyState.Pursuit);
        else if (distance > pursuitLimitRange)
            ChangeState(EnemyState.Wander);
        // targetRecognitionRange < distance <= pursuitLimitRange : 현재 상태 유지
    }

    private Vector3 CalculateWanderPosition()
    {
        const float wanderRadius = 10f;
        Vector3 rangeCenter = Vector3.zero;
        Vector3 rangeScale = Vector3.one * 100f;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 pos = transform.position + new Vector3(
            Mathf.Cos(angle) * wanderRadius, 0f, Mathf.Sin(angle) * wanderRadius);

        pos.x = Mathf.Clamp(pos.x, rangeCenter.x - rangeScale.x * 0.5f, rangeCenter.x + rangeScale.x * 0.5f);
        pos.y = 0f;
        pos.z = Mathf.Clamp(pos.z, rangeCenter.z - rangeScale.z * 0.5f, rangeCenter.z + rangeScale.z * 0.5f);

        return pos;
    }

    // =================================================================
    //  에디터 Gizmo
    // =================================================================
    private void OnDrawGizmos()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, navMeshAgent.destination - transform.position);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetRecognitionRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pursuitLimitRange);
        Gizmos.color = new Color(0.39f, 0.04f, 0.04f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}