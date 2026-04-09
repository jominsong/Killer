using System.Collections.Generic;
using UnityEngine;

public enum AttachmentSlot { Muzle, Grip, Scope, Magazine, Ammo, Spring}

[CreateAssetMenu(fileName = "WeaponAttachment", menuName = "Scriptable Objects/WeaponAttachment")]
public class WeaponAttachment : ScriptableObject
{
    public string attachmentName;
    public AttachmentSlot slot;
    public Sprite attachmentIcon;

    public int cost;  // 가격

    [Header("Allowed Weapons")]
    public WeaponType[] allowedWeaponTypes;  // 이 파츠를 장착할 수 있는 무기군

    [Header("Visual Asset")]
    public GameObject attachmentPrefab;  // 총기에 붙일 메쉬

    [Header("Recoil Modifiers")]
    public float verticalrecoilMultiplier = 1.0f;  // y축 반동 배율
    public float horizontalrecoilMultiplier = 1.0f;  // x축 반동 배율
    public float visualrecoilMultiplier = 1.0f;  // 비주얼 리코일 배율

    [Header("Stat Modifiers")]
    public float flattackRateMultiplier = 1.0f;
    public float damageMultiplier = 1.0f;
    public int maxAmmoAdder = 0;
    public float throwForceMultiplier = 1.0f;
    public float moveSpeedMultiplier = 1.0f;
    public float distanceMultiplier = 1.0f;
    public float zoomSpeedMultiplier = 1.0f;

    [Header("Spread Modifiers")]
    public float maxspreadMultiplier = 1.0f;
    public float increasespreadMultiplier = 1.0f;
    


}
