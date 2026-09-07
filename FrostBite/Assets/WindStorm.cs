using UnityEngine;

[DisallowMultipleComponent]
public class WindStorm : MonoBehaviour
{
    [SerializeField] Collider area;
    [SerializeField] bool on = true;
    [SerializeField] float coldRate = 2.5f;
    [SerializeField] float stressRate = 2.5f;
    [SerializeField] float fade = 1.5f;

    PlayerStat stat;
    Vector3 spot;
    float power;
    bool inside;

    public float Power => power;
    public bool Inside => inside;

    public bool On
    {
        get => on;
        set => on = value;
    }

    void Awake()
    {
        if (area == null) area = GetComponent<Collider>();
    }

    void Update()
    {
        stat = PlayerStat.Instance;
        inside = stat != null && Covers(stat.transform.position);
        power = Mathf.MoveTowards(power, on && inside ? 1f : 0f, fade * Time.deltaTime);

        if (power <= 0f || stat == null) return;

        stat.AddCold(coldRate * power * Time.deltaTime);
        stat.AddStress(stressRate * power * Time.deltaTime);
    }

    bool Covers(Vector3 point)
    {
        if (area == null) return true;

        spot = area.ClosestPoint(point);

        return (spot - point).sqrMagnitude < 0.0001f;
    }
}
