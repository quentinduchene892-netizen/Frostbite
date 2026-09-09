using UnityEngine;

[DisallowMultipleComponent]
public class CampfireCraft : MonoBehaviour
{
    [SerializeField] GameObject firePrefab;
    [SerializeField] Transform ghost;
    [SerializeField] GameObject ghostPrefab;
    [SerializeField] Camera view;
    [SerializeField] int woodCost = 2;
    [SerializeField] float buildTime = 2.5f;
    [SerializeField] float dropSpeed = 1.5f;
    [SerializeField] float range = 3.5f;
    [SerializeField] float dropHeight = 3f;
    [SerializeField] float flatEnough = 0.7f;
    [SerializeField] float lift = 0.02f;
    [SerializeField] float nose = 0.35f;
    [SerializeField] LayerMask ground = ~0;

    InputManager input;
    PlayerStat stat;
    RaycastHit[] buffer = new RaycastHit[12];
    RaycastHit hit;
    Vector3 origin;
    Vector3 flat;
    Campfire model;
    float best;
    int count;
    int i;
    Vector3 aim;
    float progress;
    bool holding;
    bool ok;
    bool hasWood;
    bool wants;
    bool visible;

    public float Progress => progress;
    public bool Building => holding;
    public bool Valid => ok;
    public bool Ready => ok && hasWood;

    void Awake()
    {
        if (ghost == null && ghostPrefab != null)
            ghost = Instantiate(ghostPrefab).transform;

        if (ghost == null || firePrefab == null) return;

        model = firePrefab.GetComponent<Campfire>();

        if (model == null) return;

        ghost.localScale = new Vector3(model.Radius * 2f, model.Radius * 2f, 1f);
    }

    void Update()
    {
        input = InputManager.Instance;
        stat = PlayerStat.Instance;

        Aim();

        hasWood = Inventory.Instance != null && Inventory.Instance.Has(woodCost);
        wants = input != null && input.Craft;

        holding = wants && hasWood && ok
            && (stat == null || (!stat.Fainted && !stat.Dead));

        if (holding)
            progress += Time.deltaTime / Mathf.Max(buildTime, 0.01f);
        else
            progress -= dropSpeed * Time.deltaTime;

        progress = Mathf.Clamp01(progress);

        Show();

        if (progress >= 1f) Place();
    }

    void Aim()
    {
        if (view == null) view = Camera.main;

        ok = false;

        if (view == null) return;

        origin = view.transform.position + view.transform.forward * nose;

        if (Cast(origin, view.transform.forward, range))
        {
            aim = hit.point;
            ok = Allowed(hit.normal);
            return;
        }

        aim = origin + view.transform.forward * range;

        if (Cast(aim + Vector3.up * dropHeight, Vector3.down, dropHeight * 2f))
        {
            aim = hit.point;
            ok = Allowed(hit.normal);
        }
    }

    bool Allowed(Vector3 normal)
    {
        if (normal.y < flatEnough || Reach(aim) > range) return false;

        CampfireSpot spot = CampfireSpot.Nearest(aim);
        if (spot == null || spot.Lit) return false;

        Vector3 d = aim - spot.transform.position;
        d.y = 0f;
        if (d.magnitude > spot.Radius) return false;

        aim = spot.transform.position;
        return true;
    }

    float Reach(Vector3 point)
    {
        flat = point - transform.position;
        flat.y = 0f;

        return flat.magnitude;
    }

    bool Cast(Vector3 from, Vector3 way, float far)
    {
        count = Physics.RaycastNonAlloc(from, way, buffer, far, ground, QueryTriggerInteraction.Ignore);
        best = -1f;

        for (i = 0; i < count; i++)
        {
            if (buffer[i].collider.transform.root == transform.root) continue;
            if (best >= 0f && buffer[i].distance >= best) continue;

            best = buffer[i].distance;
            hit = buffer[i];
        }

        return best >= 0f;
    }

    void Show()
    {
        if (ghost == null) return;

        visible = ok && hasWood && (wants || progress > 0.001f);

        if (ghost.gameObject.activeSelf != visible) ghost.gameObject.SetActive(visible);

        if (visible) ghost.position = aim + Vector3.up * lift;
    }

    void Place()
    {
        progress = 0f;

        CampfireSpot spot = CampfireSpot.Nearest(aim);
        if (spot == null || spot.Lit || Inventory.Instance == null) return;
        if (!Inventory.Instance.Take(woodCost)) return;

        spot.Light();

        if (SoundManager.Instance != null) SoundManager.Instance.PlayCraft(spot.transform.position);
    }
}
