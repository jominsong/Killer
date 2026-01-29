using UnityEngine;


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
            ApplyStanceSpeed();  // 상태에 따라 실제 속도 적용
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
    private float currentDiveSpeed;

    public bool IsDiving => currentStance == Stance.Diving;
    public bool IsSliding => currentStance == Stance.Sliding;
    public bool IsStanding => currentStance == Stance.Standeing;
    public bool IsCrouching => currentStance == Stance.Crouching;
    public bool IsProne => currentStance == Stance.Prone;

    private enum Stance { Standeing, Crouching, Prone, Sliding,Diving}
    private Stance currentStance = Stance.Standeing; 

    private CharacterController characterController;  // 플레이어 이동 제어를 위한 컴포넌트

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        originalHeight = characterController.height;
        originalCenter = characterController.center;
    }

    private void Update()
    {
        if (currentStance == Stance.Sliding)
        {
            UpdateSliding();
        }
        else if (currentStance == Stance.Diving)
        {
            UpdateDiving();
        }
        else
        {
            if (!characterController.isGrounded) moveForce.y += gravity * Time.deltaTime;
            else if (moveForce.y < 0) moveForce.y = -2f; // 지면에 붙어있게 유지
        
        }
        // 1초당 moveForce 속력으로 이동
        characterController.Move(moveForce * Time.deltaTime);
    }

    public void MoveTo(Vector3 direction)
    {
        if (IsSliding || IsDiving) return;

        // 이동 방향 = 캐릭터의 회전 값 * 방향 값
        direction = transform.rotation * new Vector3(direction.x,0,direction.z);

        // 이동 힘 = 이동방향 * 속도
        moveForce = new Vector3(direction.x * moveSpeed, moveForce.y, direction.z * moveSpeed);
    }

    public void Jump()
    {
        // 플레이어가 바닥에 있을 때만 점프 가능
        if (characterController.isGrounded)
        {
            moveForce.y = jumpForce;
        }
    }

    public void ToggleCrouch()
    {
        if (currentStance == Stance.Crouching)
        {
            Stand();
        }
        else
        {
            Crouch();
        }
    }

    public void ToggleProne()
    {
        if (currentStance == Stance.Prone)
        {
            Stand();
        }
        else
        {
            Prone();
        }
    }

    public void StartSlide(Vector3 direction)
    {
        if (currentStance == Stance.Sliding || !CanSlide) return;

        currentStance = Stance.Sliding;
        slideTimer = slideDuration;
        slideDirection = direction.normalized;

        // 슬라이딩 시 높이를 앉기 높이와 동일하게 설정
        characterController.height = crouchHeight;
        characterController.center = new Vector3(originalCenter.x, crouchHeight / 5, originalCenter.z);
    }

    public void SlideCancel()
    {
        if ( currentStance == Stance.Sliding)
        {
            StopSlide();
        }
    }

    public void StartDive(Vector3 direction)
    {
        if (!characterController.isGrounded || IsDiving) return;

        currentStance = Stance.Diving;
        currentDiveSpeed = diveForwardForce;
        // 다이빙시 엎드리기 높이로 변경
        characterController.height = proneHeight;
        characterController.center = new Vector3(originalCenter.x, proneHeight / 1.5f, originalCenter.z);
        
        // 전방 및 상방 힘 계산
        slideDirection = direction.normalized;
        moveForce = direction.normalized * diveForwardForce;
        moveForce.y = diveUpwardForce;
    }

    public void Stand()
    {
        characterController.height = originalHeight;
        characterController.center = originalCenter;
        currentStance = Stance.Standeing;
        ApplyStanceSpeed();
    }

    private void Crouch()
    {
        characterController.height = crouchHeight;
        characterController.center = new Vector3(originalCenter.x, crouchHeight / 5, originalCenter.z);
        currentStance = Stance.Crouching;
        ApplyStanceSpeed();
    }

    private void Prone()
    {
        characterController.height = proneHeight;
        characterController.center = new Vector3(originalCenter.x, proneHeight / 1.5f, originalCenter.z);
        currentStance = Stance.Prone;
        ApplyStanceSpeed();
    }

    private void ApplyStanceSpeed()
    {
        switch (currentStance)
        {
            case Stance.Crouching:
                moveSpeed = baseMoveSpeed * crouchSpeedMultiplier;
                break;
            case Stance.Prone:
                moveSpeed = baseMoveSpeed * proneSpeedMultiplier;
                break;
            case Stance.Standeing:
            default:
                moveSpeed = baseMoveSpeed;
                break;
        }
    }

    private void UpdateSliding()
    {
        slideTimer -= Time.deltaTime;

        // 경사면 마찰력 조절 
        float speedMultiplier = 1.0f;
        if (Physics.Raycast(transform.position,Vector3.down,out RaycastHit hit , 1.5f,groundLayer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 5f) speedMultiplier = slopeSlideSpeedBonus;
        }

        // 시간에 따라 속도 감속
        float currentSlideSpeed = Mathf.Lerp(0, slideSpeed, slideTimer / slideDuration);
        moveForce = new Vector3(slideDirection.x * currentSlideSpeed, moveForce.y, slideDirection.z * currentSlideSpeed);

        if (slideTimer <= 0)
        {
            StopSlide();
        }
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

            // 공중에서는 감속 없이 전방 속도를 100% 유지
            Vector3 horizontalVel = slideDirection * currentDiveSpeed;
            moveForce.x = horizontalVel.x;
            moveForce.z = horizontalVel.z;
        }
        // 2. 지면 상태 (착지 후)
        else
        {
            // 착지 시 y축 힘 안정화
            if (moveForce.y < 0) moveForce.y = -2f;

            // 지면 마찰에 의한 감속 시작
            currentDiveSpeed = Mathf.MoveTowards(currentDiveSpeed, 0, diveDeceleration * Time.deltaTime);

            Vector3 horizontalVel = slideDirection * currentDiveSpeed;
            moveForce.x = horizontalVel.x;
            moveForce.z = horizontalVel.z;

            // 속도가 거의 멈췄을 때만 정지
            if (currentDiveSpeed <= 0.1f)
            {
                StopDive();
            }
        }
    }

    private void StopDive()
    {
        currentStance = Stance.Prone;
        moveForce = Vector3.zero; // 힘 초기화
        ApplyStanceSpeed(); // Prone 속도 적용
    }
}