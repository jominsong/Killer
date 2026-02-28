using UnityEngine;
using Unity.Cinemachine;

public class CameraEffects : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private CinemachineImpulseSource impulseSource;
    private Camera weaponCamera;
    private MovementCharacterController movement;
    private CharacterController characterController;

    [Header("FOV Settings")]
    // fov kick 
    [SerializeField]
    private float fovKickAmount = 2f;  // 사격시 순간적으로 늘어날 Fov 값
    [SerializeField]
    private float fovKickReturnSpeed = 10f;  // fov가 돌아오는 속도
    private float targetFOV;  // 최종 목표 FOV

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
    private float stepDistance = 2f;
    private float shakeTimer;
    private float distanceAccumulator;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        movement = GetComponentInParent<MovementCharacterController>();
        characterController = GetComponentInParent<CharacterController>();
        weaponCamera = GameObject.Find("Weapon Camera")?.GetComponent<Camera>();
        virtualCamera = GameObject.FindAnyObjectByType<CinemachineCamera>();

        targetFOV = virtualCamera.Lens.FieldOfView;
    }

    private void Update()
    {
        if (virtualCamera == null) return;

        // 현재 FOV에서 targetFOV로 부드럽게 회귀
        virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * fovKickReturnSpeed);
        weaponCamera.fieldOfView = virtualCamera.Lens.FieldOfView;

        // 틸트 계산
        HandleCameraTilt();

        // 흔들림 처리
        HandleMovementShake();
        if (shakeTimer > 0) shakeTimer -= Time.deltaTime;
    }

    public void PlayFireEffects(float intensity = 1f)
    {
        // FOV Kick
        virtualCamera.Lens.FieldOfView += fovKickAmount * intensity;

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

    public void SetTargetFOV(float fov) => targetFOV = fov;

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

    private void HandleMovementShake()
    {
        if (movement == null || impulseSource == null) return;

        // 실제 이동 속도 계산
        Vector3 horizontalVelocity = new Vector3(movement.GetComponent<CharacterController>().velocity.x, 0, movement.GetComponent<CharacterController>().velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // 공중에 있거나 너무 느리면 누적 거리 초기화 후 중단
        if (!movement.GetComponent<CharacterController>().isGrounded || currentSpeed < 0.1f)
        {
            distanceAccumulator = 0;
            return;
        }

        // 이동 거리 누적
        distanceAccumulator += currentSpeed * Time.deltaTime;

        if (shakeTimer <= 0)
        {
            if (movement.IsSliding)
            {
                impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;

                Vector3 randomVelocity = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0.02f);
                impulseSource.GenerateImpulseWithVelocity(randomVelocity);
                shakeTimer = 0.1f;
                distanceAccumulator = 0;
            }
            else if (movement.IsDiving)
            {
                impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
                impulseSource.GenerateImpulseWithVelocity(Random.insideUnitSphere * 0.02f);
                shakeTimer = 0.05f;
                distanceAccumulator = 0;
            }
            // 일반 이동(질주/걷기) 시 거리 기반 흔들림
            else if (distanceAccumulator >= stepDistance)
            {
                distanceAccumulator = 0;
                impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;

                float intensity = currentSpeed > 5f ? 1.2f : 0.6f;
                impulseSource.GenerateImpulseWithVelocity(new Vector3(0.1f * intensity, -0.1f * intensity, 0));

                // 연속 진동 방지용 아주 짧은 타이머
                shakeTimer = 0.1f;
            }
        }
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
}
