using System;
using NUnit.Framework.Constraints;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public enum WeaponType { HandGun =0, AR, SMG, ShotGun }

public enum WeaponSlot {  Primary =0, Secondary =1}

[System.Serializable]
public class AmmoEvent : UnityEngine.Events.UnityEvent<int, int> { }
[System.Serializable]
public class CrossHairEvent : UnityEngine.Events.UnityEvent<float> { }
[System.Serializable]
public class AimEvent : UnityEngine.Events.UnityEvent<bool> { }

public abstract class WeaponBase : MonoBehaviour
{
    [Header("WeaponBase")]
    [SerializeField]
    protected WeaponType weaponType;  // 무기 군
    [SerializeField]
    protected WeaponSlot weaponSlot;  // 무기 슬롯
    [SerializeField]
    protected WeaponSetting weaponSetting;  // 무기 설정
    [SerializeField]
    protected WeaponRunTimeStats weaponRT;  // 무기 모딩 업데이트
    [SerializeField]
    protected WeaponSwitchSystem weaponSwitchSystem;  // 무기 전환 시스템

    protected float lasetAttackTime = 0f;  // 마지막 발사시간 체크용
    protected bool isAttack = false;  // 공격 여부 체크용
    protected bool isEquipped = false;  // 장착 여부 확인
    protected AudioSource audioSource;  // 사운드 재생 컴포넌트
    protected PlayerAnimatorController animator;  // 애니메이션 재생 제어
    protected MovementCharacterController movement;  // 플레이어 무브먼트
    protected CameraRecoil cameraRecoil;  // 카메라 반동
    protected WeaponVisualRecoil visualRecoil;  // 비주얼 리코일
    protected WeaponModifier modifier;  // 총기 개조 수치
    protected CameraEffects cameraEffects;  // 카메라 효과 제어
    protected Coroutine attackCoroutine;  // 코루틴 정리
    

    // 외부에서 이벤트 함수 등록을 할 수 있도록 public 선언
    [HideInInspector]
    public AmmoEvent onAmmoEvent = new AmmoEvent();
    [HideInInspector]
    public CrossHairEvent onCrossHairEvent = new CrossHairEvent();
    [HideInInspector]
    public AimEvent onAimEvent = new AimEvent();

    // 외부에서 필요한 정보를 열람하기 위해 정의한 Get Property's
    public PlayerAnimatorController Animator => animator;
    public WeaponName WeaponName => weaponSetting.weaponName;
    public WeaponSetting WeaponSetting => weaponSetting;
    public WeaponType GetWeaponType() => weaponType;
    public WeaponSlot GetWeaponSlot() => weaponSlot;

    public abstract void StartWeaponAction(int type = 0);
    public abstract void StopWeaponAction(int type = 0);
    public abstract void ThrowWeapon();

    protected void PlaySound(AudioClip clip)
    {
        audioSource.Stop();  // 기존에 재생중인 사운드를 정지하고,
        audioSource.clip = clip;  // 새로운 사운드 clip으로 교체 후
        audioSource.Play();  // 사운드 재생
    }

    protected void Setup()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponentInParent<PlayerAnimatorController>();
        if (animator == null)
            animator = GetComponentInChildren<PlayerAnimatorController>();
        weaponSwitchSystem = UnityEngine.Object.FindFirstObjectByType<WeaponSwitchSystem>();
        movement = GetComponentInParent<MovementCharacterController>();
        cameraRecoil = Camera.main.GetComponent<CameraRecoil>();
        visualRecoil = GetComponent<WeaponVisualRecoil>();
        modifier = GetComponent<WeaponModifier>();
        cameraEffects = FindAnyObjectByType<CameraEffects>();

        if (WeaponSetting.recoilData != null)
        {
            weaponSetting.recoilData = weaponSetting.recoilData.Clone();
            if (visualRecoil != null)
                visualRecoil.SetRecoilData(weaponSetting.recoilData);
        }

        UpdateMod();
    }

    public virtual void OnEquipped()
    {
        isEquipped = true;
    }

    public virtual void OnUnequipped()
    {
        isEquipped = false;

        if ( attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    public virtual void SetupModifier(WeaponModifier newmodifier)
    {
        modifier = newmodifier;
    }

    public virtual void UpdateMod()
    {
        if (modifier != null)
        {
            // 무기 스텟 데이터
            weaponRT.finalAmmo = weaponSetting.maxAmmo + modifier.GetMaxAmmo();
            weaponRT.finalDamage = weaponSetting.damage * modifier.GetDamageMod();
            weaponRT.finalAttackRate = weaponSetting.attackRate / modifier.GetAttacklateMod();
            weaponRT.finalDistance = weaponSetting.attackDistance * modifier.GetMaxDistanceMod();
            weaponRT.finalZoominSpeed = weaponSetting.zoominSpeed * modifier.GetZoomSpeedMod();
            weaponRT.finalThrowforce = weaponSetting.Throwforce * modifier.GetThrowMod();

            // 반동 데이터
            weaponRT.vRecoil = modifier.GetVerticalRecoilMod();
            weaponRT.hRecoil = modifier.GetHorizontalRecoilMod();
            weaponRT.VisualRecoil = weaponSetting.rotationIntensity * modifier.GetVisualRecoilMod();

            // 탄퍼짐 데이터
            weaponRT.maxSpread = modifier.GetMaxSpreadMod();
            // float increaseSpread = GetIncreaseSpreadMod();

            if (visualRecoil != null)
                visualRecoil.SetMaxAmmo(weaponRT.finalAmmo);

            if (cameraRecoil != null)
                cameraRecoil.SetMaxAmmo(weaponRT.finalAmmo);
        }
    }
}