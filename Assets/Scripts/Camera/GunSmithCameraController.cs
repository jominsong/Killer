using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GunSmithCameraController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera gunSmithCamera;
    [SerializeField] private Camera weaponCamera;

    [Header("Fade")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.3f;

    private void Awake()
    {
        gunSmithCamera.cullingMask = 0;
        SetFadeAlpha(0f);
    }

    public void EnterGunSmith(System.Action onComplete = null)
    {
        StartCoroutine(TransitionRoutine(toGunSmith: true, onComplete));
    }

    public void ExitGunSmith(System.Action onComplete = null)
    {
        StartCoroutine(TransitionRoutine(toGunSmith: false, onComplete));
    }

    private IEnumerator TransitionRoutine(bool toGunSmith, System.Action onComplete)
    {
        // 페이드 아웃 (화면 검게)
        if (fadePanel != null)
            yield return StartCoroutine(Fade(0f, 1f));

        if (toGunSmith)
        {
            // 건스미스 카메라 GunSmithDisplay 레이어 활성화
            gunSmithCamera.cullingMask = LayerMask.GetMask("GunSmithDisplay", "Default","Weapon");
            // 메인 카메라 컬링 비활성화
            mainCamera.cullingMask = 0;
            weaponCamera.cullingMask = 0;
        }
        else
        {
            // 건스미스 카메라 → Nothing으로 복귀
            gunSmithCamera.cullingMask = 0;
            mainCamera.cullingMask = -1;
            weaponCamera.cullingMask = -1;
        }

        // 페이드 인 (화면 밝아짐)
        yield return StartCoroutine(Fade(1f, 0f));

        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Time.timeScale 영향 안 받도록
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadePanel == null) return;
        Color c = fadePanel.color;
        c.a = alpha;
        fadePanel.color = c;
    }
}