using UnityEngine;

[DisallowMultipleComponent]
public class WindStorm : MonoBehaviour
{
    [SerializeField] Collider area;
    [SerializeField] ParticleSystem snow;
    [SerializeField] ParticleSystem gust;
    [SerializeField] bool on = true;
    [SerializeField] float coldRate = 2.5f;
    [SerializeField] float stressRate = 2.5f;
    [SerializeField] float fade = 1.5f;
    [SerializeField] float snowRate = 900f;
    [SerializeField] float gustRate = 16f;
    [SerializeField] float snowHeight = 11f;
    [SerializeField] float gustHeight = 2f;

    PlayerStat stat;
    ParticleSystem.EmissionModule snowEmission;
    ParticleSystem.EmissionModule gustEmission;
    Vector3 spot;
    float power;
    bool inside;
    bool hasSnow;
    bool hasGust;

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

        hasSnow = snow != null;
        hasGust = gust != null;

        if (hasSnow) snowEmission = snow.emission;
        if (hasGust) gustEmission = gust.emission;
    }

    void Update()
    {
        stat = PlayerStat.Instance;
        inside = stat != null && Covers(stat.transform.position);
        power = Mathf.MoveTowards(power, on && inside ? 1f : 0f, fade * Time.deltaTime);

        Blow();

        if (power <= 0f || stat == null) return;

        stat.AddCold(coldRate * power * Time.deltaTime);
        stat.AddStress(stressRate * power * Time.deltaTime);
    }

    void Blow()
    {
        if (stat == null) return;

        if (hasSnow)
        {
            snow.transform.position = stat.transform.position + Vector3.up * snowHeight;
            snowEmission.rateOverTime = snowRate * power;
        }

        if (hasGust)
        {
            gust.transform.position = stat.transform.position + Vector3.up * gustHeight;
            gustEmission.rateOverTime = gustRate * power;
        }
    }

    bool Covers(Vector3 point)
    {
        if (area == null) return true;

        spot = area.ClosestPoint(point);

        return (spot - point).sqrMagnitude < 0.0001f;
    }
}
