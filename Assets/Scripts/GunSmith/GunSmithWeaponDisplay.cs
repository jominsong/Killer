using UnityEngine;
using System.Collections.Generic;

public class GunSmithWeaponDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private Transform displayPoint;      // 무기 소환 위치
    [SerializeField] private string displayLayer = "GunSmithDisplay";

    // 무기군별 전시용 프리팹 연결
    [Header("Display Prefabs")]
    [SerializeField] private GameObject handGunDisplayPrefab;
    [SerializeField] private GameObject arDisplayPrefab;
    [SerializeField] private GameObject smgDisplayPrefab;
    [SerializeField] private GameObject shotgunDisplayPrefab;

    private GameObject currentDisplayWeapon;  // 현재 표시 중인 무기
    private WeaponModifier currentModifier;   // 파츠 동기화용

    // 무기군 버튼 클릭 시 GunSmithManager에서 호출
    public void ShowWeapon(WeaponType weaponType, WeaponModifier modifier)
    {
        currentModifier = modifier;

        // 기존 전시 무기 제거
        if (currentDisplayWeapon != null)
        {
            Destroy(currentDisplayWeapon);
            currentDisplayWeapon = null;
        }

        // 해당 무기군 프리팹 선택
        GameObject prefab = GetDisplayPrefab(weaponType);
        if (prefab == null)
        {
            Debug.LogWarning($"[GunSmithDisplay] {weaponType} 전시용 프리팹이 없습니다.");
            return;
        }

        // 소환 및 레이어 설정
        currentDisplayWeapon = Instantiate(prefab, displayPoint.position, displayPoint.rotation);
        int layer = LayerMask.NameToLayer(displayLayer);
        Debug.Log($"[GunSmithDisplay] 레이어 '{displayLayer}' = {layer}, 무기 소환 위치: {displayPoint.position}");
        SetLayerRecursively(currentDisplayWeapon, layer);

        // 현재 장착된 파츠 시각 동기화
        SyncAttachments();
    }

    // 파츠 구매 시 호출 → 전시 무기에도 즉시 반영
    public void SyncAttachments()
    {
        if (currentDisplayWeapon == null || currentModifier == null) return;

        WeaponModifier displayModifier = currentDisplayWeapon.GetComponent<WeaponModifier>();
        if (displayModifier == null) return;

        // 현재 장착된 모든 파츠를 전시 무기에 복사
        foreach (var attachment in currentModifier.GetCurrentAttachments())
        {
            displayModifier.AddAttachment(attachment);
        }
    }

    // 건스미스 닫을 때 전시 무기 제거
    public void HideWeapon()
    {
        if (currentDisplayWeapon != null)
        {
            Destroy(currentDisplayWeapon);
            currentDisplayWeapon = null;
        }
        currentModifier = null;
    }

    public void UpdateModifier(WeaponModifier modifier)
    {
        currentModifier = modifier;
    }

    private GameObject GetDisplayPrefab(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.HandGun: return handGunDisplayPrefab;
            case WeaponType.AR: return arDisplayPrefab;
            case WeaponType.SMG: return smgDisplayPrefab;
            case WeaponType.ShotGun: return shotgunDisplayPrefab;
            default: return null;
        }
    }

    // 자식 오브젝트 포함 레이어 일괄 변경
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}