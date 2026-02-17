using UnityEngine;
using System.Collections;

public class ItemCoin : ItemBase
{
    [SerializeField]
    private int coinValue = 1;  // 재화 가치
    [SerializeField]
    private float magntRange = 2f;  // 끌어당길 범위
    [SerializeField]
    private float moveSpeed = 10f;  // 끌려오는 속도

    private Transform playerTransform;
    private bool isFollowing = false;

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 일정 거리 안에 들어오면 플레이어에게 추적 시작
        if (distance <= magntRange) isFollowing = true;

        if (isFollowing)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position 
                + Vector3.up, moveSpeed * Time.deltaTime);
        }
    }

    public override void Use(GameObject entity)
    {
        if (entity == null) return;

        //  플레이어의 재화 데이터 증가
        PlayerInventory inventory = entity.GetComponent<PlayerInventory>();

        if (inventory != null)
        {
            inventory.AddCoins(coinValue);
            Destroy(gameObject);
        }
    }

    // 플레이어 참조를 위해 초기화
    public void SetTarget(Transform target) => playerTransform = target;
}
