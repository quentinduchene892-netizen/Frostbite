using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class StressEffects : MonoBehaviour
{
    [SerializeField] Volume volume;
    [SerializeField] CameraPlayer cam;

    [Header("Seuils (part de stress 0-1)")]
    [SerializeField] float calmBand = 0.25f;
    [SerializeField] float tensionBand = 0.50f;
    [SerializeField] float panicBand = 0.70f;
    [SerializeField] float collapseBand = 0.85f;

    [Header("Pouls")]
    [SerializeField] AudioClip heartClip;
    [SerializeField] float bpmCalm = 60f;
    [SerializeField] float bpmMax = 150f;
    [SerializeField] float beatVolume = 0.55f;

    [Header("Souffle")]
    [SerializeField] AudioClip breathClip;
    [SerializeField] float breathVolume = 0.5f;
    [SerializeField] float breathPitchMin = 1f;
    [SerializeField] float breathPitchMax = 1.25f;

    [Header("Vignette (reprend aussi le froid)")]
    [SerializeField] float coldVignette = 1f;
    [SerializeField] float stressVignette = 0.55f;
    [SerializeField] float pulseVignette = 0.18f;
    [SerializeField] float vignetteSmooth = 4f;
    [SerializeField] Color coldTint = new Color(0.58f, 0.89f, 1f);
    [SerializeField] Color stressTint = new Color(0.12f, 0.01f, 0.01f);
    [Range(0f, 1f)][SerializeField] float vignetteMax = 0.7f;
    [Tooltip("Part de stress au-dela de laquelle la vignette cesse de se refermer. La teinte, elle, continue de virer : "
        + "passe ce seuil on garde l'ouverture telle quelle au lieu de finir sur un trou d'epingle.")]
    [Range(0f, 1f)][SerializeField] float vignetteHoldFrom = 0.60f;
    [SerializeField] float smoothCalm = 0.7f;
    [SerializeField] float smoothPanic = 0.5f;
    [SerializeField] bool roundedVignette = true;

    [Header("Distorsion")]
    [SerializeField] float pulseDistort = 0.35f;

    [Header("Acouphene")]
    [SerializeField] float tinnitusVolume = 0.05f;
    [SerializeField] float tinnitusHz = 3000f;
    [SerializeField] float tinnitusFade = 1.5f;

    [Header("Ambiance etouffee")]
    [SerializeField] float cutoffOpen = 22000f;
    [SerializeField] float cutoffChoked = 900f;

    [Header("Faux positifs")]
    [SerializeField] AudioClip[] ghostSteps;
    [SerializeField] float ghostMinDelay = 4f;
    [SerializeField] float ghostMaxDelay = 12f;

    [Header("Chaleur du feu")]
    [Tooltip("Teinte prise par l'image quand on se tient dans le rayon d'un feu allume.")]
    [SerializeField] Color fireTint = new Color(1f, 0.70f, 0.42f);
    [Tooltip("Force de la teinte chaude, 0 pour la desactiver.")]
    [Range(0f, 1f)] [SerializeField] float fireWarmth = 0.45f;
    [Tooltip("Part de la vignette de froid effacee au plus pres des flammes.")]
    [Range(0f, 1f)] [SerializeField] float fireRelief = 0.65f;
    [Tooltip("Vitesse d'apparition et de disparition de la chaleur.")]
    [SerializeField] float fireSmooth = 1.8f;

    [Header("Micro-blackouts")]
    [SerializeField] float blackoutFrom = 0.95f;
    [SerializeField] float blackoutTime = 0.2f;

    public static float SwayMultiplier = 1f;

    Vignette vignette;
    ChromaticAberration chroma;
    FilmGrain grain;
    ColorAdjustments color;
    LensDistortion lens;

    AudioSource heartSrc;
    AudioSource breathSrc;
    AudioSource tinnitusSrc;
    AudioSource ghostSrc;

    readonly List<AudioLowPassFilter> ambience = new List<AudioLowPassFilter>();
    readonly List<Campfire> fires = new List<Campfire>();

    float stress;
    float lastStress;
    float beatTimer;
    float pulse;
    float vignetteCurrent;
    float ghostTimer;
    float blackoutTimer;
    float blackoutCooldown;
    float rescanTimer;
    float warmth;

    void Awake()
    {
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out chroma);
            volume.profile.TryGet(out grain);
            volume.profile.TryGet(out color);
            volume.profile.TryGet(out lens);
        }

        if (cam == null) cam = GetComponent<CameraPlayer>();

        heartSrc = MakeSource("Heartbeat", heartClip != null ? heartClip : Synth.Heartbeat(), false);
        tinnitusSrc = MakeSource("Tinnitus", Synth.Tone(tinnitusHz, 2f), true);
        ghostSrc = MakeSource("GhostSteps", null, false);
        ghostSrc.spatialBlend = 1f;

        if (breathClip != null)
        {
            breathSrc = MakeSource("Breath", breathClip, true);
            breathSrc.Play();
        }

        tinnitusSrc.Play();

        CollectAmbience();
        ghostTimer = Random.Range(ghostMinDelay, ghostMaxDelay);
    }

    AudioSource MakeSource(string label, AudioClip clip, bool loop)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    void CollectAmbience()
    {
        fires.Clear();
        fires.AddRange(FindObjectsByType<Campfire>(FindObjectsInactive.Exclude));

        ambience.Clear();

        foreach (var src in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!src.loop || src.transform.IsChildOf(transform)) continue;

            var filter = src.GetComponent<AudioLowPassFilter>();
            if (filter == null)
            {
                filter = src.gameObject.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = cutoffOpen;
            }

            ambience.Add(filter);
        }
    }

    void Update()
    {
        if (PlayerStat.Instance == null) return;

        stress = PlayerStat.Instance.StressPart;

        float unease = Band(calmBand, tensionBand);
        float tension = Band(tensionBand, panicBand);
        float panic = Band(panicBand, collapseBand);
        float collapse = Band(collapseBand, 1f);

        warmth = Mathf.MoveTowards(warmth, FireWarmth(), fireSmooth * Time.deltaTime);

        Pulse(panic, collapse);
        Grade(unease, tension, panic, collapse);
        Sound(unease, tension, collapse);
        Ghosts(panic);
        Blackout();

        SwayMultiplier = 1f + tension * 0.5f + panic * 0.5f + collapse * 1.5f;

        rescanTimer -= Time.deltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = 3f;
            CollectAmbience();
        }

        lastStress = stress;
    }

    float FireWarmth()
    {
        var stat = PlayerStat.Instance;
        if (stat == null) return 0f;

        float best = 0f;

        for (int i = 0; i < fires.Count; i++)
        {
            Campfire f = fires[i];
            if (f == null || !f.Lit) continue;

            float d = Vector3.Distance(f.transform.position, stat.transform.position);
            float part = 1f - Mathf.Clamp01(d / Mathf.Max(f.Radius, 0.01f));

            if (part > best) best = part;
        }

        return best;
    }

    float Band(float lo, float hi)
    {
        return Mathf.InverseLerp(lo, hi, stress);
    }

    void Pulse(float panic, float collapse)
    {
        float bpm = Mathf.Lerp(bpmCalm, bpmMax, stress);
        float interval = 60f / bpm;

        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime / Mathf.Max(interval * 0.5f, 0.01f));

        beatTimer -= Time.deltaTime;
        if (beatTimer > 0f) return;

        beatTimer = interval;
        pulse = 1f;

        if (stress >= tensionBand)
        {
            heartSrc.volume = beatVolume * Band(tensionBand, 1f);
            heartSrc.pitch = Mathf.Lerp(0.9f, 1.25f, stress);
            heartSrc.Play();
        }

        if (cam != null && stress >= panicBand)
            cam.Kick(0.10f + collapse * 0.25f);
    }

    void Grade(float unease, float tension, float panic, float collapse)
    {
        if (vignette != null)
        {
            float coldWeight = PlayerStat.Instance.ColdPart * coldVignette;
            float stressWeight = Band(calmBand, 1f) * stressVignette;

            float held = Mathf.Min(stress, vignetteHoldFrom);
            float heldWeight = Mathf.InverseLerp(calmBand, 1f, held) * stressVignette;
            float heldTension = Mathf.InverseLerp(tensionBand, panicBand, held);
            float heldPanic = Mathf.InverseLerp(panicBand, collapseBand, held);

            vignetteCurrent = Mathf.Lerp(vignetteCurrent, coldWeight + heldWeight, vignetteSmooth * Time.deltaTime);

            float serre = Mathf.Min(vignetteCurrent + pulse * pulseVignette * heldTension, vignetteMax);
            vignette.intensity.value = serre * (1f - warmth * fireRelief);

            Color froid = Color.Lerp(coldTint, stressTint,
                stressWeight / Mathf.Max(coldWeight + stressWeight, 0.001f));
            vignette.color.value = Color.Lerp(froid, fireTint, warmth);

            vignette.smoothness.value = Mathf.Lerp(smoothCalm, smoothPanic, heldPanic);
            vignette.rounded.value = roundedVignette;
        }

        if (chroma != null)
            chroma.intensity.value = Mathf.Clamp01(tension * 0.25f + collapse * 0.4f);

        if (grain != null)
            grain.intensity.value = Mathf.Clamp01(unease * 0.15f + tension * 0.35f);

        if (color != null)
        {
            color.saturation.value = -(unease * 10f + tension * 25f + panic * 20f + collapse * 45f) + warmth * 25f;
            color.colorFilter.value = Color.Lerp(Color.white, fireTint, warmth * fireWarmth);
        }

        if (lens != null)
            lens.intensity.value = -pulse * Mathf.Max(tension * 0.4f, panic) * pulseDistort;
    }

    void Sound(float unease, float tension, float collapse)
    {
        if (breathSrc != null)
        {
            breathSrc.volume = breathVolume * unease;
            breathSrc.pitch = Mathf.Lerp(breathPitchMin, breathPitchMax, Band(calmBand, 1f));
        }

        tinnitusSrc.volume = Mathf.Lerp(tinnitusSrc.volume, collapse * tinnitusVolume,
                                        tinnitusFade * Time.deltaTime);

        float cutoff = Mathf.Lerp(cutoffOpen, cutoffChoked, tension);
        foreach (var filter in ambience)
            if (filter != null)
                filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, cutoff, 3f * Time.deltaTime);
    }

    void Ghosts(float panic)
    {
        if (panic <= 0f || ghostSteps == null || ghostSteps.Length == 0) return;

        ghostTimer -= Time.deltaTime * (0.5f + panic);
        if (ghostTimer > 0f) return;

        ghostTimer = Random.Range(ghostMinDelay, ghostMaxDelay);

        Vector3 back = -transform.forward * Random.Range(3f, 6f)
                     + transform.right * Random.Range(-2f, 2f);

        ghostSrc.transform.position = transform.position + back;
        ghostSrc.volume = 0.4f + panic * 0.3f;
        ghostSrc.pitch = Random.Range(0.9f, 1.1f);
        ghostSrc.PlayOneShot(ghostSteps[Random.Range(0, ghostSteps.Length)]);
    }

    void Blackout()
    {
        if (color == null) return;

        blackoutTimer -= Time.deltaTime;
        blackoutCooldown -= Time.deltaTime;

        if (stress - lastStress > 0.05f)
            blackoutTimer = blackoutTime * 0.5f;

        if (blackoutCooldown <= 0f && stress >= blackoutFrom)
        {
            blackoutTimer = blackoutTime;
            blackoutCooldown = Random.Range(1.5f, 3.5f);
        }

        color.postExposure.value = blackoutTimer > 0f ? -12f : 0f;
    }

    void OnDisable()
    {
        SwayMultiplier = 1f;

        foreach (var filter in ambience)
            if (filter != null) filter.cutoffFrequency = cutoffOpen;

        if (color != null)
        {
            color.postExposure.value = 0f;
            color.colorFilter.value = Color.white;
        }
    }
}

static class Synth
{
    const int Rate = 44100;

    public static AudioClip Heartbeat()
    {
        int len = Rate;
        var data = new float[len];

        Thump(data, 0, 0.16f, 1f);
        Thump(data, (int)(Rate * 0.24f), 0.20f, 0.7f);

        var clip = AudioClip.Create("Heartbeat", len, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static void Thump(float[] data, int start, float duration, float gain)
    {
        int count = (int)(Rate * duration);

        for (int i = 0; i < count && start + i < data.Length; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * 22f);
            float body = Mathf.Sin(2f * Mathf.PI * 68f * t) * 0.8f
                       + Mathf.Sin(2f * Mathf.PI * 136f * t) * 0.2f;

            data[start + i] += body * env * gain;
        }
    }

    public static AudioClip Tone(float hz, float seconds)
    {
        int len = (int)(Rate * seconds);
        var data = new float[len];

        int cycles = Mathf.Max(1, Mathf.RoundToInt(len * hz / Rate));
        float tuned = cycles * (float)Rate / len;

        for (int i = 0; i < len; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * tuned * i / Rate) * 0.25f;

        var clip = AudioClip.Create("Tone", len, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
