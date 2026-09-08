using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Pinceau de placement d'arbres.
//   Tools > FrostBite > Tree Painter
//   Clic gauche glisse = peindre, Maj + clic gauche = effacer, Alt = camera libre.
public class TreePainter : EditorWindow
{
    private enum Mode { GameObjects, ArbresDeTerrain }

    private Mode mode = Mode.ArbresDeTerrain;
    private bool painting;

    private GameObject prefab;
    private string parentName = "Trees";
    private int prototypeIndex;

    private float radius = 30f;
    private float spacing = 3.2f;
    private int perStroke = 12;
    private float strokeStep = 4f;
    private bool fullFill;

    private float scaleMin = 0.75f;
    private float scaleMax = 1.4f;
    private float stretchMin = 0.85f;
    private float stretchMax = 1.25f;
    private float tilt = 4f;
    private bool alignToNormal;
    private float slopeMax = 40f;

    private LayerMask groundMask = ~0;
    private float yOffset;

    private bool registerInTreeWind = true;

    private Vector3 lastPaintPos = Vector3.positiveInfinity;
    private Vector2 scroll;

    // --- grille de hachage spatial : test d'ecart en O(1) au lieu de O(n) ---
    private readonly Dictionary<long, List<Vector2>> hash = new Dictionary<long, List<Vector2>>();
    private float hashCell = 1f;

    [MenuItem("Tools/FrostBite/Tree Painter")]
    private static void Open()
    {
        var w = GetWindow<TreePainter>("Tree Painter");
        w.minSize = new Vector2(300f, 520f);
    }

