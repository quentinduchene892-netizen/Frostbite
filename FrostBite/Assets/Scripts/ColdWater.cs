using UnityEngine;

[DisallowMultipleComponent]
public class ColdWater : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] float entryCold = 12f;
    [SerializeField] float coldRate = 22f;
    [SerializeField] float stressRate = 9f;

    PlayerStat stat;

    // Une riviere sinueuse est decoupee en plusieurs boites : on compte les
    // chevauchements pour ne declencher le choc thermique qu'a la vraie entree.
    int overlaps;

    public bool Inside => overlaps > 0;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        overlaps++;

        if (overlaps > 1) return;

        if (PlayerStat.Instance != null) PlayerStat.Instance.AddCold(entryCold);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayFootstep(other.transform.position, false);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        overlaps = Mathf.Max(0, overlaps - 1);
    }

    void Update()
    {
        if (overlaps <= 0) return;

        stat = PlayerStat.Instance;

        if (stat == null || stat.Dead) return;

        stat.AddCold(coldRate * Time.deltaTime);
        stat.AddStress(stressRate * Time.deltaTime);
    }
}
