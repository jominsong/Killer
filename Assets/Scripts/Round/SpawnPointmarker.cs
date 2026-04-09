using System.Collections;
using UnityEngine;

/// <summary>
/// 스폰 포인트 예고 마커 프리팹 컴포넌트.
/// 
/// 활성화 → 페이드 애니메이션 → 비활성화 (WaveEnemySpawner가 풀로 회수)
/// MeshRenderer의 Material은 반드시 알파를 지원하는 셰이더여야 함 (URP: Universal/Lit, Alpha mode)
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class SpawnPointMarker : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private Color markerColor = new Color(1f, 0.2f, 0.2f, 1f);

    private MeshRenderer meshRenderer;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnEnable()
    {
        // 매 활성화마다 색상 초기화
        SetAlpha(1f);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(PulseEffect());
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private IEnumerator PulseEffect()
    {
        while (true)
        {
            float alpha = Mathf.Lerp(1f, 0f, Mathf.PingPong(Time.time * fadeSpeed, 1f));
            SetAlpha(alpha);
            yield return null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (meshRenderer == null) return;
        Color c = markerColor;
        c.a = alpha;
        // MaterialPropertyBlock으로 드로우콜 배칭 유지
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);  // URP
        block.SetColor("_Color", c);      // Built-in fallback
        meshRenderer.SetPropertyBlock(block);
    }
}