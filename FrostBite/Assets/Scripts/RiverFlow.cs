using UnityEngine;

// Oriente le courant du shader FrostBite/RiverWater sur l'axe Z du transform.
// Tourne l'objet et l'eau coule dans la nouvelle direction, en editeur comme en jeu.
[ExecuteAlways]
[DisallowMultipleComponent]
public class RiverFlow : MonoBehaviour
{
    [SerializeField] private Renderer surface;
    [SerializeField] private float speed = 1.8f;
    [SerializeField] private float waveHeight = 0.4f;
    [SerializeField] private float foamAmount = 0.6f;

    private static readonly int FlowDirId = Shader.PropertyToID("_FlowDir");
    private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
    private static readonly int WaveHeightId = Shader.PropertyToID("_WaveHeight");
    private static readonly int FoamAmountId = Shader.PropertyToID("_FoamAmount");

    private MaterialPropertyBlock block;
    private Vector2 dir;

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    private void Update()
    {
        if (!Application.isPlaying) Apply();
    }

    public void Apply()
    {
        if (surface == null) surface = GetComponentInChildren<Renderer>();
        if (surface == null) return;

        dir = new Vector2(transform.forward.x, transform.forward.z);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        if (block == null) block = new MaterialPropertyBlock();

        surface.GetPropertyBlock(block);
        block.SetVector(FlowDirId, new Vector4(dir.x, dir.y, 0f, 0f));
        block.SetFloat(FlowSpeedId, speed);
        block.SetFloat(WaveHeightId, waveHeight);
        block.SetFloat(FoamAmountId, foamAmount);
        surface.SetPropertyBlock(block);
    }
}
