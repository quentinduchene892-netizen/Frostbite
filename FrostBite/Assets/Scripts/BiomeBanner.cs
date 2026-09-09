using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Affiche le nom de la region traversee, a la maniere d'une entree de sanctuaire :
// apparition douce, maintien, disparition.
[DisallowMultipleComponent]
public class BiomeBanner : MonoBehaviour
{
    public static BiomeBanner Instance { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Rythme")]
    [SerializeField] private float fadeIn = 0.8f;
    [SerializeField] private float hold = 2.6f;
    [SerializeField] private float fadeOut = 1.2f;

    [Header("Mouvement")]
    [Tooltip("Montee du texte pendant l'apparition, en pixels.")]
    [SerializeField] private float rise = 26f;

    private readonly List<Biome> inside = new List<Biome>();
    private Biome current;
    private Coroutine routine;
    private RectTransform rt;
    private Vector2 home;

    private void Awake()
    {
        Instance = this;

        if (group == null) group = GetComponent<CanvasGroup>();
        rt = transform as RectTransform;
        if (rt != null) home = rt.anchoredPosition;
        if (group != null) group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Enter(Biome b)
    {
        if (b == null) return;
        if (!inside.Contains(b)) inside.Add(b);
        Refresh();
    }

    public void Exit(Biome b)
    {
        inside.Remove(b);
        Refresh();
    }

    // La region affichee est toujours la plus prioritaire parmi celles ou l'on se trouve.
    private void Refresh()
    {
        Biome best = null;
        for (int i = 0; i < inside.Count; i++)
        {
            if (inside[i] == null) continue;
            if (best == null || inside[i].Priority > best.Priority) best = inside[i];
        }

        if (best == current) return;

        current = best;
        if (current == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Play(current.Title, current.Subtitle));
    }

    private IEnumerator Play(string title, string subtitle)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeIn);
            float e = u * u * (3f - 2f * u);
            if (group != null) group.alpha = e;
            if (rt != null) rt.anchoredPosition = home + Vector2.up * Mathf.Lerp(-rise, 0f, e);
            yield return null;
        }

        if (group != null) group.alpha = 1f;
        if (rt != null) rt.anchoredPosition = home;

        yield return new WaitForSecondsRealtime(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeOut);
            if (group != null) group.alpha = 1f - u * u * (3f - 2f * u);
            yield return null;
        }

        if (group != null) group.alpha = 0f;
        routine = null;
    }
}
