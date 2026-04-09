using System.Net.NetworkInformation;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Input KeyCodes")]
    [SerializeField]
    private KeyCode keyCodeRun = KeyCode.LeftShift; // 달리기 키
    [SerializeField]
    private KeyCode keyCodeJump = KeyCode.Space;  // 점프 키
    [SerializeField]
    private KeyCode keyCodeThrow = KeyCode.R;  // 무기 던지기 키
    [SerializeField]
    private KeyCode keyCodeCrouch = KeyCode.LeftControl;  // 앉기 키
    [SerializeField]
    private KeyCode keyCodeProne = KeyCode.C;  // 엎드리기 키

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipWalk;  // 걷기 사운드
    [SerializeField]
    private AudioClip audioClipRun;  // 달리기 사운드

    private RotateToMouse rotateToMouse;  // 마우스 이동으로 카메라 회전
    private MovementCharacterController movement;  // 키보드 입력으로 플레이어 이동,점프
    private CharacterController characterController;  // 플레이어 이동 제어
    private CrosshairUi crosshairUi;  // 크로스 헤어
    private Status status;  // 이동속도 등의 플레이어 정보
    private AudioSource audioSource;  // 사운드 재생 제어
    private WeaponBase weapon;  // 모든 무기가 상속받는 기반 클래스
    
    public bool isRun = false; 

    private void Awake()
    {
        // 마우스 커서를 보이지 않게 설정,현재 위치에 고정한다
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        rotateToMouse = GetComponentInChildren<RotateToMouse>();
        movement = GetComponent<MovementCharacterController>();
        status = GetComponent<Status>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        crosshairUi = FindAnyObjectByType<CrosshairUi>();
    }

    private void Update()
    {
        UpdateRotate();
        UpdateMove();
        UpdateJump();
        UpdateCrouchAndProne();
        UpdateWeaponAction();
        UpdateAnimatorStates();
    }

    private void UpdateRotate()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        rotateToMouse.UpdateRotate(mouseX, mouseY);
    }

    private void UpdateMove()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 이동중 일 때 (걷기 or 뛰기)
        if (x != 0 || z != 0)
        {
            // 옆이나 뒤로 이동할 때는 달릴 수 없다
            bool canRun = z > 0
                 && movement.IsFullyStanding
                 && !movement.IsSliding
                 && !movement.IsDiving
                 && (weapon == null || !weapon.Animator.AimModeIs);

            isRun = canRun && Input.GetKey(keyCodeRun);

            if (Input.GetKey(keyCodeRun))
                if (movement.IsCrouching || movement.IsProne)
                    movement.Stand();

            movement.MoveSpeed = isRun ? status.RunSpeed : status.WalkSpeed;

            if (weapon != null)
                weapon.Animator.MoveSpeed = isRun ? 1f : 0.5f;

            AudioClip targetClip = isRun ? audioClipRun : audioClipWalk;
            if (audioSource.clip != targetClip)
            {
                audioSource.clip = targetClip;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        // 제자리에 멈춰있을 때
        else
        {
            isRun = false;
            movement.MoveSpeed = 0;

            if (weapon != null)
                weapon.Animator.MoveSpeed = 0;
            // 멈췄을 때 사운드가 재생중이면 정지
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        movement.MoveTo(new Vector3(x, 0, z));
    }

    private void UpdateJump()
    {
        if (!Input.GetKeyDown(keyCodeJump)) return;

        // 슬라이딩 캔슬 로직
        if (movement.IsSliding)
        {
            movement.SlideCancel();  // 슬켄
            movement.ToggleCrouch();  // 즉시 서기로 전환
        }
        else if (!movement.IsStanding && characterController.isGrounded)
        {
            movement.Stand();
        }
        else
        {
            movement.Jump();
        }
    }

    private void UpdateCrouchAndProne()
    {
        bool isRunning = Input.GetKey(keyCodeRun)
                      && Input.GetAxisRaw("Vertical") > 0
                      && movement.IsFullyStanding;

        if (Input.GetKeyDown(keyCodeCrouch))
        {
            if (movement.IsDiving) return;
            if (isRunning && movement.CanSlide)
            {
                movement.StartSlide(transform.forward);
                weapon?.Animator.PlaySlide();
            }
            else
                movement.ToggleCrouch();
        }

        if (Input.GetKeyDown(keyCodeProne))
        {
            if (isRunning && movement.CanDive)
            {
                movement.StartDive(transform.forward);
                weapon?.Animator.PlayDive();
            }
            else if (movement.IsSliding)
            {
                movement.SlideCancel();
                movement.ToggleCrouch();
            }
            else if (!movement.IsDiving && !isRunning)
                movement.ToggleProne();
        }
    }

    private void UpdateAnimatorStates()
    {
        if (weapon == null) return;
        weapon.Animator.IsGrounded = characterController.isGrounded;
        weapon.Animator.IsCrouching = movement.IsCrouching;
        weapon.Animator.IsProne = movement.IsProne;
    }

    private void UpdateWeaponAction()
    {
        if (weapon == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            weapon.StartWeaponAction();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            weapon.StopWeaponAction();
        }

        if (Input.GetMouseButtonDown(1))
        {
            weapon.StartWeaponAction(1);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            weapon.StopWeaponAction(1);
        }

        if (Input.GetKeyDown(keyCodeThrow))
        {
            weapon.Animator.PlayThrow();
            weapon.ThrowWeapon();
            crosshairUi?.SetWeaponMode(false);
        }
    }

    public void TakeDamage(int damage)
    {
        bool isDie = status.DecreaseHP(damage);

        if (isDie == true)
        {
            Debug.Log("GameOver");
        }
    }

    public void SwitchingWeapon(WeaponBase newweapon)
    {
        weapon = newweapon;
        crosshairUi?.SetWeaponMode(newweapon != null);
    }
}