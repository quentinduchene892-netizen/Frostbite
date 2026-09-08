using UnityEngine;

[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [SerializeField] int wood;

    public int Wood => wood;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Add(int amount)
    {
        wood = Mathf.Max(0, wood + amount);
    }

    public bool Has(int amount)
    {
        return wood >= amount;
    }

    public bool Take(int amount)
    {
        if (wood < amount) return false;

        wood -= amount;

        return true;
    }
}
