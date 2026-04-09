using System.Collections;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private float respawnTime = 15f;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Spawn Offset")]
    [SerializeField] private float spawnHeightOffset = 0.5f;

    private GameObject spawnedItem;
    private Coroutine respawnCoroutine;

    private void Start()
    {
        if (spawnOnStart)
            StartCoroutine(InitialSpawn());
    }

    private IEnumerator InitialSpawn()
    {
        yield return null; // 한 프레임 대기
        SpawnItem();
    }

    private void Update()
    {
        // 이미 아이템이 스폰했으면 안함
        if (spawnedItem != null) return;

        // spawnedItem이 null이고 리스폰 코루틴도 없을 때만 리스폰 시작
        if (spawnedItem == null && respawnCoroutine == null)
        {
            respawnCoroutine = StartCoroutine(RespawnRoutine());
        }
    }

    private void SpawnItem()
    {
        if (itemPrefabs == null)
        {
            Debug.LogError($"[ItemSpawner] '{gameObject.name}' : itemPrefab이 비어있습니다!");
            return;
        }
        if (spawnedItem != null) return;

        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        if (prefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        spawnedItem = Instantiate(prefab, spawnPos, transform.rotation);

        ItemWeapon weaponItem = spawnedItem.GetComponent<ItemWeapon>();
        if (weaponItem != null && GunSmithManager.Instance != null)
        {
            weaponItem.SetInGameAttachments(
                GunSmithManager.Instance.GetCurrentAttachments(weaponItem.weaponType));
        }

        StartCoroutine(VerifySpawn(spawnedItem, spawnPos));
    }

    private IEnumerator VerifySpawn(GameObject item, Vector3 spawnPos)
    {
        yield return null;

        if (item == null)
        {
            spawnedItem = null;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        Debug.Log($"[ItemSpawner] '{gameObject.name}' : 리스폰 대기 ({respawnTime}초)");

        yield return new WaitForSeconds(respawnTime);

        respawnCoroutine = null;
        SpawnItem();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        Gizmos.DrawWireSphere(spawnPos, 0.4f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * spawnHeightOffset, 0.4f);
    }
}