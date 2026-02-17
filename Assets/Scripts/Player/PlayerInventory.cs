using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public class CoinEvent : UnityEvent<int> { }

public class PlayerInventory : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField]
    private int currentCoins = 0;

    public CoinEvent onCoinChanged = new CoinEvent();
    public int CurrentCoins => currentCoins;

    private void Start()
    {
        onCoinChanged.Invoke(CurrentCoins);
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        onCoinChanged.Invoke(currentCoins);
    }

    public bool ConsumeCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            onCoinChanged.Invoke(currentCoins);
            return true;
        }
        return false;
    }
}
