using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Maps")]
    [Tooltip("추가할 맵 ScriptableObject 목록 맵 추가 시 여기에만 등록하면 됨")]
    [SerializeField] private List<MapDefinition> mapPool = new List<MapDefinition>();

    [Tooltip("같은 맵이 연속으로 나오는 걸 방지")]
    [SerializeField] private bool preventRepeat = true;

    private GameObject currentMapInstance;
    private MapSpawnPointContainer currentContainer;
    private int lastMapIndex = -1;

    public Transform CurrentPlayerSpawnPoint =>
        currentContainer != null ? currentContainer.PlayerSpawnPoint : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 랜덤 맵 로드. 이전 맵 제거  새 맵 인스턴스 스폰포인트 등록.
    /// RoundManager 또는 ShopExitTeleporter에서 호출.
    /// </summary>
    public void LoadRandomMap()
    {
        if (mapPool.Count == 0)
        {
            Debug.LogError("[MapManager] mapPool이 비어있습니다!");
            return;
        }

        // 이전 맵 제거
        if (currentMapInstance != null)
            Destroy(currentMapInstance);

        // 랜덤 선택 (반복 방지)
        int index = PickRandomIndex();
        lastMapIndex = index;
        MapDefinition def = mapPool[index];

        // 인스턴스 생성
        currentMapInstance = Instantiate(def.mapPrefab);
        currentContainer = currentMapInstance.GetComponent<MapSpawnPointContainer>();

        if (currentContainer == null)
        {
            Debug.LogError($"[MapManager] '{def.mapPrefab.name}'에 MapSpawnPointContainer가 없습니다!");
            return;
        }

        if (def.lightingProfile != null)
        {
            // 전역 조명
            RenderSettings.ambientLight = def.lightingProfile.ambientColor;
            RenderSettings.ambientIntensity = def.lightingProfile.ambientIntensity;

            // Directional Light 교체
            Light dirLight = RenderSettings.sun;
            if (dirLight != null)
            {
                dirLight.color = def.lightingProfile.directionalLightColor;
                dirLight.intensity = def.lightingProfile.directionalLightIntensity;
                dirLight.transform.rotation = Quaternion.Euler(def.lightingProfile.directionalLightEuler);
            }

            // 보조 Directional Light
            GameObject subLightObj = GameObject.Find("Sub Light");
            if (subLightObj != null)
            {
                Light subLight = subLightObj.GetComponent<Light>();
                if (subLight != null)
                {
                    subLight.color = def.lightingProfile.fillLightColor;
                    subLight.intensity = def.lightingProfile.fillLightIntensity;
                    subLight.transform.rotation = Quaternion.Euler(def.lightingProfile.fillLightEuler);
                }
            }

            // 안개
            RenderSettings.fog = def.lightingProfile.fogEnabled;
            RenderSettings.fogColor = def.lightingProfile.fogColor;
            RenderSettings.fogDensity = def.lightingProfile.fogDensity;

            // 스카이박스
            if (def.lightingProfile.skybox != null)
                RenderSettings.skybox = def.lightingProfile.skybox;
        }

        currentContainer.Initialize(def.playerSpawnPointName);

        // WaveEnemySpawner에 스폰포인트 전달
        WaveEnemySpawner spawner = FindFirstObjectByType<WaveEnemySpawner>();
        if (spawner != null)
            spawner.ReplaceSpawnPoints(currentContainer.GetSpawnPoints());

        Debug.Log($"[MapManager] 맵 로드: {def.mapDisplayName} (스폰포인트 {currentContainer.GetSpawnPoints().Count}개)");
    }

    private int PickRandomIndex()
    {
        if (mapPool.Count == 1) return 0;

        int index;
        int tries = 0;
        do
        {
            index = Random.Range(0, mapPool.Count);
            tries++;
        }
        while (preventRepeat && index == lastMapIndex && tries < 10);

        return index;
    }
}