using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAssaultRifle : WeaponBase
{
    [Header("Fire Effects")]
    [SerializeField]
    private GameObject muzzleFlashEffect;  // 총구 이펙트 (On/Off)

    [Header("Spawn Points")]
    [SerializeField]
    private Transform casingSpawnPoint;  // 탄피 생성 위치
    [SerializeField]
    private Transform bulletSpawnPoint;  // 총알 생성 위치
    [SerializeField]
    private Transform throwPoint;  // 던질 위치

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipTakeOutWeapon;  // 무기 장착 사운드
    [SerializeField]
    private AudioClip audioClipFire;  // 공격 사운드

    [Header("ThrowWeapon")]
    [SerializeField]
    private GameObject throwWeaponPrepfab;  // 던질 무기 프리팹
    private float throwForce = 15;
    private float spinForce = 500;

    private bool isModeChange = false;  // 모드 전환 여부 체크용
    private float defaultModeFOV = 60;  // 기본모드에서의 카메라 FOV
    private float aimModeFov = 30;  // AIM모드에서의 카메라 FOV
    private float currentSpread;  // 탄퍼짐 체크용

    private CasingMemoryPool casingMemoryPool;  // 탄피 생성 후 활성/비활성 관리
    private ImpactMemoryPool impactMemoryPool;  // 공격 효과 생성 후 활성/비활성 관리
    private Camera mainCamera;  // 광선 발사

    private void Awake()
    {
        // 기반 클래스의 초기화를 위한 Setup() 메소드 호출
        base.Setup();

        casingMemoryPool = GetComponent<CasingMemoryPool>();
        impactMemoryPool = GetComponent<ImpactMemoryPool>();
        mainCamera = Camera.main;

        // 처음 탄창 수는 최대로 설정
        weaponSetting.currentMagazine = weaponSetting.maxMagazine;
        // 처음 탄 수는 최대로 설정
        weaponSetting.currentAmmo = weaponSetting.maxAmmo;
        // 탄퍼짐 설정
        currentSpread = weaponSetting.minSpread;
    }

    private void OnEnable()
    {
        // 무기 장착 사운드 재생
        PlaySound(audioClipTakeOutWeapon);
        // 총구 이펙트 오브젝트 비활성화
        muzzleFlashEffect.SetActive(false);
        // 무기 활성화
        OnEquipped();
        // 무기가 활성화될 때 해당 무기의 탄창 정보를 갱신한다
        onMagazineEvent.Invoke(weaponSetting.currentMagazine);
        // 무기가 활성화될 때 해당 무기의 탄 수 정보를 갱신한다
        onAmmoEvent.Invoke(weaponSetting.currentAmmo,weaponSetting.maxAmmo);

        ResetVariables();
    }

    private void Update()
    {
        if (!isEquipped) return;

        currentSpread = Mathf.Lerp(currentSpread, 
        weaponSetting.minSpread,Time.deltaTime * weaponSetting.spreadRecoverySpeed);

        if (animator != null && movement != null) GetSpreadDirection();
    }

    public override void StartWeaponAction(int type =0)
    {
        // 무기가 없을때 무기 액션 차단
        if (!isEquipped) return;
        if (attackCoroutine != null) return;

        // 모드 전환중이면 무기 액션을 할 수 없다
        if (isModeChange == true) return;

        // 마우스 왼쪽 클릭 (공격 시작)
        if (type == 0)
        {
            // 연속 공격
            if (weaponSetting.isAutomaticAttack == true )
            {
                isAttack = true;
                StartCoroutine("OnAttackLoop");
            }
            // 단발 공격
            else
            {
                OnAttack();
            }
        }
        // 마우스 오른쪽 클릭 (모드 전환)
        else
        {
            StartCoroutine("OnModeChange");
        }
    }

    public override void StopWeaponAction(int type=0)
    {
        // 마우스 왼쪽 클릭 (공격 종료)
        if ( type == 0)
        {
            isAttack = false;
            StopCoroutine("OnAttackLoop");
        }
    }

    public override void ThrowWeapon()
    {
        // 공격 / 모드 전환 중이면 무시
        if (isAttack || isModeChange) return;
        onAimEvent.Invoke(!animator.AimModeIs);

        GameObject obj = Instantiate(throwWeaponPrepfab,throwPoint.position,Quaternion.identity);

        if ( modifier != null )
        {
            ThrownWeapon thrownScript = obj.GetComponent<ThrownWeapon>();
            if (thrownScript != null)
            {
                // 현재 장착된 모든 파츠 리스트를 전달
                thrownScript.SetSavedAttachments(modifier.GetCurrentAttachments());
                // 시각적 동기화
                modifier.SyncAttachmentsTo(obj);
            }
            
        }

        // 던지기에 물리적인 힘 주입
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        obj.transform.rotation = mainCamera.transform.rotation;
        // 앞으로 날아가는 힘
        rb.AddForce(mainCamera.transform.forward * throwForce, ForceMode.Impulse);
        // 회전 토크 값
        Vector3 spinAxis = mainCamera.transform.rotation * Vector3.right;
        rb.angularVelocity = spinAxis * spinForce;

        // 현재 무기 비활성화
        weaponSwitchSystem.ClearCurrentWeapon(this);

        // WeaponSwitchSystem 에 알림
        weaponSwitchSystem.RemoveWeapon(this);

        // 무기 오브젝트 제거
        Destroy(gameObject);
    }

    public override void OnEquipped()
    {
        base.OnEquipped();
        base.Setup();
        if (movement == null) movement = GetComponentInParent<MovementCharacterController>();
        if (cameraRecoil != null && weaponSetting.recoilData != null)
            cameraRecoil.SetRecoilData(weaponSetting.recoilData);
    }

    private IEnumerator OnAttackLoop()
    {
        while (true)
        {
            OnAttack();

            yield return null;
        }
    }

    public void OnAttack()
    {
        if(!isEquipped) return;

        if ( Time.time - lasetAttackTime > weaponSetting.attackRate)
        {
            // 뛰고 있을 때는 공격할 수 없다
            if ( animator.MoveSpeed > 0.5f)
            {
                return;
            }

            // 공격주기가 되어야 공격할 수 있도록 하기 위해 현재 시간 저장
            lasetAttackTime = Time.time;

            // 탄 수가 없으면 공격 불가능
            if (weaponSetting.currentAmmo <= 0)
            {
                return;
            }

            // 공격시 currentAmmo 1 감소, 탄 수 UI 업데이트
            weaponSetting.currentAmmo--;
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

            // 무기 에니메이션 재생 (모드에 따라 AimFire or Fire 애니메이션 재생)
            string animation = animator.AimModeIs == true ? "AimFire" : "Fire";
            animator.Play(animation, -1, 0);

            // 총구 이펙트 재생 (default mode 일 떄만 재생)
            if(animator.AimModeIs == false) StartCoroutine("OnMuzzleFlashEffect");
            // 공격 사운드 재생
            PlaySound(audioClipFire);
            // 탄피 생성
            casingMemoryPool.SpawnCasing(casingSpawnPoint.position, transform.right);

            // 반동 데이터 가져오기
            float finalRecoilMod = GetFinalRecoilMod();
            float finalSpreadMod = GetFinalSpreadMod();

            // 카메라 반동 적용
            cameraRecoil.FireRecoil(finalRecoilMod);
            // 광선을 발사해 원하는 위치 공격 (+Impact Effect)
            TwoStepRaycast();

            // 탄퍼짐 증가
            currentSpread += weaponSetting.spreadIncreasePerShot * finalSpreadMod;
            currentSpread = Mathf.Clamp(currentSpread, weaponSetting.minSpread, weaponSetting.maxSpread);
        }
    }

    private IEnumerator OnMuzzleFlashEffect()
    {
        muzzleFlashEffect.SetActive(true);

        yield return new WaitForSeconds(weaponSetting.attackRate * 0.3f);

        muzzleFlashEffect.SetActive(false);
    }

    private IEnumerator OnModeChange()
    {
        float current = 0;
        float percent = 0;
        float time = 0.35f;

        animator.AimModeIs = !animator.AimModeIs;
        // 조준 상태 변경 알림
        onAimEvent.Invoke(animator.AimModeIs);

        float start = mainCamera.fieldOfView;
        float end = animator.AimModeIs == true ? aimModeFov : defaultModeFOV;

        isModeChange = true;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current/time;

            // mode에 따라 카메라의 시야각을 변경
            mainCamera.fieldOfView = Mathf.Lerp(start, end, percent);

            yield return null;
        }

        isModeChange = false;
    }

    private void ResetVariables()
    {
        isAttack = false;
        isModeChange = false;
        currentSpread = weaponSetting.minSpread;
    }

    private void TwoStepRaycast()
    {
        RaycastHit hit;
        Vector3 targetPoint;

        // 화면의 중앙 좌표 (Aim 기준으로 Raycast 연산 + 탄퍼짐)
        Vector3 spreadDirection = GetSpreadDirection();
        Ray ray = new Ray(mainCamera.transform.position,spreadDirection);

        // 공격 사거리(attackDistance) 안에 부딪히는 오브젝트가 있으면 targetPoint는 광선에 부딪힌 위치
        if ( Physics.Raycast(ray, out hit, weaponSetting.attackDistance))
        {
            targetPoint = hit.point;
        }
        // 공격 사거리 안에 부딪히는 오브젝트가 없으면 targetPoint는 최대 사거리 위치
        else
        {
            targetPoint = ray.origin + ray.direction*weaponSetting.attackDistance;
        }
        Debug.DrawRay(ray.origin, ray.direction * weaponSetting.attackDistance, Color.red);

        // 첫번째 Raycast연산으로 얻어진 targetPoint를 목표지점으로 설정하고,
        // 총구를 시작지점으로 하여 Raycast 연산
        Vector3 attackDirection = (targetPoint - bulletSpawnPoint.position).normalized;
        if ( Physics.Raycast(bulletSpawnPoint.position, attackDirection, out hit, weaponSetting.attackDistance))
        {
            impactMemoryPool.SpawnImpact(hit);

            if (hit.transform.CompareTag("ImpactEnemy"))
            {
                hit.transform.GetComponent<EnemyFSM>().TakeDamage(weaponSetting.damage);
            }
            else if (hit.transform.CompareTag("InteractionObject"))
            {
                hit.transform.GetComponent<InteractionObject>().TakeDamage(weaponSetting.damage);
            }
        }
        Debug.DrawRay(bulletSpawnPoint.position,attackDirection*weaponSetting.attackDistance, Color.blue);
    }

    private Vector3 GetSpreadDirection()
    {
        float spread = currentSpread;
        if (movement != null)
        {
            // 이동속도에 따른 패널티 추가
            float movementPenalty = movement.GetComponent<CharacterController>().velocity.magnitude;
            spread += movementPenalty * 0.01f;

            // 다이빙,슬라이딩은 추가 패널티
            if (movement.IsDiving || movement.IsSliding) spread *= 1.0f;

            else if (movement.IsCrouching) spread *= 0.7f;

            else if (movement.IsProne) spread *= 0.5f;
            // 조준 상태면 퍼짐 감소
            if (animator.AimModeIs)
            {
                spread *= weaponSetting.aimSpreadMultiplier;
            }
        }
        onCrossHairEvent.Invoke(spread);

        Vector2 random = Random.insideUnitCircle * spread;
        Vector3 direction =
            mainCamera.transform.forward +
            mainCamera.transform.right * random.x +
            mainCamera.transform.up * random.y;

        return direction.normalized;
    }
}
