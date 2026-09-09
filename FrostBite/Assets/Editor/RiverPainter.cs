using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Pinceau de riviere.
//   Tools > FrostBite > River Painter
//   Clic gauche = ajouter un point, Maj + clic sur un point = le retirer,
//   poignees jaunes = deplacer un point. Puis "Generer".
public class RiverPainter : EditorWindow
{
    private RiverPath path;
    private bool painting;
    private Vector2 scroll;

    [MenuItem("Tools/FrostBite/River Painter")]
    private static void Open()
    {
        var w = GetWindow<RiverPainter>("River Painter");
        w.minSize = new Vector2(300f, 460f);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        TryAdopt();
    }

    private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

    private void OnSelectionChange() { TryAdopt(); Repaint(); }

    private void TryAdopt()
    {
        if (Selection.activeGameObject == null) return;
        var p = Selection.activeGameObject.GetComponentInParent<RiverPath>();
        if (p != null) path = p;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        path = (RiverPath)EditorGUILayout.ObjectField("Trace", path, typeof(RiverPath), true);

        if (GUILayout.Button("Nouveau trace", GUILayout.Height(24f)))
        {
            var go = new GameObject("River");
            Undo.RegisterCreatedObjectUndo(go, "Nouvelle riviere");
            path = go.AddComponent<RiverPath>();
            Selection.activeGameObject = go;
            painting = true;
        }

        if (path == null)
        {
            EditorGUILayout.HelpBox("Cree un trace, puis clique dans la vue scene pour poser les points de la riviere.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space();
        var col = GUI.backgroundColor;
        GUI.backgroundColor = painting ? new Color(0.5f, 1f, 0.5f) : col;
        if (GUILayout.Button(painting ? "PINCEAU ACTIF  (clic pour desactiver)" : "Activer le pinceau", GUILayout.Height(32f)))
        {
            painting = !painting;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = col;

        EditorGUILayout.HelpBox("Clic gauche : ajouter un point\nMaj + clic sur un point : le retirer\nPoignees jaunes : deplacer\nAlt : camera libre", MessageType.None);

        EditorGUILayout.LabelField("Points", path.points.Count + "   (longueur ~" + path.ApproxLength().ToString("F0") + " m)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Forme", EditorStyles.boldLabel);
        Undo.RecordObject(path, "Reglages riviere");
        path.width = EditorGUILayout.Slider("Largeur de l'eau", path.width, 3f, 60f);
        path.depth = EditorGUILayout.Slider("Profondeur", path.depth, 0.5f, 12f);
        path.bankWidth = EditorGUILayout.Slider("Largeur des berges", path.bankWidth, 2f, 60f);
        path.waterDrop = EditorGUILayout.Slider("Eau sous le terrain", path.waterDrop, 0f, 6f);
        path.stepLength = EditorGUILayout.Slider("Pas du maillage", path.stepLength, 1f, 12f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Profil en long", EditorStyles.boldLabel);
        path.levelSmooth = EditorGUILayout.Slider("Lissage du niveau", path.levelSmooth, 10f, 200f);
        path.forceDownhill = EditorGUILayout.Toggle("Forcer la descente", path.forceDownhill);
        path.endTaper = EditorGUILayout.Slider("Fondu des extremites", path.endTaper, 0f, 80f);
        if (path.forceDownhill)
            EditorGUILayout.HelpBox("A n'activer que si ton trace descend vraiment. Sinon la riviere prend l'altitude de son point le plus bas et creuse un canal.", MessageType.Warning);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Courant", EditorStyles.boldLabel);
        path.flowSpeed = EditorGUILayout.Slider("Vitesse (m/s)", path.flowSpeed, 0.1f, 6f);
        EditorGUILayout.LabelField(" ", path.flowSpeed < 1f ? "eau calme"
            : path.flowSpeed < 2.5f ? "riviere tranquille"
            : path.flowSpeed < 4f ? "courant vif" : "torrent");

        EditorGUILayout.Space();
        path.spray = EditorGUILayout.Toggle("Embruns", path.spray);
        if (path.spray) path.sprayEvery = EditorGUILayout.Slider("Un emetteur tous les", path.sprayEvery, 15f, 200f);

        EditorGUILayout.Space();
        GUI.enabled = path.points.Count >= 2;
        GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
        if (GUILayout.Button("Generer la riviere", GUILayout.Height(34f)))
            Generate();
        GUI.backgroundColor = col;
        GUI.enabled = true;

        EditorGUILayout.HelpBox("La generation creuse le lit dans le terrain. Ctrl+Z restaure le terrain et supprime le maillage.", MessageType.Warning);

        GUI.enabled = path.HasBackup;
        if (GUILayout.Button("Restaurer le terrain (annuler le creusement)"))
        {
            var t2 = Terrain.activeTerrain;
            if (t2 != null)
            {
                Undo.RegisterCompleteObjectUndo(t2.terrainData, "Restaurer le terrain");
                RestoreTerrain(t2);
                EditorUtility.SetDirty(t2.terrainData);
            }
        }
        GUI.enabled = true;

        if (GUILayout.Button("Effacer tous les points"))
        {
            Undo.RecordObject(path, "Effacer les points");
            path.points.Clear();
        }

        EditorGUILayout.EndScrollView();
    }

    // ---------------- vue scene ----------------

    private void OnSceneGUI(SceneView view)
    {
        if (path == null || !painting) return;

        var e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);

        // apercu du trace
        if (path.points.Count >= 2)
        {
            Handles.color = new Color(0.3f, 0.75f, 1f, 0.9f);
            Vector3 prev = path.Sample(0f);
            for (int i = 1; i <= 160; i++)
            {
                Vector3 c = path.Sample(i / 160f);
                Handles.DrawAAPolyLine(4f, prev, c);
                Vector3 t = (c - prev).normalized;
                Vector3 n = Vector3.Cross(Vector3.up, t) * (path.width * 0.5f);
                Handles.color = new Color(0.3f, 0.75f, 1f, 0.25f);
                Handles.DrawAAPolyLine(2f, prev + n, c + n);
                Handles.DrawAAPolyLine(2f, prev - n, c - n);
                Handles.color = new Color(0.3f, 0.75f, 1f, 0.9f);
                prev = c;
            }
        }

        // poignees de deplacement
        for (int i = 0; i < path.points.Count; i++)
        {
            Handles.color = new Color(1f, 0.85f, 0.2f);
            float size = HandleUtility.GetHandleSize(path.points[i]) * 0.09f;
            EditorGUI.BeginChangeCheck();
            Vector3 np = Handles.FreeMoveHandle(path.points[i], size, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Deplacer un point");
                path.points[i] = SnapToGround(np);
            }
        }

        view.Repaint();

        if (e.alt || e.button != 0 || e.type != EventType.MouseDown) return;

        Vector3 hit;
        if (!GroundPoint(e.mousePosition, out hit)) return;

        if (e.shift)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < path.points.Count; i++)
            {
                float d = Vector3.Distance(path.points[i], hit);
                if (d < bd) { bd = d; best = i; }
            }
            if (best >= 0 && bd < Mathf.Max(4f, path.width))
            {
                Undo.RecordObject(path, "Retirer un point");
                path.points.RemoveAt(best);
            }
        }
        else
        {
            Undo.RecordObject(path, "Ajouter un point");
            path.points.Add(hit);
        }

        EditorUtility.SetDirty(path);
        e.Use();
    }

    private static bool GroundPoint(Vector2 mouse, out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = HandleUtility.GUIPointToWorldRay(mouse);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 5000f, ~0, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        var plane = new Plane(Vector3.up, Vector3.zero);
        float d;
        if (plane.Raycast(ray, out d)) { point = ray.GetPoint(d); return true; }
        return false;
    }

    private static Vector3 SnapToGround(Vector3 p)
    {
        var ter = Terrain.activeTerrain;
        if (ter != null) return new Vector3(p.x, ter.SampleHeight(p), p.z);
        return p;
    }

    // ---------------- generation ----------------

    private struct Node { public Vector3 pos; public Vector3 nrm; public float arc; public float level; }

    // Permet de relancer la generation sans passer par la fenetre.
    public static void BuildFor(RiverPath p)
    {
        if (p == null) return;
        var w = CreateInstance<RiverPainter>();
        w.path = p;
        w.Generate();
        DestroyImmediate(w);
    }

    private void Generate()
    {
        var ter = Terrain.activeTerrain;
        if (ter == null) { EditorUtility.DisplayDialog("River Painter", "Aucun Terrain actif dans la scene.", "OK"); return; }

        var nodes = BuildNodes(ter);
        if (nodes.Count < 2) return;

        Undo.RegisterCompleteObjectUndo(ter.terrainData, "Creuser le lit de la riviere");
        Undo.RecordObject(path, "Sauvegarde du terrain");
        RestoreTerrain(ter);          // sinon regenerer cumule les creusements
        Carve(ter, nodes);
        BuildWater(ter, nodes);

        EditorUtility.SetDirty(ter.terrainData);
        Debug.Log("River Painter : riviere de " + path.ApproxLength().ToString("F0") + " m generee (" + nodes.Count + " sections).");
    }

    private List<Node> BuildNodes(Terrain ter)
    {
        float len = path.ApproxLength();
        int count = Mathf.Max(2, Mathf.CeilToInt(len / Mathf.Max(path.stepLength, 0.5f)));

        var nodes = new List<Node>(count + 1);
        float arc = 0f;
        Vector3 prev = path.Sample(0f);

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            Vector3 p = path.Sample(t);
            if (i > 0) arc += Vector3.Distance(prev, p);

            Vector3 fwd = (path.Sample(Mathf.Min(t + 0.004f, 1f)) - path.Sample(Mathf.Max(t - 0.004f, 0f)));
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            fwd.Normalize();

            var n = new Node();
            n.pos = new Vector3(p.x, ter.SampleHeight(p), p.z);
            n.nrm = Vector3.Cross(Vector3.up, fwd);
            n.arc = arc;
            n.level = n.pos.y - path.waterDrop;
            nodes.Add(n);
            prev = p;
        }

        // Le niveau suit le terrain lisse : la riviere serpente en restant a
        // 'waterDrop' sous le sol, au lieu d'adopter partout son point le plus bas.
        int win = Mathf.Max(1, Mathf.RoundToInt(path.levelSmooth / Mathf.Max(path.stepLength, 0.5f)) / 2);
        var ground = new float[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) ground[i] = nodes[i].pos.y;

        for (int i = 0; i < nodes.Count; i++)
        {
            int a = Mathf.Max(0, i - win), b = Mathf.Min(nodes.Count - 1, i + win);
            float sum = 0f;
            for (int j = a; j <= b; j++) sum += ground[j];

            var n = nodes[i];
            n.level = sum / (b - a + 1) - path.waterDrop;
            nodes[i] = n;
        }

        if (path.forceDownhill)
            for (int i = 1; i < nodes.Count; i++)
            {
                var n = nodes[i];
                n.level = Mathf.Min(n.level, nodes[i - 1].level);
                nodes[i] = n;
            }

        // le fond ne doit jamais percer le plancher du terrain
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            n.level = Mathf.Max(n.level, path.depth + 0.3f);
            nodes[i] = n;
        }

