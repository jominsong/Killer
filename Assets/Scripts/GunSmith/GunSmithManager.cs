using UnityEngine;
using System.Collections.Generic;

public enum GunSmithState
{
    WeaponTypeSelect,  // 무기군 선택
    CardSelect,        // 카드 3개 표시
    PostPurchase       // 구매 완료
}

public class GunSmithManager : MonoBehaviour
{
    public static GunSmithManager Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject gunSmithCanvas;
    [SerializeField] private GameObject weaponTypeSelectPanel;  // 무기군 선택
    [SerializeField] private GameObject cardSelectPanel;        // 카드 3개
    [SerializeField] private GameObject postPurchasePanel;      // 다시 구매하기

    [Header("Card References")]
    [SerializeField] private WeaponPartCard[] partCards;        // 카드 3개

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private ItemWeapon weaponPrefab;

    [Header("Weapon Display")]
    [SerializeField] private GunSmithWeaponDisplay weaponDisplay;

    [Header("Camera")]
    [SerializeField] private GunSmithCameraController cameraController;

    // ── 내부 상태 ──────────────────────────────────────────────
    private GunSmithState currentState;
    private GameObject playerEntity;
    private WeaponType selectedWeaponType;   // 선택된 무기군
    private AttachmentSlot selectedSlot;     // 선택된 슬롯

    // 무기군별 Modifier 캐시 (무기군 해당 무기의 Modifier)
    private Dictionary<WeaponType, WeaponModifier> modifierCache
        = new Dictionary<WeaponType, WeaponModifier>();

    private WeaponModifier currentModifier;  // 현재 작업 중인 Modifier

    private void Awake() => Instance = this;

