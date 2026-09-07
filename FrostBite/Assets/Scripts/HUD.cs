using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
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
    }

    void Update()
    {
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

        // Chaque appui (et non maintien) sur E ajoute un coup de boost aléatoire
        if (Input.GetKeyDown(KeyCode.E))
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

    private void OnWolfTrapActivated()
    {
        Debug.Log("Wolf Trap Activated!");
    }
}