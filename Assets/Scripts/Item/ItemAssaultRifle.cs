using System.Collections;
using UnityEngine;

public class ItemWeaponAssaultRifle : ItemBase
{
    [SerializeField]
    private GameObject AssaultRifleEffectPrefab;
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

        WeaponAssaultRifle rifle =
        entity.GetComponentInChildren<WeaponAssaultRifle>();

        WeaponAssaultRifle newRifle = Instantiate(assaultRiflePrefab);
        newRifle.SendMessage("Setup", SendMessageOptions.DontRequireReceiver);

        weaponSystem.AddWeapon(newRifle, WeaponType.main);

        Instantiate(AssaultRifleEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
