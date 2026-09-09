using UnityEngine;

// Mesure a quel point un point du monde est abrite par la foret.
// On lit les instances d'arbres du Terrain, pas la physique : les colliders
// d'arbres de Terrain ne sont pas fiables pour un raycast.
[DisallowMultipleComponent]
public class ShelterMap : MonoBehaviour
{
    public static ShelterMap Instance { get; private set; }

    [SerializeField] private float cellSize = 8f;
    [Tooltip("Nombre d'arbres autour du joueur pour un abri total.")]
    [SerializeField] private int treesForFullShelter = 22;

    private int[] counts;
    private int nx, nz;
    private Vector3 origin;
    private Vector3 size;
    private bool ready;

    public bool Ready => ready;
    public int CellCount => counts != null ? counts.Length : 0;

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Une seule passe au demarrage : ensuite la lecture est en temps constant.
    public void Build()
    {
        ready = false;

        Terrain ter = Terrain.activeTerrain;
        if (ter == null) { Debug.LogWarning("ShelterMap : aucun Terrain actif.", this); return; }

        TerrainData td = ter.terrainData;
        origin = ter.transform.position;
        size = td.size;

        nx = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
        nz = Mathf.Max(1, Mathf.CeilToInt(size.z / cellSize));
        counts = new int[nx * nz];

        TreeInstance[] trees = td.treeInstances;
        for (int i = 0; i < trees.Length; i++)
        {
            float wx = origin.x + trees[i].position.x * size.x;
            float wz = origin.z + trees[i].position.z * size.z;

            int cx = Mathf.Clamp(Mathf.FloorToInt((wx - origin.x) / cellSize), 0, nx - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt((wz - origin.z) / cellSize), 0, nz - 1);
            counts[cz * nx + cx]++;
        }

        ready = true;
    }

    // 0 = totalement expose, 1 = sous couvert dense.
    public float Shelter(Vector3 world)
    {
        if (!ready) return 0f;

        int cx = Mathf.FloorToInt((world.x - origin.x) / cellSize);
        int cz = Mathf.FloorToInt((world.z - origin.z) / cellSize);

        int sum = 0;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = cx + dx, z = cz + dz;
                if (x < 0 || x >= nx || z < 0 || z >= nz) continue;
                sum += counts[z * nx + x];
            }

        return Mathf.Clamp01(sum / (float)Mathf.Max(1, treesForFullShelter));
    }

    public int TreesAround(Vector3 world)
    {
        if (!ready) return 0;

        int cx = Mathf.FloorToInt((world.x - origin.x) / cellSize);
        int cz = Mathf.FloorToInt((world.z - origin.z) / cellSize);

        int sum = 0;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = cx + dx, z = cz + dz;
                if (x < 0 || x >= nx || z < 0 || z >= nz) continue;
                sum += counts[z * nx + x];
            }
        return sum;
    }
}
