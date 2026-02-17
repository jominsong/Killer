using UnityEngine;

[CreateAssetMenu(fileName = "RecoilData", menuName = "ScrriptableObjects/RecoilData")]
public class RecoilData : ScriptableObject
{
    [Header("Recoil Settings")]
    public float recoilX;  // 좌우 반동 범위
    public float recoilY;  // 수직 반동
    public float snappiness;  // 반동이 튀는 속도 (정비례)
    public float returnSpeed;  // 원래 위치로 돌아오는 속도

    [Header("Recoil Pattern")]
    // 시간에 따른 반동 변화를 그래프로 제어
    public AnimationCurve recoilPatternX;
}
