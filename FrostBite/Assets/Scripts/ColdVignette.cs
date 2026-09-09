using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class ColdVignette : MonoBehaviour
{
    [SerializeField] private Volume volume;

    [SerializeField] private float smooth = 4f;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 1f;

    private Vignette vignette;
    private float current;

    private void Awake()
    {
        if (volume == null || volume.profile == null)
            return;

        volume.profile.TryGet(out vignette);
    }

    private void Update()
    {
        if (vignette == null || PlayerStat.Instance == null)
            return;

        float target = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            PlayerStat.Instance.ColdPart
        );

        current = Mathf.Lerp(
            current,
            target,
            smooth * Time.deltaTime
        );

        vignette.intensity.value = current;
    }
}