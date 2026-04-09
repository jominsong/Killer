using System.Collections;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private MovementTransform movement;
    private float projectileDistance = 30f;  // 최대 사거리
    private int damage = 5;  // 데미지

    // 이동 방향 (Raycast에 사용)
    private Vector3 moveDirection;

    // 탄환이 맞혀선 안 되는 레이어 (적 자신, 트리거 전용 콜라이더 등)
    // 인스펙터에서 설정하거나 코드에서 직접 지정
    [SerializeField] private LayerMask hitLayerMask = Physics.DefaultRaycastLayers;

    public void Setup(Vector3 targetPosition)
    {
        movement = GetComponent<MovementTransform>();
        moveDirection = (targetPosition - transform.position).normalized;
        movement.MoveTo(moveDirection);

        StartCoroutine(OnMove());
    }

    private IEnumerator OnMove()
    {
        Vector3 start = transform.position;
        Vector3 prevPosition = transform.position;

        while (true)
        {
            // 이전 프레임 위치와 현재 프레임 위치 사이를 Raycast로 검사
            Vector3 currentPosition = transform.position;
            float stepDistance = Vector3.Distance(prevPosition, currentPosition);

            if (stepDistance > 0f)
            {
                if (Physics.Raycast(prevPosition, moveDirection, out RaycastHit hit, stepDistance, hitLayerMask))
                {
                    OnHit(hit.collider, hit.point);
                    yield break;
                }
            }

            prevPosition = currentPosition;

            // 최대 사거리 초과 시 제거
            if (Vector3.Distance(currentPosition, start) >= projectileDistance)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }

    private void OnHit(Collider other, Vector3 hitPoint)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeDamage(damage);
        }

        // 플레이어든 벽이든 닿으면 제거
        Destroy(gameObject);
    }

    // OnTriggerEnter는 보조 수단으로 유지 (느린 탄환이나 큰 콜라이더 대비)
    private void OnTriggerEnter(Collider other)
    {
        // 이미 Raycast로 처리됐을 수 있으므로 null 체크
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

}
