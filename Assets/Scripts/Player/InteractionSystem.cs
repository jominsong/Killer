using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField]
    private float interactDistance = 3f;  // 상호작용 가능거리
    [SerializeField]
    private float interactRadius = 0.5f; // 감지 범위 두께
    [SerializeField]
    private LayerMask itemLayer;  // 아이템 레이어

    private PlayerHUD playerHUD;

    private void Awake()
    {
        playerHUD = Object.FindAnyObjectByType<PlayerHUD>();
        if (playerHUD != null ) playerHUD.gameObject.SetActive(false);
    }

    private void Update()
    {
        CheckInteraction();
    }

    private void CheckInteraction()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.SphereCast(ray, interactRadius, out hit, interactDistance, itemLayer))
        {
            ItemBase item = hit.collider.GetComponent<ItemBase>();
            if (item != null)
            {
                playerHUD.SetInteractionText(true, "F키를 눌러 습득");

                if (Input.GetKeyDown(KeyCode.F))
                {
                    item.Use(gameObject);
                }
                return;
            }
        }

        // 감지된 아이템이 없으면 UI 끄기
        playerHUD.SetInteractionText(false);
    }

}


