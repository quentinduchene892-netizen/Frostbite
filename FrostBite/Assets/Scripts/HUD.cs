using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider stressBar;
    [SerializeField] private Slider coldBar;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMP_Text deathText;
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
    [SerializeField] private TMP_Text woodValue;
    [SerializeField] private WoodGather gather;
    [SerializeField] private Image crosshair;

    [Header("Anneau d'interaction")]
    [Tooltip("Un seul anneau pose sur le viseur pour les deux gestes : il annonce la cible, puis se remplit au maintien.")]
    [SerializeField] private CanvasGroup interactRing;
    [SerializeField] private Image interactTrack;
    [SerializeField] private Image interactFill;
    [Tooltip("La touche a tenir, posee au centre de l'anneau.")]
    [SerializeField] private TMP_Text interactKey;
    [Tooltip("Ce que le geste coute ou rapporte, pose sous l'anneau. Vide pour le piege, qui ne coute rien.")]
    [SerializeField] private TMP_Text interactCount;
    [SerializeField] private Color ringWood = new Color(0.72f, 0.86f, 0.62f, 1f);
    [SerializeField] private Color ringFire = new Color(1f, 0.62f, 0.28f, 1f);
    [Tooltip("Teinte du remplissage quand il reflue : sans elle, un maintien interrompu ne se voit pas.")]
    [SerializeField] private Color ringDrop = new Color(0.62f, 0.36f, 0.33f, 1f);
    [Tooltip("Teinte quand on vise le foyer sans assez de bois.")]
    [SerializeField] private Color ringShort = new Color(0.88f, 0.28f, 0.20f, 1f);
    [Tooltip("Teinte du QTE du piege.")]
    [SerializeField] private Color ringTrap = new Color(1f, 0.80f, 0.20f, 1f);
    [Tooltip("Teinte du point de fin de jeu.")]
    [SerializeField] private Color ringEnd = new Color(0.70f, 0.55f, 0.95f, 1f);

    [Header("Gain de bois")]
    [Tooltip("Petit +N qui monte pres du compteur : sans lui le total saute en silence dans un coin.")]
    [SerializeField] private TMP_Text woodGain;
    [SerializeField] private float woodGainTime = 1.1f;
    [SerializeField] private float woodGainRise = 24f;
    [Header("Piege a loup")]
    [SerializeField] private float startProgress = 0.3f;
    [SerializeField] private float maxProgressBarWolfTrapProgress = 1f;
    [SerializeField] private float fillPerPressMin = 0.08f;
    [SerializeField] private float fillPerPressMax = 0.2f;
    [SerializeField] private float drainSpeed = 0.3f;

    [Header("Endurance")]
    [SerializeField] private CameraPlayer runner;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Image staminaFill;
    [SerializeField] private RectTransform staminaGhost;
    [SerializeField] private TMP_Text staminaLabel;
    [SerializeField] private TMP_Text staminaValue;
    [Tooltip("Repere du seuil a partir duquel la course redevient possible apres un essoufflement.")]
    [SerializeField] private RectTransform staminaRestMark;
    [SerializeField] private Color staminaFull = new Color(0.45f, 0.72f, 0.42f, 1f);
    [SerializeField] private Color staminaLow = new Color(0.85f, 0.55f, 0.18f, 1f);
    [SerializeField] private Color staminaSpent = new Color(0.88f, 0.22f, 0.16f, 1f);

    [Tooltip("Periode du battement de l'anneau pendant le QTE : c'est lui qui dit de marteler plutot que de maintenir.")]
    [SerializeField] private float mashPulseDuration = 0.5f;
    [SerializeField] private float mashPulseScale = 1.18f;

    [Header("Rappel des deplacements")]
    [Tooltip("Pastilles de touches affichees au spawn, dans le meme style que l'anneau d'interaction.")]
    [SerializeField] private CanvasGroup spawnHint;
    [Tooltip("Duree du rappel des deplacements au demarrage.")]
    [SerializeField] private float spawnPromptTime = 8f;
    [SerializeField] private float promptFade = 5f;

    public bool isWolfTrapTriggered = false;
    public bool isWolfTrapActivated = false;

    private float stressGhostPart;
    private float staminaGhostPart;
    private string staminaTitle;
    private Color staminaTint;
    private float coldGhostPart;
    private float currentProgress;
    private float stressPart;
    private float coldPart;
    private bool isRunning;
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
    private float spawnPromptLeft;
    private float lastFill;
    private Color crossTint;
    private int lastWood;
    private bool woodReady;
    private float woodGainLeft;
    private Vector2 woodGainHome;

    void Start()
    {
        if (craft == null) craft = FindFirstObjectByType<CampfireCraft>();
        if (gather == null) gather = FindFirstObjectByType<WoodGather>();
        if (storm == null) storm = FindFirstObjectByType<WindStorm>();
        if (runner == null) runner = FindFirstObjectByType<CameraPlayer>();

        if (staminaLabel != null)
        {
            staminaTitle = staminaLabel.text;
            staminaTint = staminaLabel.color;
        }

        PlaceRestMark();

        if (crosshair != null) crossTint = crosshair.color;

        if (woodGain != null)
        {
            woodGainHome = woodGain.rectTransform.anchoredPosition;
            woodGain.gameObject.SetActive(false);
        }

        if (hintText != null) hintText.gameObject.SetActive(false);

        if (maxProgressBarWolfTrapProgress <= 0f)
        {
            Debug.LogWarning("maxProgressBarWolfTrapProgress doit être supérieur à zéro.", this);
            maxProgressBarWolfTrapProgress = 1f;
        }

        spawnPromptLeft = spawnPromptTime;
    }

    void Update()
    {
        UpdateStatBars();
        UpdateStamina();
        UpdatePrompt();
        UpdateInteract();
        UpdateCraft();
        UpdateWoodGain();
        UpdateDeathScreen();
        UpdateShelterHint();

        if (PlayerStat.Instance != null && (PlayerStat.Instance.Fainted || PlayerStat.Instance.Dead))
        {
            StopWolfTrapQTE();
            return;
        }

        if (isWolfTrapTriggered && !isRunning)
        {
            isWolfTrapTriggered = false;
            StartWolfTrapQTE();
        }

        if (!isRunning) return;

        currentProgress -= drainSpeed * Time.deltaTime;

        if (InputManager.Instance != null && InputManager.Instance.WolfTrapPressed)
            currentProgress += Random.Range(fillPerPressMin, fillPerPressMax);

        currentProgress = Mathf.Clamp(currentProgress, 0f, maxProgressBarWolfTrapProgress);

        if (currentProgress < maxProgressBarWolfTrapProgress) return;

        isWolfTrapActivated = true;
        isRunning = false;
    }

    public void StartWolfTrapQTE()
    {
        currentProgress = startProgress;
        isRunning = true;
        isWolfTrapActivated = false;
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

    private void PlaceRestMark()
    {
        if (staminaRestMark == null || runner == null) return;

        Vector2 min = staminaRestMark.anchorMin;
        Vector2 max = staminaRestMark.anchorMax;
        min.x = runner.RestPart;
        max.x = runner.RestPart;
        staminaRestMark.anchorMin = min;
        staminaRestMark.anchorMax = max;
        staminaRestMark.anchoredPosition = Vector2.zero;
    }

    private void UpdateStamina()
    {
        if (runner == null) return;

        float part = runner.StaminaPart;
        bool spent = runner.Tired;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);

        if (staminaBar != null) staminaBar.value = part;

        staminaGhostPart = TrailTowards(staminaGhostPart, part);
        SetGhost(staminaGhost, staminaGhostPart);

        if (staminaFill != null)
            staminaFill.color = spent
                ? Color.Lerp(staminaSpent, Color.white, pulse * 0.5f)
                : Color.Lerp(staminaLow, staminaFull, part);

        if (staminaValue != null) staminaValue.text = Mathf.RoundToInt(part * 100f).ToString();

        if (staminaLabel == null) return;

        if (spent)
        {
            Color hot = Color.Lerp(staminaSpent, Color.white, pulse);
            hot.a = 0.6f + 0.4f * pulse;
            staminaLabel.text = "À BOUT";
            staminaLabel.color = hot;
        }
        else
        {
            staminaLabel.text = staminaTitle;
            staminaLabel.color = staminaTint;
        }
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

    private void UpdatePrompt()
    {
        if (spawnHint == null) return;

        if (spawnPromptLeft > 0f)
        {
            if (InputManager.Instance != null && InputManager.Instance.Move.sqrMagnitude > 0.01f)
                spawnPromptLeft = 0f;
            else
                spawnPromptLeft -= Time.deltaTime;
        }

        bool show = spawnPromptLeft > 0f && !isRunning
            && (PlayerStat.Instance == null || (!PlayerStat.Instance.Fainted && !PlayerStat.Instance.Dead));

        spawnHint.alpha = Mathf.MoveTowards(spawnHint.alpha, show ? 1f : 0f, promptFade * Time.deltaTime);

        bool alive = spawnHint.alpha > 0.001f;

        if (spawnHint.gameObject.activeSelf != alive) spawnHint.gameObject.SetActive(alive);
    }

    private void UpdateCraft()
    {
        if (woodValue != null && Inventory.Instance != null)
            woodValue.text = Inventory.Instance.Wood.ToString();
    }

    private void UpdateInteract()
    {
        if (interactRing == null) return;

        bool trap = isRunning;
        bool wood = !trap && gather != null && gather.Aimed;
        bool fire = !trap && !wood && craft != null && craft.Aimed;
        bool end = !trap && !wood && !fire && EndGather.Instance != null && EndGather.Instance.Aimed;
        bool on = trap || wood || fire || end;

        interactRing.alpha = Mathf.MoveTowards(interactRing.alpha, on ? 1f : 0f, 9f * Time.deltaTime);

        if (crosshair != null)
        {
            Color c = crossTint;
            c.a = crossTint.a * (1f - interactRing.alpha);
            crosshair.color = c;
        }

        if (!on)
        {
            lastFill = 0f;
            mashPulseTimer = 0f;
            interactRing.transform.localScale = Vector3.one;
            return;
        }

        float part = trap
            ? currentProgress / Mathf.Max(maxProgressBarWolfTrapProgress, 0.0001f)
            : wood ? gather.Progress : fire ? craft.Progress : EndGather.Instance.Progress;

        bool lacking = fire && !craft.HasWood;
        Color tone = trap ? ringTrap : lacking ? ringShort : wood ? ringWood : fire ? ringFire : ringEnd;

        Beat(trap);

        if (interactKey != null)
        {
            interactKey.text = trap ? "F" : wood ? "E" : fire ? "A" : "E";
            interactKey.color = tone;
        }

        if (interactCount != null)
        {
            interactCount.text = trap ? "" : wood ? "+" + gather.Gain : fire ? craft.Cost + " bois" : "";
            interactCount.color = tone;
        }

        if (interactTrack != null)
        {
            Color track = tone;
            track.a = 0.30f;
            interactTrack.color = track;
        }

        if (interactFill != null)
        {
            interactFill.fillAmount = part;
            interactFill.color = part < lastFill ? ringDrop : tone;
        }

        lastFill = part;
    }

    private void Beat(bool trap)
    {
        if (!trap)
        {
            mashPulseTimer = 0f;
            interactRing.transform.localScale = Vector3.one;
            return;
        }

        mashPulseTimer += Time.deltaTime;

        float t = (mashPulseTimer % Mathf.Max(mashPulseDuration, 0.01f)) / Mathf.Max(mashPulseDuration, 0.01f);
        float scale = 1f + (mashPulseScale - 1f) * (1f - t) * (1f - t);

        interactRing.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdateWoodGain()
    {
        if (woodGain == null || Inventory.Instance == null) return;

        int wood = Inventory.Instance.Wood;

        if (!woodReady) { lastWood = wood; woodReady = true; }

        if (wood > lastWood)
        {
            woodGain.text = "+" + (wood - lastWood);
            woodGainLeft = woodGainTime;
        }

        lastWood = wood;

        bool show = woodGainLeft > 0f;

        if (woodGain.gameObject.activeSelf != show) woodGain.gameObject.SetActive(show);

        if (!show) return;

        woodGainLeft -= Time.deltaTime;

        float k = 1f - Mathf.Clamp01(woodGainLeft / Mathf.Max(woodGainTime, 0.01f));

        Color c = woodGain.color;
        c.a = 1f - k * k;
        woodGain.color = c;

        woodGain.rectTransform.anchoredPosition = woodGainHome + Vector2.up * (woodGainRise * k);
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
}