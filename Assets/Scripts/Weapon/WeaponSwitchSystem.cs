 using UnityEngine;

public class WeaponSwitchSystem : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private PlayerHUD playerHUD;
    [SerializeField]
    private Transform mountPrimary;
    [SerializeField]
    private Transform mountSecondary;

    [SerializeField]
    private WeaponBase[] weapons = new WeaponBase[2];

    private WeaponBase currentWeapon;  // 현재 사용중인 무기
    private WeaponBase previousWeapon;  // 직전에 사용했던 무기
    private CameraEffects cameraEffects;

    public WeaponBase CurrentWeapon => currentWeapon;

    private void Awake()
    {
        // 무기 정보 출력을 위해 현재 소지중인 모든 무기 이벤트 등록
        playerHUD.SetupAllWeapons(weapons);

        // Main 무기를 현재 사용 무기로 설정
        SwitchingWeapon(WeaponSlot.Primary);
    }

    private void Update()
    {
        UpdateSwitch();
    }

    private void UpdateSwitch()
    {
        if( !Input.anyKeyDown) return;

        // 1 = Primary, 2 = Secondary
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchingWeapon(WeaponSlot.Primary);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchingWeapon(WeaponSlot.Secondary);
    }

    private void SwitchingWeapon(WeaponSlot slot)
    {

        WeaponBase target = weapons[(int)slot];
        if (target == null) return;
        // 현재 사용중인 무기로 교체하려고 할 떄 종료
        if (target == currentWeapon) return;

        // 이전에 사용하던 무기 비활성화
        if (currentWeapon != null)
        {
            previousWeapon = currentWeapon;
            previousWeapon.gameObject.SetActive(false);
        }

        // 무기 교체
        currentWeapon = target;
        // 현재 사용하는 무기 활성화
        currentWeapon.gameObject.SetActive(true);


        // 무기를 사용하는 PlayerController, PlayerHUD에 현재 무기 정보 전달
        playerController.SwitchingWeapon(currentWeapon);
        playerHUD.SwitchingWeapon(currentWeapon);
    }

    private Transform GetMountPoint(WeaponSlot slot)
    {
        return slot == WeaponSlot.Primary ? mountPrimary : mountSecondary;
    }

    public void ClearCurrentWeapon(WeaponBase weapon)
    {
        if (currentWeapon == weapon)
        {
            currentWeapon = null;
            previousWeapon = null;

            playerController.SwitchingWeapon(null);
            playerHUD.SwitchingWeapon(null);
        }
    }

    public void RemoveWeapon(WeaponBase weapon)
    {
        weapon.OnUnequipped();

        for (int i = 0; i < weapons.Length; ++ i)
        {
            if ( weapons[i] == weapon )
            {
                weapons[i] = null;
                break;
            }
        }

        // HUD에 등록 갱신
        playerHUD.SetupAllWeapons(weapons);

        // 현재 무기를 버렸다면 다른 무기 자동 장착
        if (currentWeapon == weapon)
        {
            currentWeapon = null;
            previousWeapon = null;

            // 남아있는 무기 중 하나 자동 선택
            for (int i = 0;i < weapons.Length; ++ i)
            {
                if ( weapons[i] == null )
                {
                    SwitchingWeapon((WeaponSlot)i);
                    return;
                }
            }
        }

        // 무기가 하나도 없을때
        playerController.SwitchingWeapon(null);
        playerHUD.SwitchingWeapon(null);
    }

    public void AddWeapon(WeaponBase newWeapon, WeaponSlot slot)
    {
        if (weapons[(int)slot] != null) return;

        weapons[(int)slot] = newWeapon;

        Transform mount = GetMountPoint(slot);

        // 플레이어 무기 하위로 정렬, 활성화 처리
        newWeapon.transform.SetParent(mount);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        newWeapon.OnEquipped();

        // HUD에 등록 갱신
        playerHUD.SetupAllWeapons(weapons);
        playerHUD.crosshairUI.SetActive(true);

        // 바로 장착
        SwitchingWeapon(slot);
    }

    public bool HasWeapon(WeaponSlot slot) => weapons[(int)slot] != null;

    public WeaponBase GetWeapon(WeaponSlot slot) => weapons[(int)slot];
}
