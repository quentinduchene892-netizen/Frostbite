using UnityEngine;

[DisallowMultipleComponent]
public class WoodGather : MonoBehaviour
{
    public static WoodGather Instance;

    [SerializeField] Camera view;
    [Tooltip("Portee du regard. Au-dela, la buche n'est plus ramassable meme si on la voit.")]
    [SerializeField] float range = 3f;
    [Tooltip("Epaisseur du rayon. Un rayon fin obligerait a centrer le viseur au pixel pres sur une buche minuscule.")]
    [SerializeField] float aimRadius = 0.35f;
    [SerializeField] float nose = 0.35f;
    [SerializeField] LayerMask mask = ~0;
    [SerializeField] float gatherTime = 0.9f;
    [SerializeField] float dropSpeed = 2.5f;

    InputManager input;
    PlayerStat stat;
    WoodPickup target;
    RaycastHit[] buffer = new RaycastHit[16];
    Vector3 origin;
    float progress;
    bool holding;

    public float Progress => progress;
    public bool Aimed => target != null;
    public int Gain => target != null ? target.Amount : 0;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        Aim();

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

    void Aim()
    {
        if (view == null) view = Camera.main;

        WoodPickup found = null;

        if (view != null)
        {
            origin = view.transform.position + view.transform.forward * nose;

            int count = Physics.SphereCastNonAlloc(origin, aimRadius, view.transform.forward, buffer, range, mask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                WoodPickup pickup = buffer[i].collider.GetComponentInParent<WoodPickup>();

                if (pickup == null || buffer[i].distance >= best) continue;

                best = buffer[i].distance;
                found = pickup;
            }
        }

        if (found == target) return;

        target = found;
        progress = 0f;
    }

    void Take()
    {
        progress = 0f;

        if (Inventory.Instance != null) Inventory.Instance.Add(target.Amount);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayPickup(target.transform.position);

        Destroy(target.gameObject);
        target = null;
    }
}
