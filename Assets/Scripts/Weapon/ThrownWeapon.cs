using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ThrownWeapon : MonoBehaviour
{
    private List<WeaponAttachment> savedAttachments = new List<WeaponAttachment>();

    [Header("Damage")]
    [SerializeField]
    private int damage = 30;

    [Header("Life Time")]
    [SerializeField]
    private float lifeTime = 8f;

    private bool hasHit = false;
    private Rigidbody rb;
    private Collider col;
    private WeaponBase wb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        wb = GetComponent<WeaponBase>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        if(collision.collider.CompareTag("ImpactEnemy"))
        {
            hasHit = true;

            EnemyFSM enemy = collision.collider.GetComponent<EnemyFSM>();
            
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 첫 충돌 이후 적 충돌 무시
            Physics.IgnoreCollision(col, collision.collider, true);
        }
        if(collision.collider.CompareTag("InteractionObject"))
        {
            InteractionObject obj = collision.collider.GetComponent<InteractionObject>();

            if (obj != null)
            {
                obj.TakeDamage(damage);
            }
        }

    }

    public void StoreAttachments(Dictionary<AttachmentSlot,WeaponAttachment> attachments)
    {
        savedAttachments = new List<WeaponAttachment>(attachments.Values);
    }

    public List<WeaponAttachment> GetSavedAttachments()
    {
        return savedAttachments;
    }

    public void SetSavedAttachments(List<WeaponAttachment> attachments)
    {
        savedAttachments = attachments;
    }

}
