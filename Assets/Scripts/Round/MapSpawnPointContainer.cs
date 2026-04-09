using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// 맵 프리팹 루트에 붙이는 컴포넌트.
/// 자식 오브젝트 중 "SpawnPoint" 태그 또는 SpawnPointTag 이름을 가진 Transform을 자동 수집.
/// 새 맵 추가 시 이 컴포넌트만 붙이면 자동 감지됨.
/// </summary>
public class MapSpawnPointContainer : MonoBehaviour
{
    [Tooltip("스폰 포인트로 인식할 태그 (없으면 Tag 무시하고 수동 리스트 사용)")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    [Tooltip("자동 감지 대신 수동으로 지정할 때 사용")]
    [SerializeField] private List<Transform> manualSpawnPoints = new List<Transform>();

    public Transform PlayerSpawnPoint { get; private set; }
    private NavMeshSurface navMeshSurface;

    private List<Transform> spawnPoints = new List<Transform>();

    public void Initialize(string playerSpawnPointName)
    {
        spawnPoints.Clear();

        // 수동 리스트 우선
        if (manualSpawnPoints.Count > 0)
        {
            spawnPoints.AddRange(manualSpawnPoints);
        }
        else
        {
            // 태그 기반 자동 수집
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.CompareTag(spawnPointTag))
                    spawnPoints.Add(child);
            }
        }

        // 플레이어 스폰 위치 탐색
        Transform found = transform.Find(playerSpawnPointName);
        PlayerSpawnPoint = found != null ? found : transform;

        // NavMesh 런타임 베이크
        navMeshSurface = GetComponent<NavMeshSurface>();
        if (navMeshSurface == null)
            navMeshSurface = gameObject.AddComponent<NavMeshSurface>();

        navMeshSurface.BuildNavMesh();
        Debug.Log($"[MapSpawnPointContainer] NavMesh 베이크 완료");

        Debug.Log($"[MapSpawnPointContainer] '{gameObject.name}' 스폰포인트 {spawnPoints.Count}개 감지");
    }

    public List<Transform> GetSpawnPoints() => spawnPoints;
}