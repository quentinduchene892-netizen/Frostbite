using UnityEngine;

[DisallowMultipleComponent]
public class WoodPickup : MonoBehaviour
{
    [SerializeField] int amount = 10;

    public int Amount => amount;
}
