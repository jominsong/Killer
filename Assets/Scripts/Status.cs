using UnityEngine;

[System.Serializable]
public class HPEvent : UnityEngine.Events.UnityEvent<float, float> { }

public class Status : MonoBehaviour
{
    [HideInInspector]
    public HPEvent onHPEnvet = new HPEvent();

    [Header("Walk,Run Speed")]
    [SerializeField]
    private float walkSpeed;
    [SerializeField]
    private float runSpeed;

    [Header("HP")]
    [SerializeField]
    private float maxHP = 100;
    private float currentHP;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    private void OnEnable()
    {
        currentHP = maxHP;
    }

    public bool DecreaseHP(float damage)
    {
        float previousHP = currentHP;

        currentHP = currentHP - damage > 0 ? currentHP - damage : 0;

        onHPEnvet.Invoke(previousHP, currentHP);

        if (currentHP == 0)
        {
            return true;
        }

        return false;
    }

    public void IncreaseHP(float hp)
    {
        float previousHP = currentHP;

        currentHP = currentHP + hp > maxHP ? maxHP : currentHP + hp;

        onHPEnvet.Invoke(previousHP, currentHP);
    }
}
