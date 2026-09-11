using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CampfireSpot : MonoBehaviour
{
    private static readonly List<CampfireSpot> spots = new List<CampfireSpot>();

    [SerializeField] private float radius = 18f;
    [SerializeField] private GameObject smoke;
    [Tooltip("Le foyer allume, desactive au depart. C'est lui qu'on rallume au lieu d'en creer un nouveau.")]
    [SerializeField] private Campfire fire;
    [Header("Fumee")]
    [SerializeField] private ParticleSystem smokeSystem;
    [Tooltip("Materiau brouillarde : panache local des clairieres lointaines.")]
    [SerializeField] private Material smokeNear;
    [Tooltip("Materiau non brouillarde : seule la clairiere la plus proche l'utilise, pour servir de cap.")]
    [SerializeField] private Material smokeFar;
    [SerializeField] private Vector2 lifetimeNear = new Vector2(5f, 8f);
    [SerializeField] private Vector2 lifetimeFar = new Vector2(14f, 18f);

    private static CampfireSpot beacon;
    private static int beaconFrame = -1;

    private bool everLit;
    private bool spent;
    private bool beaconOn;

    public float Radius => radius;
    public bool Lit => fire != null && fire.Lit;
    public bool Spent => spent;

    private void OnEnable()
    {
        if (!spots.Contains(this)) spots.Add(this);
    }

    private void OnDisable()
    {
        spots.Remove(this);
    }

    public static bool Contains(Vector3 point)
    {
        for (int i = 0; i < spots.Count; i++)
        {
            CampfireSpot s = spots[i];
            if (s == null) continue;

            Vector3 d = point - s.transform.position;
            d.y = 0f;

            if (d.sqrMagnitude <= s.radius * s.radius) return true;
        }
        return false;
    }

    public static CampfireSpot Nearest(Vector3 point)
    {
        CampfireSpot best = null;
        float bd = float.MaxValue;

        for (int i = 0; i < spots.Count; i++)
        {
            CampfireSpot s = spots[i];
            if (s == null) continue;

            Vector3 d = point - s.transform.position;
            d.y = 0f;

            if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = s; }
        }
        return best;
    }

    public void SetSmokeActive(bool active)
    {
        if (smoke != null && smoke.activeSelf != active) smoke.SetActive(active);
    }

    public void SetBeacon(bool active)
    {
        if (smokeSystem == null) return;

        var r = smokeSystem.GetComponent<ParticleSystemRenderer>();
        Material m = active ? smokeFar : smokeNear;
        if (r != null && m != null) r.sharedMaterial = m;

        Vector2 life = active ? lifetimeFar : lifetimeNear;
        var main = smokeSystem.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
    }

    public static IReadOnlyList<CampfireSpot> All => spots;

    public void Light()
    {
        if (fire == null) return;

        if (!fire.gameObject.activeSelf) fire.gameObject.SetActive(true);
        fire.Relight();
        everLit = true;
        spent = false;
        SetSmokeActive(false);
    }

    private static void PickBeacon()
    {
        if (beaconFrame == Time.frameCount) return;

        beaconFrame = Time.frameCount;
        beacon = null;

        var stat = PlayerStat.Instance;
        if (stat == null) return;

        float best = float.MaxValue;

        for (int i = 0; i < spots.Count; i++)
        {
            CampfireSpot s = spots[i];

            if (s == null || s.Lit || s.spent) continue;

            float d = (s.transform.position - stat.transform.position).sqrMagnitude;
            if (d < best) { best = d; beacon = s; }
        }
    }

    private void Update()
    {
        if (fire == null) return;

        if (fire.Lit) { SetSmokeActive(false); Beacon(false); return; }

        if (everLit) { spent = true; SetSmokeActive(false); Beacon(false); return; }

        SetSmokeActive(true);

        PickBeacon();
        Beacon(this == beacon);
    }

    private void Beacon(bool active)
    {
        if (active == beaconOn) return;

        beaconOn = active;
        SetBeacon(active);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.9f);
        for (int i = 0; i < 48; i++)
        {
            float a0 = i / 48f * Mathf.PI * 2f, a1 = (i + 1) / 48f * Mathf.PI * 2f;
            Gizmos.DrawLine(
                transform.position + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius,
                transform.position + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius);
        }
    }
}
