using UnityEngine;

[DisallowMultipleComponent]
public class Campfire : MonoBehaviour
{
    [SerializeField] Light glow;
    [SerializeField] AudioSource fireAudio;
    [SerializeField] float radius = 5f;
    [SerializeField] float burnTime = 20f;
    [SerializeField] float warmRate = 3f;
    [SerializeField] float calmRate = 3.5f;
    [Tooltip("Vie rendue par seconde. Sans ca, les blessures par balle s'accumulent sans aucun recours.")]
    [SerializeField] float healRate = 4f;
    [SerializeField] float coldFloor = 0f;
    [SerializeField] float stressFloor = 0f;

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
        if (fireAudio == null) fireAudio = GetComponent<AudioSource>();

        if (fireAudio != null)
        {
            fireAudio.loop = true;
            fireAudio.Play();
        }
    }

    void Update()
    {
        if (!lit) return;

        left -= Time.deltaTime;

        stat = PlayerStat.Instance;
        gap = stat != null ? Vector3.Distance(stat.transform.position, transform.position) : Mathf.Infinity;
        near = gap <= radius;

        if (near && !stat.Dead) Heat();

        if (left <= 0f) Out();
    }

    void Heat()
    {
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

        if (healRate > 0f && stat.Life < 100f)
            stat.Heal(healRate * Time.deltaTime);

        stat.Dry(Time.deltaTime);
    }

    public void Relight()
    {
        left = burnTime;
        lit = true;

        if (glow != null) glow.enabled = true;
        if (fireAudio != null && !fireAudio.isPlaying) fireAudio.Play();
    }

    void Out()
    {
        left = 0f;
        lit = false;

        if (glow != null) glow.enabled = false;
        if (fireAudio != null) fireAudio.Stop();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
