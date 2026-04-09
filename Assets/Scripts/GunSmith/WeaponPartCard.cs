using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponPartCard : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI slotText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI costText;
    public Button purchaseButton;

    private WeaponAttachment currentAttachment;

    // 카드를 특정 부착물 데이터로 셋업
    public void SetupCard(WeaponAttachment attachment, bool canAfford)
    {
        if (iconImage == null || nameText == null) return;

        currentAttachment = attachment;

        // 아이콘
        if (iconImage != null)
            iconImage.sprite = attachment.attachmentIcon;

        // 이름
        nameText.text = attachment.attachmentName;

        // 슬롯 표시
        slotText.text = GetSlotDisplayName(attachment.slot);

        // 가격
        costText.text = $"{attachment.cost} 코인";
        costText.color = canAfford ? Color.white : Color.red;

        // 스탯
        BuildStatsText(attachment);

        // 구매 버튼 클릭 이벤트 연결
        purchaseButton.interactable = canAfford;
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnCardClicked);
    }

    private void BuildStatsText(WeaponAttachment a)
    {
        statsText.text = "";

        // 스탯 이름 / 배율 / 높을수록 좋은지 여부
        AddStatLine("데미지", a.damageMultiplier, good: true);
        AddStatLine("사거리", a.distanceMultiplier, good: true);
        AddStatLine("탄창", a.flattackRateMultiplier, good: true);
        AddStatLine("줌 속도", a.zoomSpeedMultiplier, good: true);
        AddStatLine("이동 속도", a.moveSpeedMultiplier, good: true);
        AddStatLine("수직 반동", a.verticalrecoilMultiplier, good: false);
        AddStatLine("수평 반동", a.horizontalrecoilMultiplier, good: false);
        AddStatLine("비주얼 반동", a.visualrecoilMultiplier, good: false);
        AddStatLine("최대 탄퍼짐", a.maxspreadMultiplier, good: false);
        AddStatLine("탄퍼짐 증가", a.increasespreadMultiplier, good: false);

        // 탄창 추가량 (배율이 아닌 정수)
        if (a.maxAmmoAdder != 0)
        {
            string color = a.maxAmmoAdder > 0 ? "#00FF88" : "#FF4444";
            string sign = a.maxAmmoAdder > 0 ? "▲" : "▼";
            statsText.text += $"<color={color}>{sign} 탄창 +{a.maxAmmoAdder}발</color>\n";
        }

        // 변화 없을 때
        if (string.IsNullOrEmpty(statsText.text))
            statsText.text = "<color=#888888>스탯 변화 없음</color>";
    }

    private void AddStatLine(string label, float multiplier, bool good)
    {
        // 1.0이면 변화 없음 → 표시 안 함
        if (Mathf.Approximately(multiplier, 1f)) return;

        float delta = (multiplier - 1f) * 100f;
        bool isUp = delta > 0f;
        bool isGood = good ? isUp : !isUp;   // 좋은 변화인지 판단

        string color = isGood ? "#00FF88" : "#FF4444";   // 초록 / 빨강
        string arrow = isUp ? "▲" : "▼";
        string amount = $"{Mathf.Abs(delta):F0}%";

        statsText.text += $"<color={color}>{arrow} {label}  {amount}</color>\n";
    }

    private string GetSlotDisplayName(AttachmentSlot slot)
    {
        switch (slot)
        {
            case AttachmentSlot.Muzle: return "[ 총구 ]";
            case AttachmentSlot.Grip: return "[ 그립 ]";
            case AttachmentSlot.Scope: return "[ 스코프 ]";
            case AttachmentSlot.Magazine: return "[ 탄창 ]";
            default: return "[ 기타 ]";
        }
    }

    private void OnCardClicked()
    {
        // gunsmithmanager에 구매 요청 전달
        GunSmithManager.Instance.PurchaseAttachment(currentAttachment);
    }
}
