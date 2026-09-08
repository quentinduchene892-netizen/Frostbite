using UnityEngine;

[DisallowMultipleComponent]
public class WindStorm : MonoBehaviour
{
    static int fogFrame = -1;
    static float fogBest;

    [SerializeField] Collider area;
    [SerializeField] ParticleSystem snow;
    [SerializeField] ParticleSystem gust;
    [SerializeField] AudioSource stormAudio;
    [SerializeField] bool on = true;
    [SerializeField] bool random;
    [SerializeField] float calmMin = 20f;
    [SerializeField] float calmMax = 45f;
    [SerializeField] float blowMin = 12f;
    [SerializeField] float blowMax = 25f;
    [SerializeField] float coldRate = 2.5f;
    [SerializeField] float stressRate = 2.5f;
    [SerializeField] float rise = 0.12f;
    [SerializeField] float fall = 0.35f;
    [SerializeField] float snowRate = 900f;
    [SerializeField] float gustRate = 16f;
    [SerializeField] float snowHeight = 11f;
    [SerializeField] float gustHeight = 2f;
    [SerializeField] bool driveFog = true;
    [SerializeField] float fogNear = 0f;
    [SerializeField] float fogFar = 5f;
    [SerializeField] Color fogTint = new Color(0.90f, 0.92f, 0.95f, 1f);

    PlayerStat stat;
    Camera sky;
    Color baseSky;
    ParticleSystem.EmissionModule snowEmission;
    ParticleSystem.EmissionModule gustEmission;
    Vector3 spot;
    float goal;
    Color baseTint;
    float baseNear;
    float baseFar;
    float power;
    float turnLeft;
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

        if (random)
        {
            on = false;
            turnLeft = Random.Range(calmMin, calmMax);
        }

        sky = Camera.main;

        if (sky != null) baseSky = sky.backgroundColor;

        baseNear = RenderSettings.fogStartDistance;
        baseFar = RenderSettings.fogEndDistance;
        baseTint = RenderSettings.fogColor;

        if (stormAudio != null)
        {
            stormAudio.loop = true;
            stormAudio.volume = 0f;
            stormAudio.Play();
        }
    }

    void Update()
    {
        Turn();

        stat = PlayerStat.Instance;
        inside = stat != null && Covers(stat.transform.position);
        goal = on && inside ? 1f : 0f;
        power = Mathf.MoveTowards(power, goal, (goal > power ? rise : fall) * Time.deltaTime);

        Blow();
        Fog();
        Sound();

        if (power <= 0f || stat == null) return;

        stat.AddCold(coldRate * power * Time.deltaTime);
        stat.AddStress(stressRate * power * Time.deltaTime);
    }

    void Turn()
    {
        if (!random) return;

        turnLeft -= Time.deltaTime;

        if (turnLeft > 0f) return;

        on = !on;
        turnLeft = on ? Random.Range(blowMin, blowMax) : Random.Range(calmMin, calmMax);
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

    void Sound()
    {
        if (stormAudio != null) stormAudio.volume = power;
    }

    void Fog()
    {
        if (!driveFog) return;

        if (fogFrame != Time.frameCount)
        {
            fogFrame = Time.frameCount;
            fogBest = 0f;
        }

        if (power <= fogBest && fogBest > 0f) return;

        fogBest = power;

        RenderSettings.fogStartDistance = Mathf.Lerp(baseNear, fogNear, power);
        RenderSettings.fogEndDistance = Mathf.Lerp(baseFar, fogFar, power);
        RenderSettings.fogColor = Color.Lerp(baseTint, fogTint, power);

        if (sky != null) sky.backgroundColor = Color.Lerp(baseSky, fogTint, power);
    }

    void OnDisable()
    {
        if (!driveFog) return;

        RenderSettings.fogStartDistance = baseNear;
        RenderSettings.fogEndDistance = baseFar;
        RenderSettings.fogColor = baseTint;

        if (sky != null) sky.backgroundColor = baseSky;
    }

    bool Covers(Vector3 point)
    {
        if (area == null) return true;

        spot = area.ClosestPoint(point);

        return (spot - point).sqrMagnitude < 0.0001f;
    }
}
