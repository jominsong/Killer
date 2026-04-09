using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Analytics;


[RequireComponent(typeof(CharacterController))]
public class MovementCharacterController : MonoBehaviour
{
    [Header("move,jump")]
    [SerializeField]
    private float moveSpeed;  //이동속도
    private float baseMoveSpeed = 0f;
    private Vector3 moveForce;  //이동 힘 (x,z와 y축을 별도로 계산해 실제 이동에 적용)

    public float MoveSpeed
    {
        set
        {
            baseMoveSpeed = Mathf.Max(0, value);
            if (!isTransitioning)
                ApplyStanceSpeed();
            else
                ApplyTransitionSpeed(); // 전환 중엔 이전 자세 속도 유지
        }
        get => moveSpeed;
    }

    [SerializeField]
    private float jumpForce;  // 점프 힘
    [SerializeField]
    private float gravity;  // 중력 계수

    [Header("Stance")]
    private float originalHeight;
    private Vector3 originalCenter;
    [SerializeField]
    private float crouchHeight = 1.0f;  // 앉기시 높이
    [SerializeField]
    private float crouchSpeedMultiplier = 0.5f;
    [SerializeField]
    private float proneHeight = 0.5f;  // 엎드리기시 높이
    [SerializeField]
    private float proneSpeedMultiplier = 0.25f;
    [SerializeField]
    private float stanceSmoothSpeed = 10f;  // 전환 속도
    [SerializeField]
    private float crouchStandSpeed = 6f;
    [SerializeField]
    private float proneStandSpeed = 3f;

    private float targetHeight;
    private float currentSmoothSpeed;

    // 서기 전환 중 이전 자세 추적
    private Stance previousStance = Stance.Standing;
    private bool isTransitioning = false;

    public bool IsFullyStanding => IsStanding && !isTransitioning
                             && Mathf.Abs(characterController.height - originalHeight) < 0.05f;

    [Header("Sliding")]
    [SerializeField]
    private float slideSpeed = 12f;  // 슬라이딩 초기 속도
    [SerializeField]
    private float slideDuration = 0.5f;  // 슬라이딩 지속 시간
    [SerializeField]
    private float slopeSlideSpeedBonus = 1.5f;  // 경사면 가속
    [SerializeField]
    private LayerMask groundLayer;  // 지면 레이어
    [SerializeField]
    private float slideCooldown = 1.2f;  // 슬라이딩 재사용 대기시간
    private float lastSlideTime = -99f;
    private float slideTimer;
    private Vector3 slideDirection;
    public bool CanSlide => Time.time >= lastSlideTime + slideCooldown && characterController.isGrounded;

    [Header("Diving")]
    [SerializeField]
    private float diveForwardForce = 15f;  // 다이빙 가속
    [SerializeField]
    private float diveUpwardForce = 4f;  // 수직 상승량
    [SerializeField]
    private float diveDeceleration = 10f;  // 착지시 감속량
    [SerializeField]
    private float diveCooldown = 1.2f;  // 다이빙 재사용 대기시간
    private float lastDiveTime = -99f;
    private float currentDiveSpeed;

    public bool CanDive => characterController.isGrounded
                    && IsStanding
                    && !isTransitioning
                    && Time.time >= lastDiveTime + diveCooldown;

    public bool IsDiving => currentStance == Stance.Diving;
    public bool WasInAir { get; private set; }
    public bool IsSliding => currentStance == Stance.Sliding;
    public bool IsStanding => currentStance == Stance.Standing;
    public bool IsCrouching => currentStance == Stance.Crouching;
    public bool IsProne => currentStance == Stance.Prone;

    private enum Stance { Standing, Crouching, Prone, Sliding, Diving }
    private Stance currentStance = Stance.Standing;

    private CharacterController characterController;  // 플레이어 이동 제어를 위한 컴포넌트
    private CameraEffects cameraEffects;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        cameraEffects = GetComponentInChildren<CameraEffects>();

