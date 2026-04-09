using UnityEngine;
using static Unity.Cinemachine.CinemachineFreeLookModifier;

public class CameraRecoil : MonoBehaviour
{
    private RecoilData recoilData;  // 현재 무기의 데이터
    private RotateToMouse rotateToMouse;  // 회전을 담당하는 컴포넌트

    private int shotCount = 0;
    private int maxAmmo;

    // 목표 반동값
    private float targetRecoilX;
    private float targetRecoilY;

    //  부드럽게 따라가는 반동값
    private float currentRecoilX;
    private float currentRecoilY;

    // 이전 프레임 currentRecoil (복귀량 계산용)
    private float prevRecoilX;
    private float prevRecoilY;

    private float timeSinceLastFire = 999f;
    private const float SHOT_RESET_DELAY = 0.5f;

    private void Awake()
    {
        rotateToMouse = GetComponentInParent<RotateToMouse>();
    }

    public void SetRecoilData(RecoilData newData)
    {
        recoilData = newData;
        ResetRecoil();
    }

    public void SetMaxAmmo(int ammo)
    {
        maxAmmo = Mathf.Max(1, ammo);
    }

    private void Update()
    {
        if (recoilData == null || rotateToMouse == null) return;

        timeSinceLastFire += Time.deltaTime;

        prevRecoilX = currentRecoilX;
        prevRecoilY = currentRecoilY;

        // current는 target을 부드럽게 추적
        currentRecoilX = Mathf.Lerp(currentRecoilX, targetRecoilX, recoilData.snappiness * Time.deltaTime);
        currentRecoilY = Mathf.Lerp(currentRecoilY, targetRecoilY, recoilData.snappiness * Time.deltaTime);

        float deltaX = currentRecoilX - prevRecoilX;
        float deltaY = currentRecoilY - prevRecoilY;

        rotateToMouse.AddRecoil(deltaX, deltaY);

        // 발사 중단 후 shotCount 리셋
        if (timeSinceLastFire > SHOT_RESET_DELAY)
        {
            currentRecoilX = 0f; currentRecoilY = 0f;
            targetRecoilX = 0f; targetRecoilY = 0f;
            shotCount = 0;
        }
    }

    public void FireRecoil(float vModifier = 1.0f, float hModifier = 1.0f)
    {
        if (recoilData == null || rotateToMouse == null) return;

        timeSinceLastFire = 0f;
        
        float recoilUp = -recoilData.recoilY * vModifier;
        float recoilSide = Random.Range(-recoilData.recoilX, recoilData.recoilX) * hModifier;

        targetRecoilX += recoilUp;
        targetRecoilY += recoilSide;

        shotCount++;
    }

    public void ResetRecoil()
    {
        targetRecoilX = 0f; targetRecoilY = 0f;
        currentRecoilX = 0f; currentRecoilY = 0f;
        prevRecoilX = 0f; prevRecoilY = 0f;
        shotCount = 0;
        timeSinceLastFire = 999f;
    }
}
