using UnityEngine;

public class GunSmithWorkBench : InteractionBase
{
    [SerializeField]
    private WeaponAttachment attachmentToSell;  // 이 선반에서 파는 파츠

    public override void Use(GameObject entity)
    {
        WeaponSwitchSystem switchSystem = entity.GetComponentInChildren<WeaponSwitchSystem>();
        if (switchSystem == null || switchSystem.CurrentWeapon == null) return;

        // 매니저를 통해 건스미스 화면 오픈
        GunSmithManager.Instance.OpenGunSmith(entity, switchSystem.CurrentWeapon);

    }
}