    private void OnEnable()
    {
        if (prefab == null)
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TreeTerrain.prefab")
                  ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tree.prefab");

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        var col = GUI.backgroundColor;
        GUI.backgroundColor = painting ? new Color(0.5f, 1f, 0.5f) : col;
        if (GUILayout.Button(painting ? "PINCEAU ACTIF  (clic pour desactiver)" : "Activer le pinceau", GUILayout.Height(32f)))
        {
            painting = !painting;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = col;

        EditorGUILayout.HelpBox("Clic gauche glisse : peindre\nMaj + clic gauche : effacer\nAlt : camera libre", MessageType.None);

        EditorGUILayout.Space();
        mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);

        if (mode == Mode.GameObjects)
        {
            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
            parentName = EditorGUILayout.TextField("Parent", parentName);
            registerInTreeWind = EditorGUILayout.Toggle("Inscrire dans TreeWind", registerInTreeWind);
            EditorGUILayout.HelpBox("Vrais GameObjects : animes par TreeWind, mais un draw call chacun. A reserver aux abords des chemins, quelques centaines au maximum.", MessageType.Warning);
        }
        else
        {
            prototypeIndex = EditorGUILayout.IntField("Index du prototype", prototypeIndex);
            EditorGUILayout.HelpBox("Instances de Terrain : instancing GPU et culling par distance. C'est le mode a utiliser pour la masse de la foret.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pinceau", EditorStyles.boldLabel);
        radius = EditorGUILayout.Slider("Rayon", radius, 1f, 800f);
        spacing = EditorGUILayout.Slider("Ecart minimum", spacing, 0.5f, 20f);

        fullFill = EditorGUILayout.Toggle("Remplissage complet", fullFill);
        if (fullFill)
            EditorGUILayout.HelpBox("Chaque clic remplit tout le disque du pinceau a la densite voulue. Avec un rayon de 400, un seul clic couvre toute la carte.", MessageType.Info);
        else
        {
            perStroke = EditorGUILayout.IntSlider("Essais par pas", perStroke, 1, 60);
            strokeStep = EditorGUILayout.Slider("Pas du trace", strokeStep, 0.2f, 40f);
        }

        // estimation de ce que va poser un clic
        float area = Mathf.PI * radius * radius;
        float perTree = spacing * spacing / (fullFill ? 0.82f : 0.7f);
        EditorGUILayout.LabelField(fullFill ? "Un clic posera" : "Densite visee",
            "~" + Mathf.RoundToInt(area / perTree).ToString("N0") + " arbres   (1 / " + perTree.ToString("F0") + " m2)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Variation", EditorStyles.boldLabel);
        EditorGUILayout.MinMaxSlider(new GUIContent("Echelle"), ref scaleMin, ref scaleMax, 0.2f, 3f);
        EditorGUILayout.LabelField(" ", scaleMin.ToString("F2") + "  ->  " + scaleMax.ToString("F2"));
        EditorGUILayout.MinMaxSlider(new GUIContent("Elancement"), ref stretchMin, ref stretchMax, 0.5f, 2f);
        EditorGUILayout.LabelField(" ", stretchMin.ToString("F2") + "  ->  " + stretchMax.ToString("F2"));
        tilt = EditorGUILayout.Slider("Inclinaison aleatoire", tilt, 0f, 20f);
        alignToNormal = EditorGUILayout.Toggle("Aligner sur la pente", alignToNormal);
        slopeMax = EditorGUILayout.Slider("Pente maxi", slopeMax, 5f, 90f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pose au sol", EditorStyles.boldLabel);
        groundMask = EditorGUILayout.MaskField("Calques du sol",
            UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(groundMask),
            UnityEditorInternal.InternalEditorUtility.layers);
        groundMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(groundMask);
        yOffset = EditorGUILayout.Slider("Decalage vertical", yOffset, -3f, 3f);
        EditorGUILayout.HelpBox("Les triggers et les arbres deja poses sont toujours ignores par le raycast de pose.", MessageType.None);

        EditorGUILayout.Space();
        if (GUILayout.Button("Tout effacer sous le parent"))
            ClearAll();

        if (GUILayout.Button("Nettoyer la liste TreeWind"))
            Debug.Log("Tree Painter : " + CleanTreeWind() + " references mortes ou en double retirees de TreeWind.");

        EditorGUILayout.EndScrollView();
    }

    private void OnSceneGUI(SceneView view)
    {
        if (!painting) return;

        var e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);

        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(id);

        Vector3 point, normal;
        if (!Raycast(e.mousePosition, out point, out normal))
            return;

        bool erasing = e.shift;
        Handles.color = erasing ? new Color(1f, 0.35f, 0.3f, 0.9f) : new Color(0.4f, 1f, 0.6f, 0.9f);
        Handles.DrawWireDisc(point, Vector3.up, radius);
        Handles.DrawWireDisc(point, Vector3.up, radius * 0.99f);
        view.Repaint();

        if (e.alt || e.button != 0) return;

        if (e.type == EventType.MouseDown)
        {
            lastPaintPos = Vector3.positiveInfinity;
            Stroke(point, erasing);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag)
        {
            // en remplissage complet, un clic suffit : on ne repeint pas au glisse
            if (!fullFill && Vector3.Distance(point, lastPaintPos) >= strokeStep)
                Stroke(point, erasing);
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            lastPaintPos = Vector3.positiveInfinity;
            e.Use();
        }
    }

    private void Stroke(Vector3 center, bool erasing)
    {
        lastPaintPos = center;

        if (erasing) Erase(center);
        else if (mode == Mode.GameObjects) PaintObjects(center);
        else PaintTerrain(center);
    }

    // ---------- grille de hachage ----------

    private static long Key(int cx, int cz) { return ((long)cx << 32) ^ (uint)cz; }

    private void HashBegin(float cell)
    {
        hash.Clear();
        hashCell = Mathf.Max(0.05f, cell);
    }

    private void HashAdd(Vector2 p)
    {
        long k = Key(Mathf.FloorToInt(p.x / hashCell), Mathf.FloorToInt(p.y / hashCell));
        List<Vector2> l;
        if (!hash.TryGetValue(k, out l)) hash[k] = l = new List<Vector2>(4);
        l.Add(p);
    }

    // La cellule vaut l'ecart minimum, donc tout voisin plus proche que 'spacing'
    // se trouve forcement dans les 9 cellules autour : 9 lookups au lieu de n comparaisons.
    private bool HashNear(Vector2 p)
    {
        int cx = Mathf.FloorToInt(p.x / hashCell), cz = Mathf.FloorToInt(p.y / hashCell);
        float sq = spacing * spacing;

        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                List<Vector2> l;
                if (!hash.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                for (int i = 0; i < l.Count; i++)
                    if ((l[i] - p).sqrMagnitude < sq) return true;
            }
        return false;
    }

    // Points candidats : grille jitteree pour un remplissage regulier, tirs aleatoires sinon.
    private List<Vector2> Candidates(Vector2 center)
    {
        var pts = new List<Vector2>();

        if (!fullFill)
        {
            for (int i = 0; i < perStroke; i++)
                pts.Add(center + Random.insideUnitCircle * radius);
            return pts;
        }

        float step = spacing * 0.9f;
        int n = Mathf.CeilToInt(radius * 2f / step);
        float jit = step * 0.45f;

        for (int iy = 0; iy <= n; iy++)
            for (int ix = 0; ix <= n; ix++)
            {
                var p = new Vector2(center.x - radius + ix * step + Random.Range(-jit, jit),
                                    center.y - radius + iy * step + Random.Range(-jit, jit));
                if ((p - center).sqrMagnitude <= radius * radius) pts.Add(p);
            }
        return pts;
    }

    private bool Confirm(int count, string what)
    {
        if (count < 4000) return true;
        return EditorUtility.DisplayDialog("Tree Painter",
            "Ce clic va tenter de poser environ " + count.ToString("N0") + " " + what + ".\nContinuer ?",
            "Poser", "Annuler");
    }

    // ---------- pose ----------

    private void PaintObjects(Vector3 center)
    {
        if (prefab == null) { Debug.LogWarning("Tree Painter : aucun prefab assigne."); return; }

        var cands = Candidates(new Vector2(center.x, center.z));
        if (!Confirm(cands.Count, "GameObjects (chacun coute un draw call, prefere le mode Terrain pour la masse)")) return;

        Transform parent = GetParent();
        Physics.SyncTransforms();

        HashBegin(spacing);
        for (int i = 0; i < parent.childCount; i++)
        {
            var p = parent.GetChild(i).position;
            HashAdd(new Vector2(p.x, p.z));
        }

        var placed = new List<Transform>();
        bool bar = cands.Count > 2000;

        for (int i = 0; i < cands.Count; i++)
        {
            if (bar && (i & 511) == 0 &&
                EditorUtility.DisplayCancelableProgressBar("Tree Painter", "Pose des arbres...", i / (float)cands.Count)) break;

            Vector2 c = cands[i];
            if (HashNear(c)) continue;

            Vector3 probe = new Vector3(c.x, center.y, c.y);
            Vector3 pos, nrm;
            if (!Drop(probe, parent, center.y, out pos, out nrm)) continue;
            if (Vector3.Angle(nrm, Vector3.up) > slopeMax) continue;

            pos.y += yOffset;

            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Quaternion rot = Quaternion.Euler(0f, Random.value * 360f, 0f);
            if (alignToNormal) rot = Quaternion.FromToRotation(Vector3.up, nrm) * rot;
            else rot = Quaternion.Euler(Random.Range(-tilt, tilt), rot.eulerAngles.y, Random.Range(-tilt, tilt));

            g.transform.SetPositionAndRotation(pos, rot);
            float s = Random.Range(scaleMin, scaleMax);
            g.transform.localScale = new Vector3(s, s * Random.Range(stretchMin, stretchMax), s);

            Undo.RegisterCreatedObjectUndo(g, "Peindre arbres");
            placed.Add(g.transform);
            HashAdd(c);
        }

        if (bar) EditorUtility.ClearProgressBar();
        if (registerInTreeWind && placed.Count > 0) RegisterTreeWind(placed);
        if (placed.Count > 0) Debug.Log("Tree Painter : " + placed.Count + " arbres poses.");
    }

    private void PaintTerrain(Vector3 center)
    {
        var ter = FindTerrain(center);
        if (ter == null) { Debug.LogWarning("Tree Painter : aucun Terrain sous le curseur."); return; }

        var td = ter.terrainData;
        if (td.treePrototypes.Length == 0) { Debug.LogWarning("Tree Painter : le Terrain n'a aucun prototype d'arbre."); return; }

        var cands = Candidates(new Vector2(center.x, center.z));
        if (!Confirm(cands.Count, "arbres de terrain")) return;

        int proto = Mathf.Clamp(prototypeIndex, 0, td.treePrototypes.Length - 1);
        Undo.RegisterCompleteObjectUndo(td, "Peindre arbres terrain");

        Vector3 tp = ter.transform.position;
        Vector3 size = td.size;

        var list = new List<TreeInstance>(td.treeInstances);
        HashBegin(spacing);
        for (int i = 0; i < list.Count; i++)
            HashAdd(new Vector2(tp.x + list[i].position.x * size.x, tp.z + list[i].position.z * size.z));

        int added = 0;
        bool bar = cands.Count > 2000;

        for (int i = 0; i < cands.Count; i++)
        {
            if (bar && (i & 511) == 0 &&
                EditorUtility.DisplayCancelableProgressBar("Tree Painter", "Pose des arbres...", i / (float)cands.Count)) break;

            Vector2 c = cands[i];

            float u = (c.x - tp.x) / size.x;
            float v = (c.y - tp.z) / size.z;
            if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
            if (td.GetSteepness(u, v) > slopeMax) continue;
            if (HashNear(c)) continue;

            float y = ter.SampleHeight(new Vector3(c.x, 0f, c.y)) + yOffset;
            float s = Random.Range(scaleMin, scaleMax);

            var inst = new TreeInstance();
            inst.position = new Vector3(u, y / size.y, v);
            inst.prototypeIndex = proto;
            inst.widthScale = s;
            inst.heightScale = s * Random.Range(stretchMin, stretchMax);
            inst.rotation = Random.value * 6.2832f;
            inst.color = Color.white;
            inst.lightmapColor = Color.white;

            list.Add(inst);
            HashAdd(c);
            added++;
        }

        if (bar) EditorUtility.ClearProgressBar();

        td.SetTreeInstances(list.ToArray(), true);
        ter.Flush();

        if (added > 0) Debug.Log("Tree Painter : " + added + " arbres poses, " + list.Count + " au total sur le terrain.");
    }

    // ---------- effacement ----------

    private void Erase(Vector3 center)
    {
        if (mode == Mode.GameObjects)
        {
            var go = GameObject.Find(parentName);
            if (go == null) return;

            bool removed = false;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Transform c = go.transform.GetChild(i);
                Vector3 d = c.position - center;
                d.y = 0f;
                if (d.magnitude <= radius) { Undo.DestroyObjectImmediate(c.gameObject); removed = true; }
            }
            if (removed) CleanTreeWind();
            return;
        }

        var ter = FindTerrain(center);
        if (ter == null) return;

        var td = ter.terrainData;
        Undo.RegisterCompleteObjectUndo(td, "Effacer arbres terrain");

        Vector3 tp = ter.transform.position;
        Vector3 size = td.size;
        var keep = new List<TreeInstance>();
        float sq = radius * radius;

        foreach (var t in td.treeInstances)
        {
            float dx = tp.x + t.position.x * size.x - center.x;
            float dz = tp.z + t.position.z * size.z - center.z;
            if (dx * dx + dz * dz > sq) keep.Add(t);
        }

        td.SetTreeInstances(keep.ToArray(), true);
        ter.Flush();
    }

