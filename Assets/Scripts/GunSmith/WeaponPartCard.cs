using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponPartCard : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI costText;
    public Button purchaseButton;

    private WeaponAttachment currentAttachment;

    // 카드를 특정 부착물 데이터로 셋업
    public void SetupCard(WeaponAttachment attachment, bool canAfford)
    {
        if (iconImage == null || nameText == null) return;

        currentAttachment = attachment;

        iconImage.sprite = attachment.attachmentIcon;
        nameText.text = attachment.attachmentName;
        costText.text = $"{attachment.cost} Coins";

        // 스탯 변화 표시
        string recoilChange = $"Recoil: {Mathf.RoundToInt((1 - attachment.recoilMultiplier) * 100)}% ↓";
        string spreadChange = $"Spread: {Mathf.RoundToInt((1 - attachment.spreadMultiplier) * 100)}% ↓";
        statsText.text = $"{recoilChange}\n{spreadChange}";

        // 돈이 부족하면 버튼 비활성화 및 색상 변경
        purchaseButton.interactable = canAfford;
        costText.color = canAfford ? Color.white : Color.red;

        // 버튼 클릭 이벤트 연결
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnCardClicked);

    }

    private void OnCardClicked()
    {
        // gunsmithmanager에 구매 요청 전달
        GunSmithManager.Instance.PurchaseAttachment(currentAttachment);
    }
}
