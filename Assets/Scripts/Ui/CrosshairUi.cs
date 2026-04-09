using UnityEngine;
using UnityEngine.UI;

public class CrosshairUi : MonoBehaviour
{
    [Header("Crosshair Parts")]
    public RectTransform up;
    public RectTransform down;
    public RectTransform left;
    public RectTransform right;

    [Header("Spread Settings")]
    public float baseGap = 10f;  // Ui 최소 간격
    public float spreadMultiplier = 1000f;  // 퍼짐 -> Ui 변환 비율
    public float smoothSpeed = 15f;

    private float currentOffset;
    private float targetOffset;

    public void SetSpread(float spread)
    {
        targetOffset = baseGap + spread * spreadMultiplier;
    }

    private void Update()
    {

        currentOffset = Mathf.Lerp(currentOffset, targetOffset, Time.deltaTime * smoothSpeed);

        up.anchoredPosition = new Vector2(0,currentOffset);
        down.anchoredPosition = new Vector2(0 ,-currentOffset);
        left.anchoredPosition = new Vector2(-currentOffset, 0);
        right.anchoredPosition = new Vector2(currentOffset,0);
    }

    public void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }

    public void SetWeaponMode(bool hasWeapon)
    {
        up.gameObject.SetActive(hasWeapon);
        down.gameObject.SetActive(hasWeapon);
        left.gameObject.SetActive(hasWeapon);
        right.gameObject.SetActive(hasWeapon);
    }
}
