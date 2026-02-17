using UnityEngine;

public class TeleportInteractor : InteractionBase
{
    [Header("Teleport Settings")]
    [SerializeField]
    private Transform targetTransform;  // 이동할 목표 좌표

    public override void Use(GameObject entity)
    {
        if (targetTransform == null) return;

        CharacterController controller = entity.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;

            entity.transform.position = targetTransform.position;
            entity.transform.rotation = targetTransform.rotation;

            controller.enabled = true;
        }
        else
        {
            entity.transform.position = targetTransform.position;
            entity.transform.rotation = targetTransform.rotation;
        }
    }

    // 개발 편의용
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(targetTransform.position, 0.5f);
        Gizmos.DrawLine(targetTransform.position, targetTransform.position);
    }
}
