using UnityEngine;
using Unity.Cinemachine;

public class BlenderCameraShake : CinemachineExtension
{
    [SerializeField] private Transform shakeTarget; // 팔 Rig의 카메라 본

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Aim) return;
        if (shakeTarget == null) return;

        state.PositionCorrection += state.RawOrientation * shakeTarget.localPosition;
        state.OrientationCorrection *= shakeTarget.localRotation;
    }
}