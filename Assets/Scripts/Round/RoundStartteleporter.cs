using UnityEngine;

/// <summary>
/// 상점 출구 문.
/// 플레이어가 F키로 상호작용 → 다음 라운드 맵으로 텔레포트 + 다음 라운드 시작.
///
/// 씬 셋업:
///   1. 상점 출구 오브젝트에 이 스크립트 추가
///   2. Layer를 InteractionSystem의 itemLayer와 동일하게 설정
///   3. BoxCollider 추가
///   4. nextRoundSpawnPoint : 라운드 맵 플레이어 시작 위치 빈 오브젝트
/// </summary>
public class ShopExitTeleporter : InteractionBase
{
    [Header("Teleport")]
    [Tooltip("다음 라운드 맵의 플레이어 시작 위치 빈 오브젝트")]
    [SerializeField] private Transform nextRoundSpawnPoint;

    // 중복 입력 방지
    private bool isTransitioning = false;

    // InteractionSystem이 F키 눌렸을 때 호출
    public override void Use(GameObject entity)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        MapManager.Instance.LoadRandomMap();

        // 새 맵의 PlayerSpawnPoint로 텔레포트
        Transform spawnPoint = MapManager.Instance.CurrentPlayerSpawnPoint;
        if (spawnPoint != null)
        {
            CharacterController cc = entity.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            entity.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            if (cc != null) cc.enabled = true;
        }

        int nextIndex = RoundManager.Instance.CurrentRoundIndex + 1;
        RoundManager.Instance.StartRound(nextIndex);

        isTransitioning = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (nextRoundSpawnPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(nextRoundSpawnPoint.position, 0.4f);
        Gizmos.DrawLine(transform.position, nextRoundSpawnPoint.position);
    }
}