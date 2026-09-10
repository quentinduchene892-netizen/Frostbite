using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IntroCinematic : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Texture image;
        [TextArea(2, 4)] public string caption;
        [Tooltip("Duree d'affichage une fois le fondu d'entree termine.")]
        public float hold = 4.5f;
    }

    [SerializeField] string nextScene = "LD";
    [SerializeField] Slide[] slides;

    [Header("Vues")]
    [SerializeField] RawImage layerA;
    [SerializeField] RawImage layerB;
    [SerializeField] TextMeshProUGUI caption;
    [SerializeField] CanvasGroup captionGroup;
    [SerializeField] CanvasGroup blackout;
    [SerializeField] CanvasGroup hint;
    [SerializeField] AudioSource ambience;

    [Header("Rythme")]
    [SerializeField] float openFade = 1.5f;
    [SerializeField] float crossFade = 1.1f;
    [SerializeField] float closeFade = 1.8f;
    [Tooltip("Zoom lent pendant qu'une image est a l'ecran. Sans lui, une image fixe a l'air d'un bug.")]
    [SerializeField] float drift = 0.05f;
    [Tooltip("Duree sur laquelle le zoom se deroule entierement.")]
    [SerializeField] float driftSpan = 8f;
    [SerializeField] float ambienceVolume = 0.4f;

    RawImage front;
    RawImage back;
    float ageA;
    float ageB;
    bool advance;
    bool leaving;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        front = layerA;
        back = layerB;

        if (blackout != null) blackout.alpha = 1f;
        if (captionGroup != null) captionGroup.alpha = 0f;
        if (hint != null) hint.alpha = 0f;
        if (layerA != null) layerA.color = Color.white;
        if (layerB != null) layerB.color = new Color(1f, 1f, 1f, 0f);
    }

    void Start()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogError("IntroCinematic : aucune image a jouer, on passe directement au jeu.", this);
            Load();
            return;
        }

        StartCoroutine(Play());
    }

    void Update()
    {
        if (leaving) return;

        if (SkipPressed())
        {
            StopAllCoroutines();
            StartCoroutine(Leave());
            return;
        }

        if (NextPressed()) advance = true;
    }

    void LateUpdate()
    {
        ageA += Time.unscaledDeltaTime;
        ageB += Time.unscaledDeltaTime;

        Drift(layerA, ageA);
        Drift(layerB, ageB);
    }

    IEnumerator Play()
    {
        Show(front, slides[0]);
        front.color = Color.white;
        SetCaption(slides[0].caption);

        if (ambience != null)
        {
            ambience.loop = true;
            ambience.volume = ambienceVolume;
            ambience.Play();
        }

        yield return Dim(blackout, 1f, 0f, openFade);
        yield return Dim(captionGroup, 0f, 1f, 0.7f);

        if (hint != null) StartCoroutine(Dim(hint, 0f, 0.55f, 0.7f));

        for (int i = 0; i < slides.Length; i++)
        {
            yield return Hold(slides[i].hold);

            if (i == slides.Length - 1) break;

            yield return Cross(slides[i + 1]);
        }

        yield return Leave();
    }

    IEnumerator Hold(float seconds)
    {
        advance = false;

        for (float t = 0f; t < seconds && !advance; t += Time.unscaledDeltaTime)
            yield return null;

        advance = false;
    }

    // L'image suivante monte par-dessus l'ancienne : jamais de noir entre deux plans.
    IEnumerator Cross(Slide slide)
    {
        Show(back, slide);
        back.color = new Color(1f, 1f, 1f, 0f);
        back.transform.SetAsLastSibling();

        float span = Mathf.Max(crossFade, 0.01f);
        bool swapped = false;

        for (float t = 0f; t < span; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / span);

            back.color = new Color(1f, 1f, 1f, k);

            if (captionGroup != null)
                captionGroup.alpha = k < 0.5f ? 1f - k * 2f : (k - 0.5f) * 2f;

            if (!swapped && k >= 0.5f)
            {
                SetCaption(slide.caption);
                swapped = true;
            }

            yield return null;
        }

        if (!swapped) SetCaption(slide.caption);

        back.color = Color.white;

        if (captionGroup != null) captionGroup.alpha = 1f;

        RawImage spent = front;
        front = back;
        back = spent;
    }

    IEnumerator Leave()
    {
        leaving = true;

        float span = Mathf.Max(closeFade, 0.01f);
        float startVolume = ambience != null ? ambience.volume : 0f;
        float startCaption = captionGroup != null ? captionGroup.alpha : 0f;
        float startHint = hint != null ? hint.alpha : 0f;
        float startBlack = blackout != null ? blackout.alpha : 0f;

        for (float t = 0f; t < span; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / span);

            if (blackout != null) blackout.alpha = Mathf.Lerp(startBlack, 1f, k);
            if (captionGroup != null) captionGroup.alpha = startCaption * (1f - k);
            if (hint != null) hint.alpha = startHint * (1f - k);
            if (ambience != null) ambience.volume = startVolume * (1f - k);

            yield return null;
        }

        Load();
    }

    IEnumerator Dim(CanvasGroup group, float from, float to, float span)
    {
        if (group == null) yield break;

        span = Mathf.Max(span, 0.01f);
        group.alpha = from;

        for (float t = 0f; t < span; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / span));
            yield return null;
        }

        group.alpha = to;
    }

    void Show(RawImage layer, Slide slide)
    {
        if (layer == null) return;

        layer.texture = slide.image;

        if (layer == layerA) ageA = 0f;
        else ageB = 0f;
    }

    void Drift(RawImage layer, float age)
    {
        if (layer == null) return;

        float scale = 1f + drift * Mathf.Clamp01(age / Mathf.Max(driftSpan, 0.01f));
        layer.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    void SetCaption(string text)
    {
        if (caption != null) caption.text = text;
    }

    void Load()
    {
        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogError("IntroCinematic : aucune scene de suite renseignee.", this);
            return;
        }

        SceneManager.LoadScene(nextScene);
    }

    static bool NextPressed()
    {
        Keyboard keys = Keyboard.current;

        if (keys != null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;

        return false;
    }

    static bool SkipPressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) return true;

        return false;
    }
}
