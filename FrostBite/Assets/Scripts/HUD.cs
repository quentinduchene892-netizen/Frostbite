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

    [Header("Wolf Trap Progress Bar")]
    [SerializeField] private Image progressBarWolfTrap;
    [SerializeField] private float startProgress = 0.3f; // valeur de départ quand le QTE se déclenche
    [SerializeField] private float maxProgressBarWolfTrapProgress = 1f; // Valeur par défaut
    [SerializeField] private float fillPerPressMin = 0.08f; // gain minimum ajouté à chaque appui sur E
    [SerializeField] private float fillPerPressMax = 0.2f;  // gain maximum ajouté à chaque appui sur E
    [SerializeField] private float drainSpeed = 0.3f;    // vitesse de descente automatique (continue)

    [Header("Déclencheur")]
    [Tooltip("Passe à true (par un autre script, un event, un trigger...) pour lancer le QTE de la jauge")]
    public bool isWolfTrapTriggered = false;

    private float stressGhostPart;
    private float coldGhostPart;
    private bool isRunning = false; // le QTE est en cours d'exécution
    private float currentProgress = 0f;
    public bool isWolfTrapActivated = false;

    void Start()
    {
        if (progressBarWolfTrap == null)
        {
            Debug.LogError("progressBarWolfTrap n'est pas assignée dans l'Inspector !");
            return;
        }

        if (maxProgressBarWolfTrapProgress <= 0)
        {
            Debug.LogWarning("maxProgressBarWolfTrapProgress doit être > 0");
            maxProgressBarWolfTrapProgress = 1f;
        }

        // La barre reste cachée tant que le QTE n'a pas été déclenché
        progressBarWolfTrap.gameObject.SetActive(false);

        if (alertText != null) alertText.SetActive(false);
    }

    void Update()
    {
        if (alertText != null && alertText.activeSelf != isRunning) alertText.SetActive(isRunning);

        UpdateStatBars();
        UpdateDeathScreen();

        if (PlayerStat.Instance != null && (PlayerStat.Instance.Fainted || PlayerStat.Instance.Dead))
        {
            StopWolfTrapQTE();
            return;
        }

        if (progressBarWolfTrap == null)
            return;

        // On surveille le déclencheur (bool assigné depuis l'extérieur, ou event plus tard)
        // Dès qu'il est lu, on le "consomme" (remis à false) pour pouvoir le redéclencher plus tard
        if (isWolfTrapTriggered && !isRunning)
        {
            isWolfTrapTriggered = false;
            StartWolfTrapQTE();
        }

        if (!isRunning)
            return;

        // La jauge descend en permanence, comme dans un QTE classique
        currentProgress -= drainSpeed * Time.deltaTime;

        // Chaque appui (et non maintien) sur l'action WolfTrap ajoute un coup de boost aléatoire
        if (InputManager.Instance != null && InputManager.Instance.WolfTrapPressed)
        {
            currentProgress += Random.Range(fillPerPressMin, fillPerPressMax);
        }

        currentProgress = Mathf.Clamp(currentProgress, 0f, maxProgressBarWolfTrapProgress);

        UpdateProgressBar();

        // Vérifier si la jauge est complète
        if (currentProgress >= maxProgressBarWolfTrapProgress)
        {
            isWolfTrapActivated = true;
            isRunning = false;
            progressBarWolfTrap.gameObject.SetActive(false);
            OnWolfTrapActivated();
        }
    }

    /// <summary>
    /// Lance le QTE : affiche la jauge et la fait démarrer à startProgress.
    /// Appelée automatiquement quand isWolfTrapTriggered passe à true,
    /// mais peut aussi être appelée directement par un autre script/event.
    /// </summary>
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
        if (progressBarWolfTrap != null && maxProgressBarWolfTrapProgress > 0)
        {
            progressBarWolfTrap.fillAmount = currentProgress / maxProgressBarWolfTrapProgress;
        }
    }

    private void UpdateDeathScreen()
    {
        if (deathScreen == null || PlayerStat.Instance == null) return;

        bool show = PlayerStat.Instance.Fainted || PlayerStat.Instance.Dead;

        if (deathScreen.activeSelf != show) deathScreen.SetActive(show);

        if (show && deathText != null) deathText.text = PlayerStat.Instance.Cause;
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

        Vector2 max = ghost.anchorMax;
        max.x = part;
        ghost.anchorMax = max;
        ghost.offsetMin = Vector2.zero;
        ghost.offsetMax = Vector2.zero;
    }

    private void UpdateStatBars()
    {
        if (PlayerStat.Instance == null) return;

        float stressPart = PlayerStat.Instance.StressPart;
        float coldPart = PlayerStat.Instance.ColdPart;

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

    private void OnWolfTrapActivated()
    {
        Debug.Log("Wolf Trap Activated!");
    }
}