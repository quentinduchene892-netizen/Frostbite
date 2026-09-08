using UnityEngine;

[DisallowMultipleComponent]
public class WoodGather : MonoBehaviour
{
    public static WoodGather Instance;

    [SerializeField] float gatherTime = 0.9f;
    [SerializeField] float dropSpeed = 2.5f;

    InputManager input;
    PlayerStat stat;
    WoodPickup target;
    float progress;
    bool holding;

    public float Progress => progress;
    public bool Near => target != null;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Enter(WoodPickup pickup)
    {
        target = pickup;
    }

    public void Leave(WoodPickup pickup)
    {
        if (target != pickup) return;

        target = null;
        progress = 0f;
    }

    void Update()
    {
        if (target == null)
        {
            progress = 0f;
            return;
        }

        input = InputManager.Instance;
        stat = PlayerStat.Instance;

        holding = input != null && input.Interact
            && (stat == null || (!stat.Fainted && !stat.Dead));

        if (holding)
            progress += Time.deltaTime / Mathf.Max(gatherTime, 0.01f);
        else
            progress -= dropSpeed * Time.deltaTime;

        progress = Mathf.Clamp01(progress);

        if (progress >= 1f) Take();
    }

    void Take()
    {
        progress = 0f;

        if (Inventory.Instance != null) Inventory.Instance.Add(target.Amount);

        Destroy(target.gameObject);
        target = null;
    }
}
