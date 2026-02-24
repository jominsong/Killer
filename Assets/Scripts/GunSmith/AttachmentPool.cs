using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AttachmentPool : MonoBehaviour
{
    public static AttachmentPool instance;

    [SerializeField]
    private List<WeaponAttachment> allAttachments;  // ÇÁ·ÎÁ§Æ®ÀÇ ¸ðµç ºÎÂø¹°À» µî·Ï

    private void Awake() => instance = this;

    // Æ¯Á¤ ½½·í¿¡ ¸Â´Â ·£´ý ÆÄÃ÷ 3°³¸¦ »Ì¾ÆÁÜ
    public List<WeaponAttachment> GetRandomAttachments(AttachmentSlot slot,int count = 3)
    {
        List<WeaponAttachment> filteredList = allAttachments
            .Where(a => a.slot == slot)
            .ToList();


        return filteredList.OrderBy(x => Random.value).Take(count).ToList();
    }

}