    private void Update()
    {
        if (gunSmithCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    // =================================================================
    //  진입 / 종료
    // =================================================================
    public void OpenGunSmith(GameObject player)
    {
        playerEntity = player;

        // 소지 무기들의 Modifier 캐시 갱신
        RebuildModifierCache(player);

        SetPlayerControl(false);
        gunSmithCanvas.SetActive(true);

        // 페이드와 함께 카메라 전환 → 완료 후 UI 표시
        cameraController.EnterGunSmith(onComplete: () =>
        {
            gunSmithCanvas.SetActive(true);
            ChangeState(GunSmithState.WeaponTypeSelect);
        });
    }

    public void CloseGunSmith()
    {
        gunSmithCanvas.SetActive(false);
        weaponDisplay.HideWeapon();

        // 모든 무기 스탯 최종 업데이트
        foreach (var pair in modifierCache)
        {
            WeaponBase weapon = GetWeaponByType(pair.Key);
            if (weapon != null) weapon.UpdateMod();
        }

        // 월드 아이템 동기화
        SyncWorldItems();

        cameraController.ExitGunSmith(onComplete: () =>
        {
            SetPlayerControl(true);
        });
    }

    // =================================================================
    //  상태 전환
    // =================================================================
    private void ChangeState(GunSmithState newState)
    {
        currentState = newState;

        // 패널 전환 해당 상태 패널만 활성화
        weaponTypeSelectPanel.SetActive(newState == GunSmithState.WeaponTypeSelect);
        cardSelectPanel.SetActive(newState == GunSmithState.CardSelect);
        postPurchasePanel.SetActive(newState == GunSmithState.PostPurchase);
    }

    // =================================================================
    //  무기군 선택 (WeaponTypeSelectPanel 버튼에서 호출)
    // =================================================================
    public void OnWeaponTypeClicked(int typeIndex)
    {
        selectedWeaponType = (WeaponType)typeIndex;

        modifierCache.TryGetValue(selectedWeaponType, out currentModifier);

        if (weaponDisplay == null)
        {
            Debug.LogError("[GunSmith] weaponDisplay가 null입니다! Inspector에서 연결하세요.");
            return;
        }

        Debug.Log($"[GunSmith] ShowWeapon 호출: {selectedWeaponType}");
        weaponDisplay.ShowWeapon(selectedWeaponType, currentModifier);

        ShowCards();  // 바로 카드 패널
    }

    // =================================================================
    //  슬롯 선택 (SlotSelectPanel 버튼에서 호출)
    // =================================================================
    public void OnSlotClicked(int slotIndex)
    {
        selectedSlot = (AttachmentSlot)slotIndex;

        // AttachmentPool에서 해당 슬롯 + 해당 무기군 파츠 랜덤 3개
        List<WeaponAttachment> randomParts =
            AttachmentPool.instance.GetRandomAttachments(selectedWeaponType, 3);

        // 카드 UI 세팅
        for (int i = 0; i < partCards.Length; i++)
        {
            if (i < randomParts.Count)
            {
                partCards[i].gameObject.SetActive(true);
                bool canAfford = playerInventory.CurrentCoins >= randomParts[i].cost;
                partCards[i].SetupCard(randomParts[i], canAfford);
            }
            else
            {
                partCards[i].gameObject.SetActive(false);
            }
        }

        ChangeState(GunSmithState.CardSelect);
    }

    // =================================================================
    //  구매 처리 (WeaponPartCard에서 호출)
    // =================================================================
    public void PurchaseAttachment(WeaponAttachment attachment)
    {
        if (!playerInventory.ConsumeCoins(attachment.cost)) return;

        // currentModifier가 없으면 임시 저장소 역할할 modifier 생성
        if (currentModifier == null)
        {
            GameObject modHost = new GameObject($"TempMod_{selectedWeaponType}");
            modHost.transform.SetParent(transform);
            WeaponModifier tempMod = modHost.AddComponent<WeaponModifier>();

            // 임시 무기군 정보 설정
            tempMod.SetWeaponType(selectedWeaponType);

            modifierCache[selectedWeaponType] = tempMod;
            currentModifier = tempMod;
            weaponDisplay.UpdateModifier(currentModifier);
        }

        currentModifier.AddAttachment(attachment);

        // 무기가 있으면 즉시 반영, 없으면 스킵 (나중에 무기 픽업 시 반영)
        WeaponBase weapon = GetWeaponByType(selectedWeaponType);
        if (weapon != null)
            weapon.UpdateMod();

        weaponDisplay.SyncAttachments();
        ChangeState(GunSmithState.PostPurchase);
    }

    // =================================================================
    //  버튼 핸들러
    // =================================================================

    // [다시 구매하기] 버튼 → 슬롯 선택으로 복귀
    public void OnRepurchaseClicked()
    {
        ShowCards();
    }

    // [뒤로가기] 버튼 (슬롯 선택 패널)
    public void OnBackFromSlotSelect()
    {
        ChangeState(GunSmithState.WeaponTypeSelect);
    }

    // [뒤로가기] 버튼 (카드 패널)
    public void OnBackFromCardSelect()
    {
        ChangeState(GunSmithState.WeaponTypeSelect);
    }

    // ESC 키 처리
    private void HandleEscape()
    {
        switch (currentState)
        {
            case GunSmithState.WeaponTypeSelect:
                CloseGunSmith();
                break;
            case GunSmithState.CardSelect:
            case GunSmithState.PostPurchase:
                ChangeState(GunSmithState.WeaponTypeSelect);
                break;
        }
    }

    // =================================================================
    //  내부 유틸
    // =================================================================
    private void RebuildModifierCache(GameObject player)
    {
        modifierCache.Clear();

        WeaponSwitchSystem switchSystem = player.GetComponentInChildren<WeaponSwitchSystem>();
        if (switchSystem == null) return;

        foreach (WeaponSlot slot in System.Enum.GetValues(typeof(WeaponSlot)))
        {
            WeaponBase weapon = switchSystem.GetWeapon(slot);
            if (weapon == null) continue;

            // AddComponent 제거 프리팹에 붙어있는 것만 가져옴
            WeaponModifier mod = weapon.GetComponent<WeaponModifier>();
            if (mod == null)
            {
                Debug.LogWarning($"[GunSmith] {weapon.name}에 WeaponModifier가 없습니다! 프리팹에 추가해주세요.");
                continue;
            }

            weapon.SetupModifier(mod);

            modifierCache[weapon.GetWeaponType()] = mod;
        }
    }

    private WeaponBase GetWeaponByType(WeaponType type)
    {
        if (playerEntity == null) return null;
        WeaponSwitchSystem switchSystem = playerEntity.GetComponentInChildren<WeaponSwitchSystem>();
        if (switchSystem == null) return null;

        foreach (WeaponSlot slot in System.Enum.GetValues(typeof(WeaponSlot)))
        {
            WeaponBase weapon = switchSystem.GetWeapon(slot);
            if (weapon != null && weapon.GetWeaponType() == type)
                return weapon;
        }
        return null;
    }

    private void ShowCards()
    {
        // 슬롯 무관, 무기군만 필터링해서 랜덤 3개
        List<WeaponAttachment> randomParts =
            AttachmentPool.instance.GetRandomAttachments(selectedWeaponType, 3);

        for (int i = 0; i < partCards.Length; i++)
        {
            if (i < randomParts.Count)
            {
                partCards[i].gameObject.SetActive(true);
                bool canAfford = playerInventory.CurrentCoins >= randomParts[i].cost;
                partCards[i].SetupCard(randomParts[i], canAfford);
            }
            else
            {
                partCards[i].gameObject.SetActive(false);
            }
        }

        ChangeState(GunSmithState.CardSelect);
    }

    private void SyncWorldItems()
    {
        ItemWeapon[] worldItems = FindObjectsByType<ItemWeapon>(FindObjectsSortMode.None);

        foreach (var pair in modifierCache)
        {
            WeaponType weaponType = pair.Key;
            List<WeaponAttachment> parts = pair.Value.GetCurrentAttachments();

            foreach (var item in worldItems)
            {
                if (item.weaponType == weaponType)
                    item.SetInGameAttachments(parts);
            }
        }

        // 프리팹 동기화
        if (weaponPrefab != null)
        {
            WeaponType prefabType = weaponPrefab.weaponType;
            if (modifierCache.TryGetValue(prefabType, out WeaponModifier prefabMod))
                weaponPrefab.SetInGameAttachments(prefabMod.GetCurrentAttachments());
        }
    }

    private void SetPlayerControl(bool state)
    {
        Cursor.visible = !state;
        Cursor.lockState = state ? CursorLockMode.Locked : CursorLockMode.None;
        playerEntity.GetComponent<PlayerController>().enabled = state;
    }

    // 외부 접근용 (ItemSpawner 등)
    public List<WeaponAttachment> GetCurrentAttachments(WeaponType type = WeaponType.AR)
    {
        if (modifierCache.TryGetValue(type, out WeaponModifier mod))
            return mod.GetCurrentAttachments();

        return new List<WeaponAttachment>();
    }
}