using UnityEngine;

public enum AttachmentSlot { Muzle, Grip, Scope, Magazine, }

[CreateAssetMenu(fileName = "WeaponAttachment", menuName = "Scriptable Objects/WeaponAttachment")]
public class WeaponAttachment : ScriptableObject
{
    public string attachmentName;
    public AttachmentSlot slot;
    public Sprite attachmentIcon;
    public int cost;

    [Header("Stat Modifiers (Multiplier)")]
    public float recoilMultiplier = 1.0f;  // 0.8이면 반동 20% 감소
    public float spreadMultiplier = 1.0f;  // 0.8이면 탄퍼짐 20% 감소
    public float MaxmagazineMultiplier = 1.0f;
    public float moveSpeedMultiplier = 1.0f;
    public float damageMultiplier = 1.0f;

    [Header("Allowed Weapons")]
    public WeaponType[] allowedWeaponTypes;  // 이 파츠를 장착할 수 있는 무기군

    [Header("Visual Asset")]
    public GameObject attachmentPrefab;  // 총기에 붙일 메쉬
}
