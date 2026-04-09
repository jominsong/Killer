using UnityEngine;

[CreateAssetMenu(fileName = "MapDefinition", menuName = "FPS/Map Definition")]
public class MapDefinition : ScriptableObject
{
    [Tooltip("맵 프리팹 (MapSpawnPointContainer 반드시 포함)")]
    public GameObject mapPrefab;

    [Tooltip("플레이어 시작 위치 이름 맵 프리팹 안에 있는 빈 오브젝트 이름")]
    public string playerSpawnPointName = "PlayerSpawnPoint";

    [Tooltip("Inspector에서 보여줄 맵 이름")]
    public string mapDisplayName = "New Map";

    [Tooltip("맵의 라이팅 설정")]
    public MapLightingProfile lightingProfile;
}
