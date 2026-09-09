using UnityEngine;

// N'allume le signal lumineux que sur la clairiere la plus proche du joueur.
// Sans ca, huit colonnes visibles a travers le brouillard ne donneraient aucun cap.
[DisallowMultipleComponent]
public class CampfireBeacon : MonoBehaviour
{
    [SerializeField] private float refresh = 0.3f;
    [Tooltip("Au-dela de cette distance, plus aucun signal : le joueur doit chercher.")]
    [SerializeField] private float maxDistance = 500f;

    private float left;
    private CampfireSpot current;

    private void Update()
    {
        left -= Time.deltaTime;
        if (left > 0f) return;
        left = refresh;

        PlayerStat stat = PlayerStat.Instance;
        if (stat == null) return;

        CampfireSpot best = null;
        float bd = maxDistance * maxDistance;

        var all = CampfireSpot.All;
        for (int i = 0; i < all.Count; i++)
        {
            CampfireSpot s = all[i];
            if (s == null || s.Spent) continue;

            Vector3 d = s.transform.position - stat.transform.position;
            d.y = 0f;

            if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = s; }
        }

        if (best == current) return;

        if (current != null) current.SetBeacon(false);
        current = best;
        if (current != null) current.SetBeacon(true);
    }
}
