using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    private WeaponBase weapon; // 현재 정보가 출력되는 무기

    [Header("Components")]
    [SerializeField]
    private Status status; // 플레이어의 상태 (이동속도, 체력)

    [Header("Weapon Base")]
    [SerializeField]
    private Image imageWeaponIcon;  // 무기 아이콘
    [SerializeField]
    private Sprite[] spriteWeaponIcons;  // 무기 아이콘에 사용되는 sprite 배열
    [SerializeField]
    private Vector2[] sizeWeaponIcons;  // 무기 아이콘의 UI 크기 배열

    [Header("Ammo")]
    [SerializeField]
    private TextMeshProUGUI textAmmo;  //  현재/최대 탄 수 출력 Text

    [Header("CorrsHair")]
    [SerializeField]
    public CrosshairUi crosshairUI;  // 크로스 헤어 Ui

    [Header("Magazine")]
    [SerializeField]
    private int maxMagazineCount;  // 처음 생성하는 최대 탄창 수

    [Header("HP & BloodScreen UI")]
    [SerializeField]
    private TextMeshProUGUI textHP;  // 플레이어의 체력을 출력하는 Text
    [SerializeField]
    private Image imageBloodScreen;  // 플레이어가 공격받았을 때 화면에 표시되는 Image
    [SerializeField]
    private AnimationCurve curveBloodScreen;

    [Header("Interaction")]
    [SerializeField]
    private TextMeshProUGUI textInteraction;  // 상호작용 표시 Text

    [Header("Coin UI")]
    [SerializeField]
    private TextMeshProUGUI textCoinCount;
    [SerializeField]
    private PlayerInventory playerInventory;

    private void Awake()
    {
        // 메소드가 등록되어 있는 이벤트 클래스(weapon.xx)의
        // Invoke() 메소드가 호출될 때 등록된 메소드(매개변수)가 실행된다
        status.onHPEnvet.AddListener(UpdateHPHUD);
        playerInventory.onCoinChanged.AddListener(UpdateCoinHUD);
    }

    public void SetupAllWeapons(WeaponBase[] weapons)
    {

        // 사용 가능한 모든 무기의 이벤트 등록
        for (int i = 0; i < weapons.Length; ++i)
        {
            if (weapons[i] == null) return;

            weapons[i].onAmmoEvent.AddListener(UpdateAmmoHUD);
            weapons[i].onCrossHairEvent.AddListener(UpdateCrosshairHUD);
            weapons[i].onAimEvent.AddListener(UpdateAimHUD);
        }
    }

    public void SwitchingWeapon(WeaponBase newWeapon)
    {
        weapon = newWeapon;
        
        if ( weapon == null)
        {
            imageWeaponIcon.enabled = false;
            textAmmo.text = "";
            return;
        }

        imageWeaponIcon.enabled = true;
        SetupWeapon();
    }

    public void SetInteractionText(bool isVisible, string message = " ")
    {
        if (textInteraction == null) return;

        if (isVisible)
        {
            textInteraction.text = message;
            textInteraction.gameObject.SetActive(true);
        }
        else
        {
            textInteraction.gameObject.SetActive(false);
        }
    }

    private void UpdateCoinHUD(int currentCoins)
    {
        if (textCoinCount != null)
        {
            textCoinCount.text = currentCoins.ToString();
        }
    }

    private void SetupWeapon()
    {
        imageWeaponIcon.sprite = spriteWeaponIcons[(int)weapon.WeaponName];
        imageWeaponIcon.rectTransform.sizeDelta = sizeWeaponIcons[(int)weapon.WeaponName];
    }

    private void UpdateAmmoHUD(int currentAmmo, int maxAmmo)
    {
        textAmmo.text = $"<size=40>{currentAmmo}/</size>{maxAmmo}";
    }

    private void UpdateCrosshairHUD(float spread)
    {
        if (crosshairUI == null) return;

        crosshairUI.SetSpread(spread);
    }

    private void UpdateAimHUD(bool isAiming)
    {
        if (crosshairUI == null) return;

        crosshairUI.SetActive(!isAiming);
    }

    private void UpdateHPHUD(int previous, int current)
    {
        textHP.text = "HP" + current;

        // 체력이 증가 했을 때는 화면에 빨간색 이미지를 출력하지 않도록 return
        if (previous <= current) return;

        if (previous - current > 0)
        {
            StopCoroutine("OnBloodScreen");
            StartCoroutine("OnBloodScreen");
        }
    }

    private IEnumerator OnBloodScreen()
    {
        float percent = 0;

        while ( percent < 1)
        {
            percent += Time.deltaTime;

            Color color = imageBloodScreen.color;
            color.a = Mathf.Lerp(1,0,curveBloodScreen.Evaluate(percent));
            imageBloodScreen.color = color;

            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.onCoinChanged.RemoveListener(UpdateCoinHUD);
        }
    }
}
