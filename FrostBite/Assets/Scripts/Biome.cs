using UnityEngine;

// Region nommee. En y entrant, son nom s'affiche a l'ecran.
// Quand plusieurs se chevauchent, la plus prioritaire l'emporte.
[DisallowMultipleComponent]
public class Biome : MonoBehaviour
{
    [SerializeField] private string title = "Region";
    [SerializeField] private string subtitle;
    [Tooltip("La plus haute l'emporte quand deux regions se chevauchent.")]
    [SerializeField] private int priority;
    [SerializeField] private string playerTag = "Player";

    private int overlaps;

    public string Title => title;
    public string Subtitle => subtitle;
    public int Priority => priority;

    // Un trigger ne se declenche qu'au franchissement. Si le joueur demarre deja
    // dedans, la region ne serait jamais enregistree : en sortant d'une zone de
    // chasse, plus rien ne serait affiche.
    private void Start()
    {
        var stat = PlayerStat.Instance;
        var col = GetComponent<Collider>();
        if (stat == null || col == null) return;

        Vector3 p = stat.transform.position;
        if ((col.ClosestPoint(p) - p).sqrMagnitude > 0.01f) return;

        overlaps = 1;
        if (BiomeBanner.Instance != null) BiomeBanner.Instance.Enter(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        overlaps++;
        if (overlaps > 1) return;

        if (BiomeBanner.Instance != null) BiomeBanner.Instance.Enter(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        overlaps = Mathf.Max(0, overlaps - 1);
        if (overlaps > 0) return;

        if (BiomeBanner.Instance != null) BiomeBanner.Instance.Exit(this);
    }

    private void OnDisable()
    {
        overlaps = 0;
        if (BiomeBanner.Instance != null) BiomeBanner.Instance.Exit(this);
    }
}
