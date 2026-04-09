using Unity.Cinemachine;
using UnityEngine;
using static UnityEditor.SceneView;

public class CameraEffects : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private CinemachineImpulseSource impulseSource;
    private CinemachineBasicMultiChannelPerlin noise;
    private Camera weaponCamera;
    private BlenderCameraShake blenderShake;

    private MovementCharacterController movement;
    private CharacterController characterController;
    private PlayerController playerController;

    [Header("FOV Settings")]
    // fov kick 
    [SerializeField]
    private float fovKickAmount = 2f;  // 사격시 순간적으로 늘어날 Fov 값
    [SerializeField]
    private float fovKickReturnSpeed = 10f;  // fov가 돌아오는 속도
    private float adsFovSpeed = 10f;  // 조준 전환 속도
    private float targetFOV;  // 최종 목표 FOV
    private enum FovMode { Idle, AdsTransition, KickReturn }
    private FovMode fovMode = FovMode.Idle;

    [Header("Slide Tilt Settings")]
    [SerializeField]
    private float movementTiltAngle = 0.01f;  // 좌우 이동시 기울기
    [SerializeField]
    private float sldiesmoothSpeed = 15;  // 기울어지는 속도 (낮을수록 부드러움)
    [SerializeField]
    private float slidereturnSpeed = 10f;  // 돌아오는 속도
    private float slideTiltKick;

    [Header("Movement Settings")]
    [SerializeField]
    private float lerpSpeed = 5f;
    private float shakeTimer;

    private void Awake()
    {
        CameraSetting();

        movement = GetComponentInParent<MovementCharacterController>();
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController>();

        targetFOV = virtualCamera.Lens.FieldOfView;
    }

    private void Update()
    {
        if (virtualCamera == null) return;

        if (fovMode == FovMode.AdsTransition)
        {
            virtualCamera.Lens.FieldOfView = Mathf.MoveTowards(
                virtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * adsFovSpeed);

            if (Mathf.Approximately(virtualCamera.Lens.FieldOfView, targetFOV))
                fovMode = FovMode.Idle;
        }
        else
        {
            // KickReturn + Idle 둘 다 Lerp로 처리
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * fovKickReturnSpeed);
        }

        weaponCamera.fieldOfView = virtualCamera.Lens.FieldOfView;

        // 틸트 계산
        HandleCameraTilt();

        // 흔들림 처리
        if (shakeTimer > 0) shakeTimer -= Time.deltaTime;
        HandleNoise();
    }

    public void PlayFireEffects(float intensity = 1f)
    {
        // FOV Kick
        virtualCamera.Lens.FieldOfView += fovKickAmount * intensity;
        fovMode = FovMode.KickReturn;

        impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
        impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = 0.1f;

        // 카메라 흔들림 생성
        if (impulseSource != null)
        {
            Vector3 randomVelocity = new Vector3(
                Random.Range(-0.03f, 0.03f),
                Random.Range(-0.03f, 0.03f),
                -0.05f
            ) * intensity;
            impulseSource.GenerateImpulseWithVelocity(randomVelocity);
        }
    }

    public void SetTargetFOV(float fov, float speed = -1f)
    {
        targetFOV = fov;
        if (speed > 0f)
        {
            adsFovSpeed = speed * 100;
            fovMode = FovMode.AdsTransition;
        }
    }

    public float GetCurrentFOV() => virtualCamera != null ? virtualCamera.Lens.FieldOfView : 60f;

    public void PlaySlideTiltKick()
    {
        slideTiltKick = -5f;
    }

    public void PlayDiveTiltKick()
    {
        impulseSource.GenerateImpulseWithVelocity(new Vector3(0, 0.2f, -0.05f));
    }

    public void PlayDiveShock()
    {
        var definition = impulseSource.ImpulseDefinition;
        definition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;

        definition.TimeEnvelope.AttackTime = 0.01f;  // 즉각적인 충격
        definition.TimeEnvelope.SustainTime = 0.5f;  // 충격 유지 시간
        definition.TimeEnvelope.DecayTime = 1f;  // 충격이 사라지는 시간

        impulseSource.GenerateImpulseWithVelocity(new Vector3(0.1f, -0.4f, -0.05f));
    }

    private void HandleCameraTilt()
    {
        // 슬라이딩 카메라 틸트 설정
        // 이동 방향에 따른 미세한 기울기
        float sideSpeed = transform.InverseTransformDirection(characterController.velocity).x;
        float targetDutch = -sideSpeed * movementTiltAngle;

        // 슬라이딩 할때 틸트 상태
        slideTiltKick = Mathf.MoveTowards(slideTiltKick, 0, Time.deltaTime * slidereturnSpeed);
        virtualCamera.Lens.Dutch = Mathf.Lerp(virtualCamera.Lens.Dutch, targetDutch + slideTiltKick, Time.deltaTime * sldiesmoothSpeed);
    }

    private void HandleNoise()
    {
        if (movement.IsSliding && characterController.isGrounded)
        {
            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, 1f, Time.deltaTime * lerpSpeed);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, 1.5f, Time.deltaTime * lerpSpeed);
        }
        else
        { 
            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, 0, Time.deltaTime * lerpSpeed);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, 0, Time.deltaTime * lerpSpeed);
        }
        //blenderShake?.SetRunning(playerController.isRun && characterController.isGrounded);
    }

    private void CameraSetting()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        weaponCamera = GameObject.Find("Weapon Camera")?.GetComponent<Camera>();
        virtualCamera = GetComponent<CinemachineCamera>();
        noise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        blenderShake = virtualCamera.GetComponent<BlenderCameraShake>();
    }

}