    private void ClearAll()
    {
        if (mode == Mode.GameObjects)
        {
            var go = GameObject.Find(parentName);
            if (go == null) return;
            if (!EditorUtility.DisplayDialog("Tout effacer",
                "Supprimer les " + go.transform.childCount + " enfants de '" + parentName + "' ?", "Supprimer", "Annuler")) return;

            for (int i = go.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(go.transform.GetChild(i).gameObject);

            CleanTreeWind();
        }
        else
        {
            var ter = Terrain.activeTerrain;
            if (ter == null) return;
            if (!EditorUtility.DisplayDialog("Tout effacer",
                "Supprimer les " + ter.terrainData.treeInstanceCount + " arbres du Terrain ?", "Supprimer", "Annuler")) return;

            Undo.RegisterCompleteObjectUndo(ter.terrainData, "Effacer arbres terrain");
            ter.terrainData.SetTreeInstances(new TreeInstance[0], true);
            ter.Flush();
        }
    }

    // ---------- utilitaires ----------

    private bool RayFiltered(Ray ray, Transform ignore, out RaycastHit best)
    {
        best = default(RaycastHit);

        var hits = Physics.RaycastAll(ray, 5000f, groundMask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        bool ok = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i].collider;
            if (c == null || c is CharacterController) continue;
            if (ignore != null && c.transform.IsChildOf(ignore)) continue;
            if (hits[i].distance >= nearest) continue;

            nearest = hits[i].distance;
            best = hits[i];
            ok = true;
        }
        return ok;
    }

