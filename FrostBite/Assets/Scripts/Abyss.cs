using UnityEngine;

[DisallowMultipleComponent]
public class Abyss : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (PlayerStat.Instance != null) PlayerStat.Instance.Fall();
    }
}
