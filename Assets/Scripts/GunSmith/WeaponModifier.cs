using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WeaponModifier : MonoBehaviour
{
    private WeaponBase weapon;
    // 장착된 부착물들을 관리 (부위별로 하나씩만 장착 가능하도록 dictionary 사용)
    private Dictionary<AttachmentSlot, WeaponAttachment> equippedAttachments = new Dictionary<AttachmentSlot, WeaponAttachment>();
    // 현재 장착된 파츠 오브젝트들을 관리
    private Dictionary<AttachmentSlot, GameObject> spawnedParts = new Dictionary<AttachmentSlot, GameObject>();
    // 임시 modifier용 weapon 없이 무기군만 지정
    private WeaponType? forcedWeaponType = null;

    [Header("Attachment Points")]
    [SerializeField]
    private Transform muzzlePoint;
    [SerializeField]
    private Transform gripPoint;
    [SerializeField]
    private Transform scopePoint;
    [SerializeField]
    private Transform magazinePoint;

    public List<WeaponAttachment> GetCurrentAttachments()
    {
        return new List<WeaponAttachment>(equippedAttachments.Values);
    }

    public void Setup(WeaponBase attachedWeapon)
    {
        weapon = attachedWeapon;
    }

    public void SetWeaponType(WeaponType type)
    {
        forcedWeaponType = type;
    }

    public bool CanAttach(WeaponAttachment attachment)
    {
        if (attachment == null) return false;

        // forcedWeaponType 우선 사용
        WeaponType targetType = forcedWeaponType ?? (weapon != null ? weapon.GetWeaponType() : (WeaponType?)null) ?? WeaponType.AR;

        return System.Array.Exists(attachment.allowedWeaponTypes, t => t == targetType);
    }

    // 파츠 장착 로직
    public void AddAttachment(WeaponAttachment newAttachment)
    {
        if (newAttachment == null) return;

        // 같은 부위의 기존 파츠가 있다면 교체 (확장성)
        equippedAttachments[newAttachment.slot] = newAttachment;

        // 기존 모델링 제거
        if (spawnedParts.ContainsKey(newAttachment.slot))
        {
            Destroy(spawnedParts[newAttachment.slot]);
            spawnedParts.Remove(newAttachment.slot);
        }

        // 새 모델링 생성
        if (newAttachment.attachmentPrefab != null)
        {
            Transform targetPoint = GetTargetPoint(newAttachment.slot);
            if (targetPoint != null)
            {
                GameObject partObj = Instantiate(newAttachment.attachmentPrefab, targetPoint);
                partObj.transform.localPosition = Vector3.zero;
                partObj.transform.localRotation = Quaternion.identity;

                spawnedParts[newAttachment.slot] = partObj;
            }
        }
    }

    // 투척용 프리팹에 파츠 정보를 그대로 복사해줌
    public void SyncAttachmentsTo(GameObject thrownObj)
    {
        WeaponModifier thrownModifier = thrownObj.GetComponent<WeaponModifier>();
        if (thrownModifier != null)
        {
            foreach (var entry in equippedAttachments)
            {
                thrownModifier.AddAttachment(entry.Value);
            }
        }
        
        ThrownWeapon thrownScript = thrownObj.GetComponent<ThrownWeapon>();
        if (thrownScript != null)
        {
            thrownScript.StoreAttachments(equippedAttachments);
        }
    }

    public void RemoveAttachment(AttachmentSlot slot)
    {
        if (equippedAttachments.ContainsKey(slot))
        {
            equippedAttachments.Remove(slot);
        }
    }

    private Transform GetTargetPoint(AttachmentSlot slot)
    {
        switch (slot)
        {
            case AttachmentSlot.Muzle: return muzzlePoint;
            case AttachmentSlot.Grip: return gripPoint;
            case AttachmentSlot.Scope: return scopePoint;
            case AttachmentSlot.Magazine: return magazinePoint;
            default: return null;
        }
    }

    // 최종 반동 배율 계산
    public float GetVerticalRecoilMod() { return CalculateMod(a => a.verticalrecoilMultiplier); }
    public float GetHorizontalRecoilMod() { return CalculateMod(a =>a.horizontalrecoilMultiplier); }
    public float GetVisualRecoilMod() { return CalculateMod(a => a.visualrecoilMultiplier); }

    // 최종 탄퍼짐 배율 계산
    public float GetMaxSpreadMod() { return CalculateMod(a => a.maxspreadMultiplier); }
    public float GetIncreaseSpreadMod() { return CalculateMod(a => a.increasespreadMultiplier); }

    // 최종 부가 스텟 계산
    public float GetMoveSpeedMod() { return CalculateMod(a => a.moveSpeedMultiplier); }
    public float GetAttacklateMod() { return CalculateMod(a => a.flattackRateMultiplier); }
    public float GetDamageMod() { return CalculateMod(a => a.damageMultiplier); }
    public float GetMaxDistanceMod() {return CalculateMod(a => a.distanceMultiplier); }
    public float GetZoomSpeedMod() {return CalculateMod(a => a.zoomSpeedMultiplier);}
    public float GetThrowMod() { return CalculateMod(a => a.throwForceMultiplier); }
    public int GetMaxAmmo() { return equippedAttachments.Values.Sum(a => a.maxAmmoAdder); }

    private float CalculateMod(System.Func<WeaponAttachment, float> selector)
    {
        float mod = 1.0f;
        foreach (var a in equippedAttachments.Values) mod *= selector(a);
        return mod;
    }
}
