using UnityEngine;
using System.Collections.Generic;

public class GunSmithManager : MonoBehaviour
{
    public static GunSmithManager Instance;

    [Header("UI References")]
    [SerializeField]
    private GameObject gunSmithCanvas;  // 건스미스 전체 Ui 판넬
    [SerializeField]
    private GameObject partSelectionUI;  // 하단 3택 카드 Ui 판넬
    [SerializeField]
    private WeaponPartCard[] partCards;  // 하단 카드 3개를 드래그 앤 드롭
    [SerializeField]
    private PlayerInventory playerInventory;  // 플레이어 인벤토리 참조

    [Header("Camera Settings")]
    [SerializeField]
    private Camera gunSmithCamera;  // 총기를 비출 전용 카메라

    [Header("Item Sync")]
    [SerializeField]
    private ItemWeaponAssaultRifle assaultRifleItemPrefab;

    private GameObject playerEntity;
    private WeaponBase targetWeapon;
    private WeaponModifier targetModifier;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (gunSmithCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseGunSmith();
        }
    }

    // Workbench에서 호출할 진입 함수
    public void OpenGunSmith(GameObject player, WeaponBase weapon)
    {
        playerEntity = player;
        targetWeapon = weapon;

        // 무기에 Modifier가 있는지 확인 및 설정
        targetModifier = weapon.GetComponent<WeaponModifier>();
        if (targetModifier == null)
        {
            targetModifier = weapon.gameObject.AddComponent<WeaponModifier>();
            targetModifier.Setup(weapon);
        }

        weapon.SetupModifier(targetModifier);

        // 플레이어 조작 정지
        SetPlayerControl(false);

        // Ui 활성화
        gunSmithCanvas.SetActive(true);
        partSelectionUI.SetActive(false);  // 처음엔 슬룻 선택창만 
    }

    public void CloseGunSmith()
    {
        if (targetModifier != null)
        {
            List<WeaponAttachment> currentParts = targetModifier.GetCurrentAttachments();

            ItemWeaponAssaultRifle[] worldItems = FindObjectsByType<ItemWeaponAssaultRifle>(FindObjectsSortMode.None);
            foreach (ItemWeaponAssaultRifle item in worldItems)
            {
                item.SetInGameAttachments(currentParts);
            }

            // 아이템 프리팹의 리스트를 실시간으로 업데이트
            assaultRifleItemPrefab.SetInGameAttachments(currentParts);
        }

        SetPlayerControl(true);
        gunSmithCanvas.SetActive(false);
        partSelectionUI.SetActive(false);
    }

    private void SetPlayerControl(bool state)
    {
        // 커서 상태 제어
        Cursor.visible = !state;
        Cursor.lockState = state ? CursorLockMode.Locked : CursorLockMode.None;

        // 플레이어 이동 및 사격 중지 로직
        playerEntity.GetComponent<PlayerController>().enabled = state;
    }

    public void OnSlotClicked(int slotIndex)
    {
        AttachmentSlot selectedSlot = (AttachmentSlot)slotIndex;

        // 하단 카드 Ui 활성화
        partSelectionUI.SetActive(true);

        // AttachmentPool에서 랜덤 3개 가져오기
        List<WeaponAttachment> randomParts = AttachmentPool.instance.GetRandomAttachments(selectedSlot, 3);

        // Ui 카드들에 데이터 전달
        for (int i = 0; i < partCards.Length; i++)
        {
            if (i < randomParts.Count)
            {
                partCards[i].gameObject.SetActive(true);
                bool canAfford = playerInventory.CurrentCoins >= randomParts[i].cost;
                partCards[i].SetupCard(randomParts[i],canAfford);
            }
            else
            {
                partCards[i].gameObject.SetActive(false);
            }
        }
    }

    // 실제 구매 처리
    public void PurchaseAttachment(WeaponAttachment attachment)
    {
        if (playerInventory.ConsumeCoins(attachment.cost))
        {
            targetModifier.AddAttachment(attachment);

            // 구매 후 Ui 닫기
            partSelectionUI.SetActive(false);
        }
    }

    public void CancelSelection()
    {
        partSelectionUI.SetActive(false);
    }
}