        return nodes;
    }

    private void Carve(Terrain ter, List<Node> nodes)
    {
        var td = ter.terrainData;
        int res = td.heightmapResolution;
        Vector3 tp = ter.transform.position;
        Vector3 size = td.size;

        float half = path.width * 0.5f;
        float reach = half + path.bankWidth;
        float totalArc = nodes[nodes.Count - 1].arc;

        // on ne touche qu'a la boite englobante du trace
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var n in nodes)
        {
            minX = Mathf.Min(minX, n.pos.x); maxX = Mathf.Max(maxX, n.pos.x);
            minZ = Mathf.Min(minZ, n.pos.z); maxZ = Mathf.Max(maxZ, n.pos.z);
        }
        minX -= reach + 4f; maxX += reach + 4f;
        minZ -= reach + 4f; maxZ += reach + 4f;

        int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - tp.x) / size.x * (res - 1)), 0, res - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((maxX - tp.x) / size.x * (res - 1)), 0, res - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt((minZ - tp.z) / size.z * (res - 1)), 0, res - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt((maxZ - tp.z) / size.z * (res - 1)), 0, res - 1);

        int w = x1 - x0 + 1, h = z1 - z0 + 1;
        if (w <= 1 || h <= 1) return;

        var heights = td.GetHeights(x0, z0, w, h);
        float stepX = size.x / (res - 1), stepZ = size.z / (res - 1);

        // memorise l'etat d'origine pour pouvoir regenerer ou restaurer
        path.savedX = x0; path.savedZ = z0; path.savedW = w; path.savedH = h;
        path.savedHeights = new float[w * h];
        for (int zz = 0; zz < h; zz++)
            for (int xx = 0; xx < w; xx++)
                path.savedHeights[zz * w + xx] = heights[zz, xx];
        EditorUtility.SetDirty(path);

        for (int zz = 0; zz < h; zz++)
        {
            if ((zz & 15) == 0)
                EditorUtility.DisplayProgressBar("River Painter", "Creusement du lit...", zz / (float)h);

            float wz = tp.z + (z0 + zz) * stepZ;
            for (int xx = 0; xx < w; xx++)
            {
                float wx = tp.x + (x0 + xx) * stepX;

                // Distance au SEGMENT le plus proche, pas au point le plus proche :
                // un champ de distance base sur les points produit un disque a chaque
                // bout du trace, donc un cratere circulaire.
                float bd = float.MaxValue;
                float level = 0f, arcHere = 0f;

                for (int i = 0; i < nodes.Count - 1; i++)
                {
                    float ax = nodes[i].pos.x, az = nodes[i].pos.z;
                    float bx = nodes[i + 1].pos.x, bz = nodes[i + 1].pos.z;
                    float ex = bx - ax, ez = bz - az;
                    float segLen2 = ex * ex + ez * ez;
                    if (segLen2 < 1e-6f) continue;

                    // rejet rapide : segment trop loin pour compter
                    float mx = (ax + bx) * 0.5f - wx, mz = (az + bz) * 0.5f - wz;
                    float lim = Mathf.Sqrt(segLen2) * 0.5f + reach;
                    if (mx * mx + mz * mz > lim * lim) continue;

                    float u = Mathf.Clamp01(((wx - ax) * ex + (wz - az) * ez) / segLen2);
                    float px = ax + ex * u - wx, pz = az + ez * u - wz;
                    float d = px * px + pz * pz;

                    if (d < bd)
                    {
                        bd = d;
                        level = Mathf.Lerp(nodes[i].level, nodes[i + 1].level, u);
                        arcHere = Mathf.Lerp(nodes[i].arc, nodes[i + 1].arc, u);
                    }
                }

                float dist = Mathf.Sqrt(bd);
                if (dist > reach) continue;

                // fondu aux deux bouts : la riviere s'efface au lieu de finir en trou
                float edge = Mathf.Min(arcHere, totalArc - arcHere);
                float fade = Mathf.Clamp01(edge / Mathf.Max(path.endTaper, 0.01f));
                fade = fade * fade * (3f - 2f * fade);
                if (fade <= 0.001f) continue;

                float cur = heights[zz, xx] * size.y;
                float target;

                // La ligne d'eau est atteinte un peu avant le bord du ruban : le bord de l'eau
                // se glisse ainsi dans la berge au lieu de rester suspendu, malgre la
                // resolution limitee de la heightmap.
                float halfHere = half * Mathf.Lerp(0.3f, 1f, fade);
                float inner = halfHere * 0.85f;
                float depthHere = path.depth * fade;

                if (dist <= inner)
                {
                    // section parabolique : creux au milieu, remontee douce vers la rive
                    float k = dist / Mathf.Max(inner, 0.01f);
                    target = level - depthHere * (1f - k * k);
                }
                else
                {
                    float t = Mathf.Clamp01((dist - inner) / Mathf.Max(path.bankWidth, 0.01f));
                    target = Mathf.Lerp(level, cur, t * t * (3f - 2f * t));
                }

                heights[zz, xx] = Mathf.Clamp01(target / size.y);
            }
        }

        EditorUtility.ClearProgressBar();
        td.SetHeights(x0, z0, heights);
        ter.Flush();
    }

    private void BuildWater(Terrain ter, List<Node> nodes)
    {
        // on repart d'un enfant propre
        var old = path.transform.Find("Water");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var root = new GameObject("Water");
        Undo.RegisterCreatedObjectUndo(root, "Eau de la riviere");
        root.transform.SetParent(path.transform, false);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        float half = path.width * 0.5f;

        // Le ruban est subdivise en travers : avec seulement deux sommets de large,
        // les vagues du shader ne peuvent pas exister entre les rives et la surface
        // rend en facettes.
        int cols = Mathf.Clamp(Mathf.RoundToInt(path.width / 2.5f), 2, 12);
        int perRow = cols + 1;

        var verts = new Vector3[nodes.Count * perRow];
        var uvs = new Vector2[nodes.Count * perRow];
        var nrms = new Vector3[nodes.Count * perRow];
        float totalArc = nodes[nodes.Count - 1].arc;

        for (int i = 0; i < nodes.Count; i++)
        {
            float edge = Mathf.Min(nodes[i].arc, totalArc - nodes[i].arc);
            float fade = Mathf.Clamp01(edge / Mathf.Max(path.endTaper, 0.01f));
            fade = fade * fade * (3f - 2f * fade);
            float hw = half * Mathf.Lerp(0.3f, 1f, fade);

            Vector3 c = new Vector3(nodes[i].pos.x, nodes[i].level, nodes[i].pos.z);

            for (int j = 0; j <= cols; j++)
            {
                float lat = hw - 2f * hw * (j / (float)cols);
                int v = i * perRow + j;
                verts[v] = c + nodes[i].nrm * lat;
                // UV en metres : x = travers, y = longueur d'arc -> le courant suit les virages
                uvs[v] = new Vector2(lat, nodes[i].arc);
                nrms[v] = Vector3.up;
            }
        }

        var tris = new int[(nodes.Count - 1) * cols * 6];
        int k = 0;
        for (int i = 0; i < nodes.Count - 1; i++)
            for (int j = 0; j < cols; j++)
            {
                int v00 = i * perRow + j;
                int v01 = v00 + 1;
                int v10 = (i + 1) * perRow + j;
                int v11 = v10 + 1;
                // enroulement horaire vu de dessus : la surface regarde le ciel
                tris[k++] = v00; tris[k++] = v01; tris[k++] = v10;
                tris[k++] = v01; tris[k++] = v11; tris[k++] = v10;
            }

        var mesh = new Mesh { name = "RiverSurface", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts; mesh.uv = uvs; mesh.normals = nrms; mesh.triangles = tris;
        mesh.RecalculateBounds();

        string dir = "Assets/Meshes";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Meshes");
        AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(dir + "/RiverSurface.asset"));

        var surf = new GameObject("Surface");
        surf.transform.SetParent(root.transform, false);
        surf.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = surf.AddComponent<MeshRenderer>();
        mr.sharedMaterial = CurvedMaterial();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // vitesse propre a cette riviere, sans dupliquer le materiau
        var mpb = new MaterialPropertyBlock();
        mpb.SetFloat("_FlowSpeed", path.flowSpeed);
        mr.SetPropertyBlock(mpb);

        // volume froid : plusieurs boites, un seul ColdWater qui compte les chevauchements
        var vol = new GameObject("ColdWaterVolume");
        vol.transform.SetParent(root.transform, false);
        vol.AddComponent<ColdWater>();

        // Sans Rigidbody, les OnTriggerEnter des boites enfants partent vers les
        // enfants eux-memes et ColdWater ne recoit jamais rien.
        var rb = vol.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        const int CHUNK = 6;
        for (int i = 0; i < nodes.Count - 1; i += CHUNK)
        {
            int j = Mathf.Min(i + CHUNK, nodes.Count - 1);
            Vector3 a = nodes[i].pos, b = nodes[j].pos;
            Vector3 mid = (a + b) * 0.5f;
            float lvl = (nodes[i].level + nodes[j].level) * 0.5f;
            float len = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
            Vector3 fwd = (b - a); fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) continue;

            var box = new GameObject("Box" + i);
            box.transform.SetParent(vol.transform, false);
            box.transform.SetPositionAndRotation(
                new Vector3(mid.x, lvl - path.depth * 0.5f + 0.5f, mid.z),
                Quaternion.LookRotation(fwd.normalized, Vector3.up));
            var bc = box.AddComponent<BoxCollider>();
            bc.size = new Vector3(path.width, path.depth + 1f, len + path.stepLength);
            bc.isTrigger = true;
        }

        if (path.spray) BuildSpray(root.transform, nodes);
    }

    private void BuildSpray(Transform parent, List<Node> nodes)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WaterSpray.mat");
        if (mat == null) { Debug.LogWarning("River Painter : WaterSpray.mat introuvable, embruns ignores."); return; }

        float total = nodes[nodes.Count - 1].arc;
        int n = Mathf.Max(1, Mathf.RoundToInt(total / Mathf.Max(path.sprayEvery, 5f)));

        for (int s = 0; s < n; s++)
        {
            float target = (s + 0.5f) / n * total;
            int idx = 0;
            for (int i = 0; i < nodes.Count; i++) if (nodes[i].arc <= target) idx = i;

            var go = new GameObject("Rapids" + s);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(
                new Vector3(nodes[idx].pos.x, nodes[idx].level + 0.15f, nodes[idx].pos.z),
                Quaternion.LookRotation(Vector3.Cross(nodes[idx].nrm, Vector3.up), Vector3.up));

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.5f));
            main.gravityModifier = 0.55f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;

            var em = ps.emission; em.rateOverTime = 55f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(path.width * 0.8f, 0.4f, path.sprayEvery * 0.8f);

            var colm = ps.colorOverLifetime;
            colm.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colm.color = new ParticleSystem.MinMaxGradient(g);

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    private void RestoreTerrain(Terrain ter)
    {
        if (path == null || !path.HasBackup) return;

        var back = new float[path.savedH, path.savedW];
        for (int z = 0; z < path.savedH; z++)
            for (int x = 0; x < path.savedW; x++)
                back[z, x] = path.savedHeights[z * path.savedW + x];

        ter.terrainData.SetHeights(path.savedX, path.savedZ, back);
        ter.Flush();
    }

    private static Material CurvedMaterial()
    {
        const string p = "Assets/Materials/RiverWaterCurved.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;

        var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RiverWater.mat");
        m = src != null ? new Material(src) : new Material(Shader.Find("FrostBite/RiverWater"));
        m.SetFloat("_UseUVFlow", 1f);   // le courant suit les UV du ruban, donc les virages
        AssetDatabase.CreateAsset(m, p);
        return m;
    }
}
