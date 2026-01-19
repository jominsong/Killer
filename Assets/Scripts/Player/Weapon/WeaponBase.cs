using System;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Events;

public enum WeaponType { main =0 , sub , melee, Throw }

[System.Serializable]
public class AmmoEvent : UnityEngine.Events.UnityEvent<int, int> { }
[System.Serializable]
public class MagazineEvent : UnityEngine.Events.UnityEvent<int> { }
[System.Serializable]
public class CrossHairEvent : UnityEngine.Events.UnityEvent<float> { }
[System.Serializable]
public class AimEvent : UnityEngine.Events.UnityEvent<bool> { }

public abstract class WeaponBase : MonoBehaviour
{
    [Header("WeaponBase")]
    [SerializeField]
    protected WeaponType weaponType;  // 무기 종류
    [SerializeField]
    protected WeaponSetting weaponSetting;  // 무기 설정
    [SerializeField]
    protected WeaponSwitchSystem weaponSwitchSystem;  // 무기 전환 시스템

    protected float lasetAttackTime = 0f;  // 마지막 발사시간 체크용
    protected bool isAttack = false;  // 공격 여부 체크용
    protected AudioSource audioSource;  // 사운드 재생 컴포넌트
    protected PlayerAnimatorController animator;  // 애니메이션 재생 제어
    protected bool isEquipped = false;  // 장착 여부 확인
    protected Coroutine attackCoroutine;  // 코루틴 정리

    // 외부에서 이벤트 함수 등록을 할 수 있도록 public 선언
    [HideInInspector]
    public AmmoEvent onAmmoEvent = new AmmoEvent();
    [HideInInspector]
    public MagazineEvent onMagazineEvent = new MagazineEvent();
    [HideInInspector]
    public CrossHairEvent onCrossHairEvent = new CrossHairEvent();
    [HideInInspector]
    public AimEvent onAimEvent = new AimEvent();

    // 외부에서 필요한 정보를 열람하기 위해 정의한 Get Property's
    public PlayerAnimatorController Animator => animator;
    public WeaponName WeaponName => weaponSetting.weaponName;
    public int CurrentMagazine => weaponSetting.currentMagazine;
    public int MaxMagazine => weaponSetting.maxMagazine;

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
        animator = GetComponent<PlayerAnimatorController>();
        weaponSwitchSystem = UnityEngine.Object.FindFirstObjectByType<WeaponSwitchSystem>();
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
}
