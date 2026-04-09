using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AttachmentPool : MonoBehaviour
{
    public static AttachmentPool instance;

    [SerializeField]
    private List<WeaponAttachment> allAttachments;  // 프로젝트의 모든 부착물을 등록

    private void Awake() => instance = this;

    // 무기군 + 슬롯 동시 필터링
    public List<WeaponAttachment> GetRandomAttachments(WeaponType weaponType, int count = 3)
    {
        List<WeaponAttachment> filtered = allAttachments
         .Where(a => System.Array.Exists(a.allowedWeaponTypes, t => t == weaponType))
         .ToList();

        return filtered.OrderBy(x => Random.value).Take(count).ToList();
    }

}