        originalHeight = characterController.height;
        originalCenter = characterController.center;
        targetHeight = characterController.height;
    }

    private void Update()
    {
        UpdateStanceSmoothly();  // 매 프레임 높이 보간

        switch (currentStance)
        {
            case Stance.Sliding: UpdateSliding(); break;
            case Stance.Diving: UpdateDiving(); break;
            default:
                if (!characterController.isGrounded)
                    moveForce.y += gravity * Time.deltaTime;
                else if (moveForce.y < 0)
                    moveForce.y = -2f;// 지면에 붙어있게 유지
                break;
        }
        // 1초당 moveForce 속력으로 이동
        characterController.Move(moveForce * Time.deltaTime);
    }

    public void MoveTo(Vector3 direction)
    {
        if (IsSliding || IsDiving) return;

        direction = transform.rotation * new Vector3(direction.x, 0, direction.z);

        if (direction.sqrMagnitude > 0.01f)
        {
            Vector3 moveDir = direction.normalized * moveSpeed;
            moveForce.x = moveDir.x;
            moveForce.z = moveDir.z;
        }
        else
        {
            moveForce.x = 0f;
            moveForce.z = 0f;
        }
    }

    public void Jump()
    {
        // 플레이어가 바닥에 있을 때만 점프 가능
        if (characterController.isGrounded)
            moveForce.y = jumpForce;
    }

    public void ToggleCrouch()
    {
        if (currentStance == Stance.Crouching) Stand();
        else Crouch();
    }

    public void ToggleProne()
    {
        if (currentStance == Stance.Prone) Stand();
        else Prone();
    }

    public void StartSlide(Vector3 direction)
    {
        if (currentStance == Stance.Sliding || !CanSlide) return;

        currentStance = Stance.Sliding;
        slideTimer = slideDuration;
        slideDirection = direction.normalized;
        cameraEffects.PlaySlideTiltKick();
        // 슬라이딩 시 높이를 앉기 높이와 동일하게 설정
        targetHeight = crouchHeight;
    }

    public void SlideCancel()
    {
        if (currentStance == Stance.Sliding) StopSlide();
    }

    public void StartDive(Vector3 direction)
    {
        if (!CanDive) return;

        currentStance = Stance.Diving;
        currentDiveSpeed = diveForwardForce;
        cameraEffects.PlayDiveTiltKick();

        // 다이빙시 엎드리기 높이로 변경
        targetHeight = proneHeight;
        lastDiveTime = Time.time;

        // 전방 및 상방 힘 계산
        slideDirection = direction.normalized;
        moveForce = direction.normalized * diveForwardForce;
        moveForce.y = diveUpwardForce;
    }

    public void Stand()
    {
        if (currentStance == Stance.Standing) return;

        previousStance = currentStance;
        currentStance = Stance.Standing;
        targetHeight = originalHeight;
        isTransitioning = true;

        currentSmoothSpeed = previousStance == Stance.Prone
            ? proneStandSpeed
            : crouchStandSpeed;

        // 전환 시작 시 이전 자세 속도 즉시 반영
        ApplyTransitionSpeed();
    }

    private void Crouch()
    {
        previousStance = currentStance;
        currentStance = Stance.Crouching;
        targetHeight = crouchHeight;
        isTransitioning = false;
        currentSmoothSpeed = stanceSmoothSpeed;
        ApplyStanceSpeed();
    }

    private void Prone()
    {
        previousStance = currentStance;
        currentStance = Stance.Prone;
        targetHeight = proneHeight;
        isTransitioning = false;
        currentSmoothSpeed = stanceSmoothSpeed;
        ApplyStanceSpeed();
    }

    private void ApplyStanceSpeed()
    {
        switch (currentStance)
        {
            case Stance.Standing:
                moveSpeed = baseMoveSpeed;
                break;
            case Stance.Crouching:
                moveSpeed = baseMoveSpeed * crouchSpeedMultiplier;
                break;
            case Stance.Prone:
                moveSpeed = baseMoveSpeed * proneSpeedMultiplier;
                break;
        }
    }

    private void UpdateSliding()
    {
        slideTimer -= Time.deltaTime;

        // 경사면 마찰력 조절 
        float speedMultiplier = 1.0f;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f, groundLayer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 5f) speedMultiplier = slopeSlideSpeedBonus;
        }

        // 시간에 따라 속도 감속
        float currentSlideSpeed = Mathf.Lerp(0, slideSpeed * speedMultiplier, slideTimer / slideDuration);
        moveForce.x = slideDirection.x * currentSlideSpeed;
        moveForce.z = slideDirection.z * currentSlideSpeed;

        if (!characterController.isGrounded)
            moveForce.y += gravity * Time.deltaTime;
        else if (moveForce.y < 0)
            moveForce.y = -2f;

        if (slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    {
        // 슬라이딩 종료 후 자동으로 앉기 상태로 전환
        currentStance = Stance.Crouching;
        lastSlideTime = Time.time;  // 슬라이딩 종료 시점부터 쿨타임 게산
        ApplyStanceSpeed();
    }

    private void UpdateDiving()
    {
        // 공중 상태
        if (!characterController.isGrounded)
        {
            moveForce.y += gravity * Time.deltaTime;
            WasInAir = true;
            // 공중에서는 감속 없이 전방 속도를 100% 유지
            Vector3 horizontalVel = slideDirection * currentDiveSpeed;
            moveForce.x = horizontalVel.x;
            moveForce.z = horizontalVel.z;
        }
        // 지면 상태 (착지 후)
        else
        {
            // 착지 시 y축 힘 안정화
            if (moveForce.y < 0) moveForce.y = -2f;

            if (WasInAir)
            {
                OnLandingTrigger();
                WasInAir = false;
            }

            // 지면 마찰에 의한 감속 시작
            currentDiveSpeed = Mathf.MoveTowards(currentDiveSpeed, 0, diveDeceleration * Time.deltaTime);

            Vector3 horizontalVel = slideDirection * currentDiveSpeed;
            moveForce.x = horizontalVel.x;
            moveForce.z = horizontalVel.z;

            // 속도가 거의 멈췄을 때만 정지
            if (currentDiveSpeed <= 0.1f)
                StopDive();
        }
    }

    private void StopDive()
    {
        currentStance = Stance.Prone;
        moveForce = Vector3.zero; // 힘 초기화
        ApplyStanceSpeed(); // Prone 속도 적용
    }

    private void OnLandingTrigger()
    {
        if (cameraEffects != null)
        {
            // Y축 강한 충격(착지), Z축 밀림(관성)
            cameraEffects.PlayDiveShock();
        }
    }

    private void UpdateStanceSmoothly()
    {
        float heightDiff = Mathf.Abs(characterController.height - targetHeight);

        // 전환 중 매 프레임 속도 강제 유지 (MoveSpeed setter 덮어쓰기 방어)
        if (isTransitioning)
            ApplyTransitionSpeed();

        if (heightDiff < 0.01f)
        {
            characterController.height = targetHeight;

            if (isTransitioning)
            {
                isTransitioning = false;
                ApplyStanceSpeed(); // 완전히 일어난 후 정상 속도 적용
            }
            return;
        }

        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            currentSmoothSpeed * Time.deltaTime
        );
    }

    private void ApplyTransitionSpeed()
    {
        moveSpeed = previousStance == Stance.Prone
            ? baseMoveSpeed * proneSpeedMultiplier
            : previousStance == Stance.Crouching
                ? baseMoveSpeed * crouchSpeedMultiplier
                : baseMoveSpeed;
    }
}