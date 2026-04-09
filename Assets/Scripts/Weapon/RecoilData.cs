using UnityEngine;

[CreateAssetMenu(fileName = "RecoilData", menuName = "ScriptableObjects/RecoilData")]
public class RecoilData : ScriptableObject
{
    [Header("Recoil Settings")]
    public float recoilX;  // 좌우 반동 범위
    public float recoilY;  // 수직 반동
    public float snappiness;  // 반동이 튀는 속도 (정비례)
    public float returnSpeed;  // 원래 위치로 돌아오는 속도

    [Header("Recoil Pattern")]
    public AnimationCurve recoilPatternX;  // 카메라 수평 패턴
    public AnimationCurve recoilPatternY;  // 비주얼 ADS 수직 패턴
    public AnimationCurve recoilPatternZ;

    [Header("ADS Visual Recoil")]
    public float recoveryDelay = 0.12f;  // 사격 중단 후 복귀 시작까지 딜레이
    public float overshootStrength = 0.3f;  // 복귀 시 반대 방향으로 살짝 넘어가는 강도

    // 원본 보호를 위한 인스턴스 복사 메서드
    public RecoilData Clone()
    {
        RecoilData clone = CreateInstance<RecoilData>();
        clone.recoilX = this.recoilX;
        clone.recoilY = this.recoilY;
        clone.snappiness = this.snappiness;
        clone.returnSpeed = this.returnSpeed;
        clone.recoilPatternX = this.recoilPatternX;
        clone.recoilPatternY = this.recoilPatternY;
        clone.recoilPatternZ = this.recoilPatternZ;
        clone.recoveryDelay = this.recoveryDelay;
        clone.overshootStrength = this.overshootStrength;
        return clone;
    }
}
