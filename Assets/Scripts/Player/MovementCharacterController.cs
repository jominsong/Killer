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
    private float slideFriction = 0.5f;  // 감속 계수
    private float slideTimer;
    private Vector3 slideDirection;

    public bool IsSliding => currentStance == Stance.Sliding;

    private enum Stance { Standeing, Crouching, Prone, Sliding}
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

        // 허공에 떠있으면 중력만큼 y축 이동속도 감소
        if ( !characterController.isGrounded)
        {
            moveForce.y += gravity * Time.deltaTime;
        }

        // 1초당 moveForce 속력으로 이동
        characterController.Move(moveForce * Time.deltaTime);
    }

    public void MoveTo(Vector3 direction)
    {
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
        if (currentStance == Stance.Sliding || !characterController.isGrounded) return;

        currentStance = Stance.Sliding;
        slideTimer = slideDuration;
        slideDirection = direction.normalized;

        // 슬라이딩 시 높이를 앉기 높이와 동일하게 설정
        characterController.height = crouchHeight;
        characterController.center = new Vector3(originalCenter.x, crouchHeight / 5, originalCenter.z);
    }

    private void Stand()
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
        ApplyStanceSpeed();
    }
}