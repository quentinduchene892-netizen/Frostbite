using UnityEngine;

[DisallowMultipleComponent]
public class WoodPickup : MonoBehaviour
{
    [SerializeField] int amount = 10;
    [SerializeField] string playerTag = "Player";

    public int Amount => amount;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (WoodGather.Instance == null) return;

        WoodGather.Instance.Enter(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (WoodGather.Instance == null) return;

        WoodGather.Instance.Leave(this);
    }
}
