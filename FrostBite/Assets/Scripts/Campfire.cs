using UnityEngine;

[DisallowMultipleComponent]
public class Campfire : MonoBehaviour
{
    [SerializeField] Light glow;
    [SerializeField] float radius = 5f;
    [SerializeField] float burnTime = 20f;
    [SerializeField] float warmRate = 3f;
    [SerializeField] float calmRate = 1.5f;
    [SerializeField] float coldFloor = 20f;
    [SerializeField] float stressFloor = 15f;

    PlayerStat stat;
    float left;
    float gap;
    float drop;
    bool near;
    bool lit;

    public float Radius => radius;
    public float Left => left;
    public float LeftPart => left / burnTime;
    public bool Near => near;
    public bool Lit => lit;

    void Awake()
    {
        left = burnTime;
        lit = true;

        if (glow == null) glow = GetComponentInChildren<Light>();
    }

    void Update()
    {
        stat = PlayerStat.Instance;

        if (stat == null) return;

        gap = Vector3.Distance(stat.transform.position, transform.position);
        near = gap <= radius;

        if (!lit || !near || stat.Dead) return;

        left -= Time.deltaTime;

        if (stat.Cold > coldFloor)
        {
            drop = Mathf.Min(warmRate * Time.deltaTime, stat.Cold - coldFloor);
            stat.Warm(drop);
        }

        if (stat.Stress > stressFloor)
        {
            drop = Mathf.Min(calmRate * Time.deltaTime, stat.Stress - stressFloor);
            stat.Calm(drop);
        }

        if (left <= 0f) Out();
    }

    void Out()
    {
        left = 0f;
        lit = false;

        if (glow != null) glow.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
