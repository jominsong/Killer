using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

public class ItemWeapon : ItemBase
{
    [System.NonSerialized]
    public List<WeaponAttachment> inheritedAttachments = new List<WeaponAttachment>();

    [Header("Weapon Type")]
    public WeaponType weaponType;


    [SerializeField]
    private GameObject WeaponEffectPrefab;
    [SerializeField]
    private float rotateSpeed = 50;
    [SerializeField]
    private Weapon weaponPrefab;

    private void Awake()
    {
        inheritedAttachments = new List<WeaponAttachment>(inheritedAttachments);
    }

    private IEnumerator Start()
    {
        while (true)
        {
            // y축을 기준으로 회전
            transform.Rotate(Vector3.up * rotateSpeed *  Time.deltaTime);

            yield return null;
        }
    }

    public void SetInGameAttachments(List<WeaponAttachment> newparts)
    {
        inheritedAttachments = new List<WeaponAttachment>(newparts);
    }

    public override void Use(GameObject entity)
    {
        WeaponSwitchSystem weaponSystem = entity.GetComponentInChildren<WeaponSwitchSystem>();
        if (weaponSystem == null) return;

        if (weaponSystem.HasWeapon(WeaponSlot.Primary))
        {
            WeaponBase oldWeapon = weaponSystem.GetWeapon(WeaponSlot.Primary);
            if (oldWeapon != null)
            {
                oldWeapon.ThrowWeapon();
                weaponSystem.RemoveWeapon(oldWeapon);
            }
        }

        Weapon newWeapon = Instantiate(weaponPrefab);
        WeaponModifier modifier = newWeapon.GetComponent<WeaponModifier>();


        if (modifier != null)
        {
            modifier.Setup(newWeapon);

            // 건스미스 파츠 먼저 적용 (무기군 기준으로 정확히 가져옴)
            if (GunSmithManager.Instance != null)
            {
                var prebuiltParts = GunSmithManager.Instance.GetCurrentAttachments(weaponType);
                foreach (var part in prebuiltParts)
                {
                    if (part != null)
                        modifier.AddAttachment(part);
                }
            }

            // inheritedAttachments는 건스미스 파츠와 슬롯 겹치면 덮어씀
            // (드롭된 무기에 붙어있던 파츠 복구용)
            foreach (var part in inheritedAttachments)
            {
                if (part != null)
                    modifier.AddAttachment(part);
            }
        }

        newWeapon.SendMessage("UpdateMod", SendMessageOptions.DontRequireReceiver);
        weaponSystem.AddWeapon(newWeapon, WeaponSlot.Primary);

        if (WeaponEffectPrefab != null)
            Instantiate(WeaponEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}

