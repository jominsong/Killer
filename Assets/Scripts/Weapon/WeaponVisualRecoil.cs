using UnityEngine;

public class WeaponVisualRecoil : MonoBehaviour
{
    [Header("Smoothing")]
    [SerializeField] private float snappiness = 25.0f;
    [SerializeField] private float returnSpeed = 12.0f;

    [Header("Hip Fire - Model Recoil")]
    [SerializeField] private float kickbackAmount = 0.05f;
    [SerializeField] private float kickbackReturnSpeed = 10f;
    [SerializeField] private float tiltAmount = 3.0f;
    [SerializeField] private float muzzleRiseAmount = 1.5f;

    [Header("ADS - Pattern Recoil")]
    [SerializeField] private float adsRecoilMultiplier = 0.35f;
    [SerializeField] private float adsSnappiness = 20.0f;

    // Hip Fire
    private Quaternion hipTargetRotation = Quaternion.identity;
    private Quaternion hipCurrentRotation = Quaternion.identity;
    private Vector3 targetPosition = Vector3.zero;
    private Vector3 currentPosition = Vector3.zero;

    // ADS
    private RecoilData recoilData;
    private Quaternion adsRecoilTarget = Quaternion.identity;   // 목표 누적 반동
    private Quaternion adsCurrentRotation = Quaternion.identity;
    private int adsShotCount = 0;           // 연속 발사 횟수 (패턴 커브의 time 값으로 사용)
    private float timeSinceLastShot = 0f;   // 마지막 발사 후 경과 시간
    private bool isADSRecovering = false;   // 복귀 단계 진입 여부
    private int maxAmmo;

    private bool isADS = false;

    public void SetRecoilData(RecoilData data)
    {
        recoilData = data;
    }

    public void SetADSMode(bool ads)
    {
        isADS = ads;

        if (!ads)
        {
            // ADS → 지향사격 ADS 반동 초기화
            adsRecoilTarget = Quaternion.identity;
            adsCurrentRotation = Quaternion.identity;
            adsShotCount = 0;
            timeSinceLastShot = 0f;
            isADSRecovering = false;
        }
        else
        {
            // 지향사격 → ADS: 지향사격 반동 초기화
            hipTargetRotation = Quaternion.identity;
            hipCurrentRotation = Quaternion.identity;
            targetPosition = Vector3.zero;
        }
    }

    public void SetMaxAmmo(int ammo)
    {
        maxAmmo = Mathf.Max(1, ammo);
    }

    void Update()
    {
        if (isADS)
            UpdateADSRecoil();
        else
            UpdateHipFireRecoil();

        // 킥백 포지션 복귀 (지향사격)
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, kickbackReturnSpeed * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snappiness * Time.deltaTime);
        transform.localPosition = currentPosition;
    }

    private void UpdateHipFireRecoil()
    {
        hipTargetRotation = Quaternion.Slerp(hipTargetRotation, Quaternion.identity, returnSpeed * Time.deltaTime);
        hipCurrentRotation = Quaternion.Slerp(hipCurrentRotation, hipTargetRotation, snappiness * Time.deltaTime);
        transform.localRotation = hipCurrentRotation;
    }

    private void UpdateADSRecoil()
    {
        timeSinceLastShot += Time.deltaTime;

        // recoveryDelay 이후 복귀 시작
        if (recoilData != null && timeSinceLastShot > recoilData.recoveryDelay)
        {
            if (!isADSRecovering)
            {
                isADSRecovering = true;
                ApplyOvershoot(); // 복귀 시작 시 오버슈트 적용
            }

            // adsRecoilTarget을 identity로 복귀
            adsRecoilTarget = Quaternion.Slerp(
                adsRecoilTarget,
                Quaternion.identity,
                (recoilData != null ? recoilData.returnSpeed : returnSpeed) * Time.deltaTime
            );

            // 복귀가 거의 완료되면 shotCount 리셋
            if (Quaternion.Angle(adsRecoilTarget, Quaternion.identity) < 0.1f)
            {
                adsRecoilTarget = Quaternion.identity;
                adsShotCount = 0;
                isADSRecovering = false;
            }
        }

        adsCurrentRotation = Quaternion.Slerp(adsCurrentRotation, adsRecoilTarget, adsSnappiness * Time.deltaTime);
        transform.localRotation = adsCurrentRotation;
    }

    // 복귀 시작 시 반동 방향 반대로 살짝 추가 (오버슈트 느낌)
    private void ApplyOvershoot()
    {
        if (recoilData == null || recoilData.overshootStrength <= 0f) return;

        Vector3 euler = adsRecoilTarget.eulerAngles;
        float x = ClampAngle(euler.x);
        float y = ClampAngle(euler.y);

        // 현재 누적 반동의 반대 방향으로 살짝 밀어줌
        Quaternion overshoot = Quaternion.Euler(
            x * recoilData.overshootStrength,
            y * recoilData.overshootStrength,
            0f
        );
        adsRecoilTarget *= overshoot;
    }

    public void ApplyVisualRecoil(Vector2 spreadRandomPoint, float spreadAmount, float rotationIntensity)
    {
        if (isADS)
            ApplyADSRecoil(rotationIntensity);
        else
            ApplyHipFireRecoil(spreadRandomPoint, spreadAmount, rotationIntensity);
    }

    private void ApplyHipFireRecoil(Vector2 spreadRandomPoint, float spreadAmount, float rotationIntensity)
    {
        float recoilX = -spreadRandomPoint.y * spreadAmount * rotationIntensity * 10f;
        float recoilY = spreadRandomPoint.x * spreadAmount * rotationIntensity * 10f;
        float recoilZ = -spreadRandomPoint.x * spreadAmount * tiltAmount;
        recoilX -= muzzleRiseAmount * spreadAmount;

        hipTargetRotation = Quaternion.Euler(recoilX, recoilY, recoilZ);
        targetPosition = new Vector3(0f, 0f, -kickbackAmount * spreadAmount);
    }

    private void ApplyADSRecoil(float rotationIntensity)
    {
        if (recoilData == null) return;

        timeSinceLastShot = 0f;
        isADSRecovering = false;

        float normalizedTime = Mathf.Clamp01((float)adsShotCount / maxAmmo);

        float deltaX = recoilData.recoilPatternX.Evaluate(normalizedTime);

        float deltaY = (recoilData.recoilPatternY != null && recoilData.recoilPatternY.length > 0)
            ? recoilData.recoilPatternY.Evaluate(normalizedTime)
            : recoilData.recoilY;

        float deltaZ = (recoilData.recoilPatternZ != null && recoilData.recoilPatternZ.length > 0)
            ? recoilData.recoilPatternZ.Evaluate(normalizedTime)
            : -deltaX * 1f;

        float multiplier = adsRecoilMultiplier * rotationIntensity;

        adsRecoilTarget = Quaternion.Euler(
            -deltaY * multiplier,
             deltaX * multiplier,
             deltaZ * multiplier
        );

        targetPosition = new Vector3(0f, 0f, -kickbackAmount * rotationIntensity * 0.02f);

        adsShotCount++;
    }

    private float ClampAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}