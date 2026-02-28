using UnityEngine;

public abstract class InteractionObject : MonoBehaviour
{
    [Header("Interaction Object")]
    [SerializeField]
    protected float maxHP = 100;
    protected float currentHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public abstract void TakeDamage(float damage);
}
