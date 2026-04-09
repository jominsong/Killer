using UnityEngine;

/// <summary>
/// 라운드 종료 시 열리는 상점 입구 문.
/// InteractionBase를 상속 → F키 상호작용으로 텔레포트.
/// 
/// 씬 셋업:
///   1. 문 오브젝트에 이 스크립트 추가
///   2. Layer를 InteractionSystem의 itemLayer와 동일하게 설정
///   3. BoxCollider 추가 (IsTrigger 불필요 - SphereCast 감지용)
///   4. shopSpawnPoint : 상점 안 도착 위치 빈 오브젝트
///   5. doorVisual : 평소에 숨길 메시 오브젝트 (없으면 이 오브젝트 자체)
/// </summary>
public class RoundEndTeleporter : InteractionBase
{
    [Header("Teleport")]
    [Tooltip("상점 안 도착 위치 빈 오브젝트")]
    [SerializeField] private Transform shopSpawnPoint;

    [Header("Visual")]
    [Tooltip("라운드 종료 전엔 숨길 메시 오브젝트 (비워두면 MeshRenderer 자동 탐색)")]
    [SerializeField] private GameObject doorVisual;

    // 문이 열려 있는지 여부 - 닫혀있으면 Use() 무시
    private bool isOpen = false;

    private void Start()
    {
        if (RoundManager.Instance == null)
        {
            Debug.LogError("[RoundEndTeleporter] RoundManager가 씬에 없습니다!");
            return;
        }

        RoundManager.Instance.OnRoundTimeUp += OnRoundTimeUp;
        RoundManager.Instance.OnRoundStarted += OnRoundStarted;

        // 시작 시 닫힌 상태로 초기화
        SetOpen(false);
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance == null) return;
        RoundManager.Instance.OnRoundTimeUp -= OnRoundTimeUp;
        RoundManager.Instance.OnRoundStarted -= OnRoundStarted;
    }

    // 라운드 종료 → 문 열기
    private void OnRoundTimeUp(int roundIndex)
    {
        SetOpen(true);
        Debug.Log("[RoundEndTeleporter] 상점 문 열림!");
    }

    // 새 라운드 시작 → 문 닫기 (상점에서 나와 다음 라운드 시작 시 초기화)
    private void OnRoundStarted(int roundIndex, RoundData data)
    {
        SetOpen(false);
    }

    // InteractionSystem이 F키 눌렸을 때 호출
    public override void Use(GameObject entity)
    {
        if (!isOpen)
        {
            Debug.Log("[RoundEndTeleporter] 아직 라운드가 끝나지 않았습니다.");
            return;
        }

        Teleport(entity);
    }

    private void Teleport(GameObject player)
    {
        // shopSpawnPoint가 비어있으면 ShopManager에서 가져옴
        Transform spawnPoint = shopSpawnPoint != null
        ? shopSpawnPoint
        : ShopManager.Instance?.ShopSpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogWarning("[RoundEndTeleporter] ShopSpawnPoint를 찾을 수 없습니다!");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        if (cc != null) cc.enabled = true;

        RoundManager.Instance?.PlayerExitedRound();
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        // 비주얼만 토글 (오브젝트 자체는 항상 활성 상태 유지 → SphereCast 감지 유지)
        GameObject visual = doorVisual != null ? doorVisual : gameObject;
        if (visual != gameObject) // 자기 자신이면 건드리지 않음
            visual.SetActive(open);
    }

    private void OnDrawGizmosSelected()
    {
        if (shopSpawnPoint == null) return;
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawSphere(shopSpawnPoint.position, 0.4f);
        Gizmos.DrawLine(transform.position, shopSpawnPoint.position);
    }
}