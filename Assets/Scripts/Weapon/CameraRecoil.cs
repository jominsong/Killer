using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private RecoilData recoilData;  // 현재 무기의 데이터
    private float fireDuration = 0f;

    private RotateToMouse rotateToMouse;  // 회전을 담당하는 컴포넌트

    private void Awake()
    {
        rotateToMouse = GetComponentInParent<RotateToMouse>();
    }

    public void SetRecoilData(RecoilData newData)
    {
        recoilData = newData;
    }

    private void Update()
    {
        if (recoilData == null) return;

        // 돌아오는 회전 계산
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, recoilData.returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, recoilData.snappiness * Time.deltaTime);

        // 최종적으로 카메라 Transform에 적용 (기존 마우스 로직에 더해짐)
        if (rotateToMouse != null)
        {
            rotateToMouse.SetRecoilRotaion(currentRotation);
        }

        if (targetRotation.magnitude < 0.1f) fireDuration = 0f;
    }

    public void FireRecoil(float Ymodifier = 1.0f, float Xmodifier = 1.0f)
    {
        if (recoilData == null) return;
        fireDuration += Time.deltaTime;

        // 사격 지속 시간에 따라 그래프의 값을 가져옴
        float patternMultiplier = recoilData.recoilPatternX.Evaluate(fireDuration);

        // 수직은 위로(음수), 좌우는 랜덤하게 튀도록 설정
        float recoilX = Random.Range(-recoilData.recoilX, recoilData.recoilX) * Xmodifier + patternMultiplier;
        float recoilY = recoilData.recoilY * Ymodifier;

        targetRotation += new Vector3(-recoilY , recoilX , 0);
    }
}
