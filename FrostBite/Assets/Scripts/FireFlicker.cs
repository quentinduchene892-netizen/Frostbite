using UnityEngine;

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class FireFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 4f;
    [SerializeField] private float intensityJitter = 0.9f;
    [SerializeField] private float baseRange = 12f;
    [SerializeField] private float rangeJitter = 1.6f;

    [Header("Rythme")]
    [Tooltip("La respiration lente de la flamme.")]
    [SerializeField] private float slowSpeed = 1.7f;
    [Tooltip("Le crepitement rapide par-dessus.")]
    [SerializeField] private float fastSpeed = 9f;
    [SerializeField] private Vector3 shift = new Vector3(0.07f, 0.05f, 0.07f);

    private Light lamp;
    private Vector3 home;
    private float seed;

    private void Awake()
    {
        lamp = GetComponent<Light>();
        home = transform.localPosition;
        seed = Random.value * 100f;

        if (baseIntensity <= 0f) 
            baseIntensity = lamp.intensity;
        if (baseRange <= 0f) baseRange = lamp.range;
    }

    private void OnEnable()
    {
        if (lamp == null) return;

        lamp.intensity = baseIntensity;
        lamp.range = baseRange;
    }

    private void Update()
    {
        if (lamp == null) return;

        float t = Time.time;

        float slow = Mathf.PerlinNoise(seed, t * slowSpeed) - 0.5f;
        float fast = Mathf.PerlinNoise(seed + 31f, t * fastSpeed) - 0.5f;
        float f = slow * 1.4f + fast * 0.6f;

        lamp.intensity = Mathf.Max(0.1f, baseIntensity + f * intensityJitter);
        lamp.range = Mathf.Max(1f, baseRange + f * rangeJitter);

        transform.localPosition = home + new Vector3(
            (Mathf.PerlinNoise(seed + 7f, t * 3.1f) - 0.5f) * shift.x,
            (Mathf.PerlinNoise(seed + 13f, t * 2.3f) - 0.5f) * shift.y,
            (Mathf.PerlinNoise(seed + 19f, t * 2.9f) - 0.5f) * shift.z);
    }
}
