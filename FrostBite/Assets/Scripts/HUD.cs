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

    public bool isWolfTrapTriggered = false;
    public bool isWolfTrapActivated = false;

    private float stressGhostPart;
    private float coldGhostPart;
    private float currentProgress;
    private float stressPart;
    private float coldPart;
    private bool isRunning;
    private bool showBar;
    private bool showDeath;
    private Vector2 edge;

    void Start()
    {
        if (craft == null) craft = FindFirstObjectByType<CampfireCraft>();
        if (gather == null) gather = FindFirstObjectByType<WoodGather>();

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

        if (alertText != null) alertText.SetActive(false);
    }

    void Update()
    {
        if (alertText != null && alertText.activeSelf != isRunning) alertText.SetActive(isRunning);

        UpdateStatBars();
        UpdateCraft();
        UpdateDeathScreen();

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

        if (!isRunning) return;

        currentProgress -= drainSpeed * Time.deltaTime;

        if (InputManager.Instance != null && InputManager.Instance.WolfTrapPressed)
            currentProgress += Random.Range(fillPerPressMin, fillPerPressMax);

        currentProgress = Mathf.Clamp(currentProgress, 0f, maxProgressBarWolfTrapProgress);

        UpdateProgressBar();

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

    private void UpdateStatBars()
    {
        if (PlayerStat.Instance == null) return;

        stressPart = PlayerStat.Instance.StressPart;
        coldPart = PlayerStat.Instance.ColdPart;

        if (stressBar != null) stressBar.value = stressPart;
        if (coldBar != null) coldBar.value = coldPart;

        if (stressFill != null) stressFill.color = Color.Lerp(stressLow, stressHigh, stressPart);
        if (coldFill != null) coldFill.color = Color.Lerp(coldLow, coldHigh, coldPart);

        stressGhostPart = TrailTowards(stressGhostPart, stressPart);
        coldGhostPart = TrailTowards(coldGhostPart, coldPart);

        SetGhost(stressGhost, stressGhostPart);
        SetGhost(coldGhost, coldGhostPart);

        if (stressValue != null) stressValue.text = Mathf.RoundToInt(stressPart * 100f).ToString();
        if (coldValue != null) coldValue.text = Mathf.RoundToInt(coldPart * 100f).ToString();
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
