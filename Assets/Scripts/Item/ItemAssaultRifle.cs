using System.Collections;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ItemWeaponAssaultRifle : ItemBase
{
    [System.NonSerialized]
    public List<WeaponAttachment> inheritedAttachments = new List<WeaponAttachment>();

    [SerializeField]
    private GameObject AssaultRifleEffectPrefab;
    [SerializeField]
    private float rotateSpeed = 50;
    [SerializeField]
    private WeaponAssaultRifle assaultRiflePrefab;

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

        // 기존 슬롯에 무기가 있는지 확인
        if (weaponSystem.HasWeapon(WeaponType.main))
        {
            WeaponBase oldWeapon = weaponSystem.GetWeapon(WeaponType.main);
            if (oldWeapon != null)
            {
                // 무기 던지기 실행
                oldWeapon.ThrowWeapon();
                // 시스템에서 제거 및 리스트 갱신
                weaponSystem.RemoveWeapon(oldWeapon);
            }
        }

        // 새로운 무기 인스턴스 생성 및 설정
        WeaponAssaultRifle newRifle = Instantiate(assaultRiflePrefab);

        // modifirer 설정 및 기존 파츠 복구
        WeaponModifier modifier = newRifle.GetComponent<WeaponModifier>();

        if (modifier != null)
        {
            modifier.Setup(newRifle);

            foreach (var part in inheritedAttachments)
            {
                if (part != null)
                {
                    modifier.AddAttachment(part);
                }
            }
        }

        newRifle.SendMessage("UpdateMod",SendMessageOptions.DontRequireReceiver);

        // 새 무기를 메인 슬롯에 등록 (자동 장착 및 활성화)
        weaponSystem.AddWeapon(newRifle, WeaponType.main);

        // 시각 효과 및 아이템 오브젝트 파괴
        if (AssaultRifleEffectPrefab != null)
            Instantiate(AssaultRifleEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (inheritedAttachments != null)
        {
            inheritedAttachments.Clear();
        }
    }
}

