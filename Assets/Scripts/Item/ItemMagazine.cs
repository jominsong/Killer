using System.Collections;
using UnityEngine;

public class ItemMagazine : ItemBase
{
    [SerializeField]
    private GameObject magazineEffectPrefab;
    [SerializeField]
    private int increaseMagazine = 2;
    [SerializeField]
    private float rotateSpeed = 50;
    [SerializeField]
    private WeaponAssaultRifle assaultRiflePrefab;

    private IEnumerator Start()
    {
        while (true)
        {
            // y축을 기준으로 회전
            transform.Rotate(Vector3.up * rotateSpeed *  Time.deltaTime);

            yield return null;
        }
    }

    public override void Use(GameObject entity)
    {
        WeaponSwitchSystem weaponSystem =
       entity.GetComponentInChildren<WeaponSwitchSystem>();

        // 이미 Assault Rifle이 있으면 → 기존 로직
        WeaponAssaultRifle rifle =
            entity.GetComponentInChildren<WeaponAssaultRifle>();

        if (rifle != null)
        {
            rifle.IncreaseMagazine(increaseMagazine);
        }
        // Assault Rifle이 없으면 → 새로 생성
        else
        {
            WeaponAssaultRifle newRifle =
                Instantiate(assaultRiflePrefab);

            weaponSystem.AddWeapon(newRifle, WeaponType.main);
        }

        Instantiate(magazineEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
