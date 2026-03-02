using UnityEngine;
using Unity.Cinemachine;

public class CameraEffects : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private CinemachineImpulseSource impulseSource;
    private CinemachineBasicMultiChannelPerlin noise;
    private Camera weaponCamera;

    private MovementCharacterController movement;
    private CharacterController characterController;
    private PlayerController playerController;

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
    [SerializeField]
    private float lerpSpeed = 5f;
    [SerializeField]
    private float bobFrequency = 12f;      // 발걸음 속도
    [SerializeField]
    private float bobVerticalAmount = 0.08f;  // 위아래 폭
    [SerializeField]
    private float bobHorizontalAmount = 0.05f; // 좌우 폭
    [SerializeField]
    private float bobSmoothSpeed = 10f;
    private float shakeTimer;
    private float distanceAccumulator;
    private float bobTimer;
    private Vector3 currentBobOffset;
    private Vector3 initialPosition;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        weaponCamera = GameObject.Find("Weapon Camera")?.GetComponent<Camera>();
        virtualCamera = GetComponent<CinemachineCamera>();
        noise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

        movement = GetComponentInParent<MovementCharacterController>();
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController>();

        targetFOV = virtualCamera.Lens.FieldOfView;
        initialPosition = transform.localPosition;
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
        HandleVHeadBob();
        HandleNoise();
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
            if (movement.IsDiving)
            {
                impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
                impulseSource.GenerateImpulseWithVelocity(Random.insideUnitSphere * 0.02f);
                shakeTimer = 0.05f;
                distanceAccumulator = 0;
            }
            // 일반 이동(질주/걷기) 시 거리 기반 흔들림
            else if (playerController.isRun && distanceAccumulator >= stepDistance)
            {
                distanceAccumulator = 0;

                impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
                float intensity = 0.3f;
                impulseSource.GenerateImpulseWithVelocity(new Vector3(0.1f * intensity, -0.1f * intensity, 0));

                shakeTimer = 0.1f;
            }
            else if (distanceAccumulator >= stepDistance)
            {
                distanceAccumulator = 0;
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

    private void HandleNoise()
    {
        if (movement.IsSliding && characterController.isGrounded)
        {
            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, 1.2f, Time.deltaTime * lerpSpeed);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, 1.5f, Time.deltaTime * lerpSpeed);
        }
        else
        { 
            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, 0, Time.deltaTime * lerpSpeed);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, 0, Time.deltaTime * lerpSpeed);
        }
    }

    private void HandleVHeadBob()
    {
        Vector3 speed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);

        // 달릴 때만 작동
        if (playerController.isRun && characterController.isGrounded && speed.magnitude > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrequency;

            // 수직 V자 움직임
            float vBob = Mathf.Abs(Mathf.Sin(bobTimer * 0.5f)) * bobVerticalAmount;

            // 좌우 움직임 (좌-우로 부드럽게 왔다갔다)
            float hBob = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmount;

            currentBobOffset = new Vector2(hBob, -vBob);

            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, 0.5f, Time.deltaTime * lerpSpeed);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, 1.5f, Time.deltaTime * lerpSpeed);
        }
        else
        {
            // 멈추면 서서히 (0,0)로
            bobTimer = 0;
            currentBobOffset = Vector2.Lerp(currentBobOffset, Vector2.zero, Time.deltaTime * bobSmoothSpeed);

            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, 0, Time.deltaTime * lerpSpeed);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, 0, Time.deltaTime * lerpSpeed);
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + currentBobOffset, Time.deltaTime * bobSmoothSpeed);

    }
}