    private bool Raycast(Vector2 mouse, out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.up;

        Ray ray = HandleUtility.GUIPointToWorldRay(mouse);

        RaycastHit hit;
        if (RayFiltered(ray, FindParent(), out hit))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        var plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (plane.Raycast(ray, out d)) { point = ray.GetPoint(d); return true; }

        return false;
    }

    private Transform FindParent()
    {
        var go = GameObject.Find(parentName);
        return go != null ? go.transform : null;
    }

    private Transform GetParent()
    {
        var go = GameObject.Find(parentName);
        if (go == null)
        {
            go = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(go, "Creer " + parentName);
        }
        return go.transform;
    }

    private bool Drop(Vector3 probe, Transform ignore, float fallbackY, out Vector3 pos, out Vector3 nrm)
    {
        pos = probe;
        nrm = Vector3.up;

        RaycastHit hit;
        if (RayFiltered(new Ray(probe + Vector3.up * 500f, Vector3.down), ignore, out hit))
        {
            pos = hit.point;
            nrm = hit.normal;
            return true;
        }

        var ter = Terrain.activeTerrain;
        if (ter != null) { pos = new Vector3(probe.x, ter.SampleHeight(probe), probe.z); return true; }

        pos = new Vector3(probe.x, fallbackY, probe.z);
        return true;
    }

