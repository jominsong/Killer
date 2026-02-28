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
        statsText.text = "";

        // 수치 변화를 동적으로 생성 (1.0보다 작으면 감소, 크면 증가)
        AddStatText("V.Recoil", attachment.verticalrecoilMultiplier);
        AddStatText("H.Recoil", attachment.horizontalrecoilMultiplier);
        AddStatText("Spread", attachment.maxspreadMultiplier);
        AddStatText("Damage", attachment.damageMultiplier, true); // 반전 (데미지는 큰게 좋음)

        if (attachment.MaxAmmoAdder != 0)
            statsText.text += $"Mag: +{attachment.MaxAmmoAdder}\n";

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

    private void AddStatText(string label, float multiplier, bool inverse = false)
    {
        if (Mathf.Approximately(multiplier, 1f)) return;

        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        string color = (percent > 0) ? (inverse ? "green" : "red") : (inverse ? "red" : "green");
        string sign = (percent > 0) ? "+" : "-";

        statsText.text += $"{label}: <color={color}>{sign}{percent}%</color>\n";
    }
}
