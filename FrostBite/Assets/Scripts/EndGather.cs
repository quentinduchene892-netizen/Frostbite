using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class EndGather : MonoBehaviour
{
    public static EndGather Instance;

    [SerializeField] Camera view;
    [Tooltip("Portee du regard. Au-dela, le point de fin n'est plus visable meme si on le voit.")]
    [SerializeField] float range = 3f;
    [Tooltip("Epaisseur du rayon. Un rayon fin obligerait a centrer le viseur au pixel pres.")]
    [SerializeField] float aimRadius = 0.35f;
    [SerializeField] float nose = 0.35f;
    [SerializeField] LayerMask mask = ~0;
    [SerializeField] float gatherTime = 0.9f;
    [SerializeField] float dropSpeed = 2.5f;

    [Header("Fin de jeu")]
    [Tooltip("Si renseigne, cette scene est chargee quand la jauge est pleine.")]
    [SerializeField] string endSceneName;
    [Tooltip("Delai en secondes entre la jauge pleine et le chargement de la scene (laisse le temps a un fade ou un ecran de fin de jouer).")]
    [SerializeField] float sceneDelay = 2f;
    [Tooltip("Appele quand la jauge est pleine, avant le delai et le chargement eventuel de la scene. Branche ici un ecran de fin, un fade, etc.")]
    [SerializeField] UnityEvent onGameEnd;

    InputManager input;
    PlayerStat stat;
    EndTrigger target;
    RaycastHit[] buffer = new RaycastHit[16];
    Vector3 origin;
    float progress;
    bool holding;
    bool triggered;

    public float Progress => progress;
    public bool Aimed => target != null;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (triggered) return;

        Aim();

        if (target == null)
        {
            progress = 0f;
            return;
        }

        input = InputManager.Instance;
        stat = PlayerStat.Instance;

        holding = input != null && input.Interact
            && (stat == null || (!stat.Fainted && !stat.Dead));

        if (holding)
            progress += Time.deltaTime / Mathf.Max(gatherTime, 0.01f);
        else
            progress -= dropSpeed * Time.deltaTime;

        progress = Mathf.Clamp01(progress);

        if (progress >= 1f) Take();
    }

    void Aim()
    {
        if (view == null) view = Camera.main;

        EndTrigger found = null;

        if (view != null)
        {
            origin = view.transform.position + view.transform.forward * nose;

            int count = Physics.SphereCastNonAlloc(origin, aimRadius, view.transform.forward, buffer, range, mask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                EndTrigger point = buffer[i].collider.GetComponentInParent<EndTrigger>();

                if (point == null || buffer[i].distance >= best) continue;

                best = buffer[i].distance;
                found = point;
            }
        }

        if (found == target) return;

        target = found;
        progress = 0f;
    }

    void Take()
    {
        triggered = true;
        progress = 1f;
        target = null;

        onGameEnd?.Invoke();

        if (!string.IsNullOrEmpty(endSceneName))
            StartCoroutine(LoadSceneAfterDelay());
    }

    IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(sceneDelay);

        SceneManager.LoadScene(endSceneName);
    }
}