    private Terrain FindTerrain(Vector3 p)
    {
        RaycastHit hit;
        if (RayFiltered(new Ray(p + Vector3.up * 500f, Vector3.down), null, out hit))
        {
            var t = hit.collider.GetComponent<Terrain>();
            if (t != null) return t;
        }
        return Terrain.activeTerrain;
    }

    private static void RegisterTreeWind(List<Transform> added)
    {
        var tw = Object.FindFirstObjectByType<TreeWind>();
        if (tw == null) return;

        var so = new SerializedObject(tw);
        var arr = so.FindProperty("swayers");
        int start = arr.arraySize;
        arr.arraySize = start + added.Count;

        for (int i = 0; i < added.Count; i++)
            arr.GetArrayElementAtIndex(start + i).objectReferenceValue = added[i];

        so.ApplyModifiedProperties();
    }

    // TreeWind parcourt tout son tableau a chaque refresh : un trou coute aussi cher
    // qu'un vrai arbre, il faut donc le retirer des qu'on efface.
    private static int CleanTreeWind()
    {
        var tw = Object.FindFirstObjectByType<TreeWind>();
        if (tw == null) return 0;

        var so = new SerializedObject(tw);
        var arr = so.FindProperty("swayers");

        var keep = new List<Object>(arr.arraySize);
        var seen = new HashSet<Object>();
        for (int i = 0; i < arr.arraySize; i++)
        {
            var v = arr.GetArrayElementAtIndex(i).objectReferenceValue;
            if (v == null || !seen.Add(v)) continue;
            keep.Add(v);
        }

        int dropped = arr.arraySize - keep.Count;
        if (dropped == 0) return 0;

        arr.arraySize = keep.Count;
        for (int i = 0; i < keep.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = keep[i];

        so.ApplyModifiedProperties();
        return dropped;
    }
}
