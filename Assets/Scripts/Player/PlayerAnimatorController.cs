using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;

    private static readonly int StanceHash = Animator.StringToHash("stance");

    private void Awake()
    {
        // "Player" 오브젝트 기준으로 자식 오브젝트인
        // "arms_assault_rifle_01" 오브젝트에 Animator 컴포넌트가 있다
        animator = GetComponentInChildren<Animator>();
    }

    public float MoveSpeed
    {
        set => animator.SetFloat("movementSpeed", value, 0.1f, Time.deltaTime);
        get => animator.GetFloat("movementSpeed");
    }

    // Assault Rifle 마우스 오른쪽 클릭 액션 (default/aim mode)
    public bool AimModeIs
    {
        set => animator.SetBool("isAimMode",value);
        get => animator.GetBool("isAimMode");
    }

    public void Play(string stateName,int layer,float normalizedTime)
    {
        animator.Play(stateName,layer,normalizedTime);
    }

    public bool CurrentAnimationIs(string name)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }

    public void SetFloat(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    public bool IsCrouching
    {
        set => animator.SetFloat(StanceHash, value ? -1f :
           animator.GetFloat(StanceHash) == -2f ? -2f : 0f);
    }

    public bool IsProne
    {
        set => animator.SetFloat(StanceHash, value ? -2f : 0f);
    }

    public bool IsGrounded
    {
        set => animator.SetBool("isGrounded", value);
    }

    public void PlaySlide() => animator.SetTrigger("Slide");
    public void PlayDive() => animator.SetTrigger("Dive");
    public void PlayThrow() => animator.SetTrigger("Throw");
    public void PlayMelee(int index)
    {
        animator.SetInteger("MeleeIndex", index);
        animator.SetTrigger("Melee");
    }
    public void PlayAimIn(bool isAiming)
    {
        if (isAiming)
        {
            animator.SetFloat("AimSpeed", 1f);
            animator.Play("aim_in", 1, 0f);   // 정방향
        }
        else
        {
            animator.SetFloat("AimSpeed", -1f);
            animator.Play("aim_in", 1, 1f);   // 끝에서부터 역방향
        }
    }
}