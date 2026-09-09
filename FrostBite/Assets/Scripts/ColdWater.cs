using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ColdWater : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] float entryCold = 0f;
    [Tooltip("S'ajoute aux 6/s de l'etat trempe : 10/s dans l'eau, donc mort en 10 s.")]
    [SerializeField] float coldRate = 4f;
    [SerializeField] float stressRate = 9f;
    [Tooltip("Vitesse du joueur dans l'eau, en fraction de sa vitesse normale.")]
    [Range(0.1f, 1f)] [SerializeField] float wadeSlow = 0.4f;

    [Header("Sous l'eau")]
    [Tooltip("Voile plein ecran affiche quand la camera passe sous la surface.")]
    [SerializeField] Image underwaterOverlay;
    [SerializeField] Color underwaterTint = new Color(0.12f, 0.32f, 0.45f, 0.80f);
    [SerializeField] float underwaterFade = 8f;
    [Tooltip("Volume general sous l'eau : les sons deviennent sourds.")]
    [Range(0.05f, 1f)] [SerializeField] float muffle = 0.3f;

    PlayerStat stat;
    Collider[] boxes;
    Camera cam;
    float submerged;

    // Une riviere sinueuse est decoupee en plusieurs boites : on compte les
    // chevauchements pour ne declencher le choc thermique qu'a la vraie entree.
    int overlaps;

    public bool Inside => overlaps > 0;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        overlaps++;

        if (overlaps > 1) return;

        if (PlayerStat.Instance != null)
        {
            PlayerStat.Instance.AddCold(entryCold);
            PlayerStat.Instance.SetWaterSlow(wadeSlow);
            PlayerStat.Instance.Soak();
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlayFootstep(other.transform.position, false);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        overlaps = Mathf.Max(0, overlaps - 1);

        if (overlaps == 0 && PlayerStat.Instance != null)
            PlayerStat.Instance.SetWaterSlow(1f);
    }

    void OnDisable()
    {
        overlaps = 0;
        submerged = 0f;
        AudioListener.volume = 1f;

        if (underwaterOverlay != null) underwaterOverlay.gameObject.SetActive(false);
        if (PlayerStat.Instance != null) PlayerStat.Instance.SetWaterSlow(1f);
    }

    void Awake()
    {
        boxes = GetComponentsInChildren<Collider>(true);
    }

    // La camera peut etre sous la surface alors que le corps l'est deja :
    // c'est elle qui decide de l'affichage, pas le personnage.
    bool CameraSubmerged()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || boxes == null) return false;

        Vector3 p = cam.transform.position;
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] == null) continue;
            if ((boxes[i].ClosestPoint(p) - p).sqrMagnitude < 0.0001f) return true;
        }
        return false;
    }

    void UpdateUnderwater()
    {
        float target = overlaps > 0 && CameraSubmerged() ? 1f : 0f;
        if (Mathf.Approximately(submerged, target) && submerged <= 0f) return;

        submerged = Mathf.MoveTowards(submerged, target, Time.deltaTime * underwaterFade);

        if (underwaterOverlay != null)
        {
            Color c = underwaterTint;
            c.a = underwaterTint.a * submerged;
            underwaterOverlay.color = c;
            if (underwaterOverlay.gameObject.activeSelf != submerged > 0.01f)
                underwaterOverlay.gameObject.SetActive(submerged > 0.01f);
        }

        AudioListener.volume = Mathf.Lerp(1f, muffle, submerged);
    }

    void Update()
    {
        UpdateUnderwater();

        if (overlaps <= 0) return;

        stat = PlayerStat.Instance;

        if (stat == null || stat.Dead) return;

        stat.Soak();
        stat.AddCold(coldRate * Time.deltaTime);
        stat.AddStress(stressRate * Time.deltaTime);
    }
}
