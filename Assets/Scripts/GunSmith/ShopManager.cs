using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Spawn")]
    [SerializeField] private Transform shopSpawnPoint;

    public Transform ShopSpawnPoint => shopSpawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}