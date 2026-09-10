using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider stressBar;
    [SerializeField] private Slider coldBar;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private GameObject alertText;
    [SerializeField] private Image stressFill;
    [SerializeField] private Image coldFill;
    [SerializeField] private Color stressLow = new Color(0.35f, 0.11f, 0.10f, 1f);
    [SerializeField] private Color stressHigh = new Color(0.95f, 0.24f, 0.18f, 1f);
    [SerializeField] private Color coldLow = new Color(0.16f, 0.28f, 0.38f, 1f);
    [SerializeField] private Color coldHigh = new Color(0.78f, 0.94f, 1f, 1f);
    [SerializeField] private RectTransform stressGhost;
    [SerializeField] private RectTransform coldGhost;
    [SerializeField] private TMP_Text stressValue;
    [SerializeField] private TMP_Text coldValue;
    [Tooltip("Chevron indiquant la VITESSE du froid, pas son niveau : c'est elle qui change quand on s'abrite.")]
    [SerializeField] private TMP_Text coldTrend;
    [SerializeField] private TMP_Text hintText;
    [Tooltip("Avertissement affiche tant que la barre de froid est pleine et que la vie fond.")]
    [SerializeField] private TMP_Text frozenText;
    [SerializeField] private float hintDuration = 5f;
    [SerializeField] private float ghostSpeed = 0.3f;
    [SerializeField] private CampfireCraft craft;
    [SerializeField] private Image craftBar;
    [SerializeField] private TMP_Text woodValue;
    [SerializeField] private WoodGather gather;
    [SerializeField] private Image pickupBar;
    [SerializeField] private Image crosshair;
    [SerializeField] private Color aimIdle = new Color(1f, 1f, 1f, 0.32f);
    [SerializeField] private Color aimReady = new Color(1f, 0.70f, 0.34f, 0.90f);
    [SerializeField] private Image progressBarWolfTrap;
    [SerializeField] private float startProgress = 0.3f;
    [SerializeField] private float maxProgressBarWolfTrapProgress = 1f;
    [SerializeField] private float fillPerPressMin = 0.08f;
    [SerializeField] private float fillPerPressMax = 0.2f;
    [SerializeField] private float drainSpeed = 0.3f;

    [Header("Mash Ring (indicateur de spam)")]
    [Tooltip("RectTransform de l'anneau qui pulse pour indiquer qu'il faut spam la touche.")]
    [SerializeField] private RectTransform mashRing;
    [SerializeField] private Image mashRingImage;
    [SerializeField] private float mashPulseDuration = 0.5f;
    [SerializeField] private float mashPulseMaxScale = 1.6f;
    [SerializeField] private Color mashRingColor = new Color(1f, 0.8f, 0.2f, 1f);

    public bool isWolfTrapTriggered = false;
    public bool isWolfTrapActivated = false;

    private float stressGhostPart;
    private float coldGhostPart;
    private float currentProgress;
    private float stressPart;
    private float coldPart;
    private bool isRunning;
    private bool showBar;
    private float lastCold;
    private float coldSpeed;
    private bool coldReady;
    private WindStorm storm;
    private bool hintShown;
    private float hintLeft;
    private Color coldValueColor;
    private bool coldColorSaved;
    private bool showDeath;
    private Vector2 edge;
    private float mashPulseTimer;

    void Start()
    {
        if (craft == null) craft = FindFirstObjectByType<CampfireCraft>();
        if (gather == null) gather = FindFirstObjectByType<WoodGather>();
        if (storm == null) storm = FindFirstObjectByType<WindStorm>();

        if (hintText != null) hintText.gameObject.SetActive(false);

        if (progressBarWolfTrap == null)
        {
            Debug.LogError("progressBarWolfTrap n'est pas assignée dans l'inspecteur.", this);
            return;
        }

        if (maxProgressBarWolfTrapProgress <= 0f)
        {
            Debug.LogWarning("maxProgressBarWolfTrapProgress doit être supérieur à zéro.", this);
            maxProgressBarWolfTrapProgress = 1f;
        }

        progressBarWolfTrap.gameObject.SetActive(false);

        if (mashRing != null) mashRing.gameObject.SetActive(false);

        if (alertText != null) alertText.SetActive(false);
    }

    void Update()
    {
        if (alertText != null && alertText.activeSelf != isRunning) alertText.SetActive(isRunning);

        UpdateStatBars();
        UpdateCraft();
        UpdateDeathScreen();
        UpdateShelterHint();

        if (PlayerStat.Instance != null && (PlayerStat.Instance.Fainted || PlayerStat.Instance.Dead))
        {
            StopWolfTrapQTE();
            return;
        }

        if (progressBarWolfTrap == null) return;

        if (isWolfTrapTriggered && !isRunning)
        {
            isWolfTrapTriggered = false;
            StartWolfTrapQTE();
        }

        if (!isRunning)
        {
            UpdateMashRing();
            return;
        }

        currentProgress -= drainSpeed * Time.deltaTime;

        if (InputManager.Instance != null && InputManager.Instance.WolfTrapPressed)
            currentProgress += Random.Range(fillPerPressMin, fillPerPressMax);

        currentProgress = Mathf.Clamp(currentProgress, 0f, maxProgressBarWolfTrapProgress);

        UpdateProgressBar();
        UpdateMashRing();

        if (currentProgress < maxProgressBarWolfTrapProgress) return;

        isWolfTrapActivated = true;
        isRunning = false;
        progressBarWolfTrap.gameObject.SetActive(false);
        OnWolfTrapActivated();
    }

    public void StartWolfTrapQTE()
    {
        currentProgress = startProgress;
        isRunning = true;
        isWolfTrapActivated = false;
        progressBarWolfTrap.gameObject.SetActive(true);
        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (progressBarWolfTrap == null || maxProgressBarWolfTrapProgress <= 0f) return;

        progressBarWolfTrap.fillAmount = currentProgress / maxProgressBarWolfTrapProgress;
    }

    private void UpdateMashRing()
    {
        if (mashRing == null) return;

        bool shouldShow = isRunning;

        if (mashRing.gameObject.activeSelf != shouldShow)
            mashRing.gameObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            mashPulseTimer = 0f;
            return;
        }

        mashPulseTimer += Time.deltaTime;

        // Boucle sur la durée d'un pulse
        float t = (mashPulseTimer % mashPulseDuration) / mashPulseDuration;

        float scale = Mathf.Lerp(1f, mashPulseMaxScale, t);
        mashRing.localScale = new Vector3(scale, scale, 1f);

        if (mashRingImage != null)
        {
            Color c = mashRingColor;
            c.a = Mathf.Lerp(mashRingColor.a, 0f, t); // fade out en grandissant
            mashRingImage.color = c;
        }
    }

    private void UpdateStatBars()
    {
        if (PlayerStat.Instance == null) return;

        stressPart = PlayerStat.Instance.StressPart;
        coldPart = PlayerStat.Instance.ColdPart;

        if (stressBar != null) stressBar.value = stressPart;
        if (coldBar != null) coldBar.value = coldPart;

        if (stressFill != null) stressFill.color = Color.Lerp(stressLow, stressHigh, stressPart);
        if (coldFill != null) coldFill.color = Color.Lerp(coldLow, coldHigh, coldPart);

        UpdateFrozenWarning();

        stressGhostPart = TrailTowards(stressGhostPart, stressPart);
        coldGhostPart = TrailTowards(coldGhostPart, coldPart);

        SetGhost(stressGhost, stressGhostPart);
        SetGhost(coldGhost, coldGhostPart);

        if (stressValue != null) stressValue.text = Mathf.RoundToInt(stressPart * 100f).ToString();
        if (coldValue != null) coldValue.text = Mathf.RoundToInt(coldPart * 100f).ToString();

        UpdateColdTrend(PlayerStat.Instance.Cold);
    }

    private void UpdateFrozenWarning()
    {
        if (!coldColorSaved && coldValue != null) { coldValueColor = coldValue.color; coldColorSaved = true; }

        bool frozen = PlayerStat.Instance != null && PlayerStat.Instance.Frozen && !PlayerStat.Instance.Dead;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);

        if (frozen)
        {
            Color hot = Color.Lerp(new Color(0.88f, 0.13f, 0.10f), Color.white, pulse);
            if (coldFill != null) coldFill.color = hot;
            if (coldValue != null) coldValue.color = hot;
        }
        else if (coldValue != null && coldColorSaved)
        {
            coldValue.color = coldValueColor;
        }

        if (frozenText == null) return;

        if (frozenText.gameObject.activeSelf != frozen) frozenText.gameObject.SetActive(frozen);
        if (!frozen) return;

        frozenText.text = "VOUS GELEZ";
        Color c = frozenText.color;
        c.a = 0.55f + 0.45f * pulse;
        frozenText.color = c;
    }

    private void UpdateColdTrend(float cold)
    {
        if (coldTrend == null) return;

        if (!coldReady) { lastCold = cold; coldReady = true; }

        float instant = (cold - lastCold) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastCold = cold;
        coldSpeed = Mathf.Lerp(coldSpeed, instant, 1f - Mathf.Exp(-6f * Time.deltaTime));

        if (coldSpeed > 5f)
        {
            coldTrend.text = "▲▲▲";
            coldTrend.color = new Color(1f, 0.22f, 0.18f);
        }
        else if (coldSpeed > 1f)
        {
            coldTrend.text = "▲▲";
            coldTrend.color = new Color(0.95f, 0.45f, 0.30f);
        }
        else if (coldSpeed > 0.15f)
        {
            coldTrend.text = "▲";
            coldTrend.color = new Color(0.95f, 0.72f, 0.35f);
        }
        else if (coldSpeed < -0.15f)
        {
            coldTrend.text = "▼";
            coldTrend.color = new Color(0.55f, 0.82f, 1f);
        }
        else
        {
            coldTrend.text = "–";
            coldTrend.color = new Color(0.75f, 0.78f, 0.82f);
        }
    }

    private void UpdateShelterHint()
    {
        if (hintText == null) return;

        if (!hintShown && storm != null && storm.Power > 0.6f && storm.Exposure > 0.8f)
        {
            hintShown = true;
            hintLeft = hintDuration;
            hintText.text = "Le vent te transperce. Abrite-toi sous les arbres.";
        }

        if (hintLeft <= 0f)
        {
            if (hintText.gameObject.activeSelf) hintText.gameObject.SetActive(false);
            return;
        }

        if (!hintText.gameObject.activeSelf) hintText.gameObject.SetActive(true);

        hintLeft -= Time.deltaTime;
        Color c = hintText.color;
        c.a = Mathf.Clamp01(hintLeft);
        hintText.color = c;
    }

    private void UpdateCraft()
    {
        if (woodValue != null && Inventory.Instance != null)
            woodValue.text = Inventory.Instance.Wood.ToString();

        if (crosshair != null)
            crosshair.color = craft != null && craft.Ready ? aimReady : aimIdle;

        Fill(craftBar, craft == null ? 0f : craft.Progress);
        Fill(pickupBar, gather == null ? 0f : gather.Progress);
    }

    private void UpdateDeathScreen()
    {
        if (deathScreen == null || PlayerStat.Instance == null) return;

        showDeath = PlayerStat.Instance.Fainted || PlayerStat.Instance.Dead;

        if (deathScreen.activeSelf != showDeath) deathScreen.SetActive(showDeath);

        if (showDeath && deathText != null) deathText.text = PlayerStat.Instance.Cause;
    }

    private void StopWolfTrapQTE()
    {
        isRunning = false;
        isWolfTrapTriggered = false;

        if (progressBarWolfTrap != null && progressBarWolfTrap.gameObject.activeSelf)
            progressBarWolfTrap.gameObject.SetActive(false);

        if (mashRing != null && mashRing.gameObject.activeSelf)
            mashRing.gameObject.SetActive(false);

        mashPulseTimer = 0f;
    }

    private float TrailTowards(float ghost, float target)
    {
        if (target >= ghost) return target;

        return Mathf.MoveTowards(ghost, target, ghostSpeed * Time.deltaTime);
    }

    private void SetGhost(RectTransform ghost, float part)
    {
        if (ghost == null) return;

        edge = ghost.anchorMax;
        edge.x = part;
        ghost.anchorMax = edge;
        ghost.offsetMin = Vector2.zero;
        ghost.offsetMax = Vector2.zero;
    }

    private void Fill(Image bar, float part)
    {
        if (bar == null) return;

        showBar = part > 0.001f;

        if (bar.gameObject.activeSelf != showBar) bar.gameObject.SetActive(showBar);

        bar.fillAmount = part;
    }

    private void OnWolfTrapActivated()
    {
        Debug.Log("Wolf Trap activé.");
    }
}