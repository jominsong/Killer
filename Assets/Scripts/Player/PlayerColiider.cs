using UnityEngine;

public class PlayerColiider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if ( other.CompareTag("Coin"))
        {
            other.GetComponent<ItemBase>().Use(transform.parent.gameObject);
        }
    }
}
