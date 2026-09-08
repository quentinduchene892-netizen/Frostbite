using System.Collections.Generic;
using UnityEngine;

// Trace d'une riviere : la liste des points cliques et ses reglages.
// Sert de memoire au River Painter pour pouvoir revenir editer la riviere plus tard.
[DisallowMultipleComponent]
public class RiverPath : MonoBehaviour
{
    public List<Vector3> points = new List<Vector3>();

    [Header("Forme")]
    [Range(3f, 60f)] public float width = 14f;
    [Range(0.5f, 12f)] public float depth = 3f;
    [Range(2f, 60f)] public float bankWidth = 12f;
    [Tooltip("De combien la surface de l'eau se trouve sous le niveau du terrain.")]
    [Range(0f, 6f)] public float waterDrop = 1f;

    [Header("Profil en long")]
    [Tooltip("Longueur sur laquelle le niveau de l'eau est lisse. Plus c'est grand, plus la riviere ignore les bosses du terrain.")]
    [Range(10f, 200f)] public float levelSmooth = 60f;
    [Tooltip("Interdit a l'eau de remonter. A n'activer que si ton trace descend vraiment, sinon il creuse un canal.")]
    public bool forceDownhill;

    [Tooltip("Longueur sur laquelle la riviere s'efface a ses deux bouts, pour ne pas finir en cratere.")]
    [Range(0f, 80f)] public float endTaper = 20f;

    [Header("Courant")]
    [Tooltip("Vitesse apparente du courant, en metres par seconde. Le joueur marche a 3,2 m/s.")]
    [Range(0.1f, 6f)] public float flowSpeed = 1.5f;

    [Header("Maillage")]
    [Range(1f, 12f)] public float stepLength = 3f;

    [Header("Effets")]
    public bool spray = true;
    [Range(15f, 200f)] public float sprayEvery = 45f;

    // --- sauvegarde du terrain avant creusement, pour que regenerer ne cumule pas ---
    [HideInInspector] public float[] savedHeights;
    [HideInInspector] public int savedX, savedZ, savedW, savedH;

    public bool HasBackup => savedHeights != null && savedW > 0 && savedH > 0
                             && savedHeights.Length == savedW * savedH;

    // Catmull-Rom : passe par tous les points cliques, avec des virages doux.
    public Vector3 Sample(float t)
    {
        int n = points.Count;
        if (n == 0) return transform.position;
        if (n == 1) return points[0];
        if (n == 2) return Vector3.Lerp(points[0], points[1], Mathf.Clamp01(t));

        float u = Mathf.Clamp01(t) * (n - 1);
        int i = Mathf.Min(Mathf.FloorToInt(u), n - 2);
        float f = u - i;

        Vector3 p0 = points[Mathf.Max(i - 1, 0)];
        Vector3 p1 = points[i];
        Vector3 p2 = points[i + 1];
        Vector3 p3 = points[Mathf.Min(i + 2, n - 1)];

        return 0.5f * ((2f * p1) + (-p0 + p2) * f
             + (2f * p0 - 5f * p1 + 4f * p2 - p3) * f * f
             + (-p0 + 3f * p1 - 3f * p2 + p3) * f * f * f);
    }

    public float ApproxLength(int steps = 200)
    {
        if (points.Count < 2) return 0f;

        float len = 0f;
        Vector3 prev = Sample(0f);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 c = Sample(i / (float)steps);
            len += Vector3.Distance(prev, c);
            prev = c;
        }
        return len;
    }

    private void OnDrawGizmosSelected()
    {
        if (points.Count < 2) return;

        Gizmos.color = new Color(0.3f, 0.75f, 1f);
        Vector3 prev = Sample(0f);
        for (int i = 1; i <= 120; i++)
        {
            Vector3 c = Sample(i / 120f);
            Gizmos.DrawLine(prev, c);
            prev = c;
        }

        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        for (int i = 0; i < points.Count; i++)
            Gizmos.DrawWireSphere(points[i], 1.5f);
    }
}
