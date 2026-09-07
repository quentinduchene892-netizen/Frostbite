using UnityEngine;

public class Hunter : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint; // optionnel : point de sortie du tir (ex: bout du fusil). Si vide, utilise la position du Hunter.

    [Header("Réglages")]
    [Tooltip("Si coché, ce Hunter tire tout seul 5s après le Start (indépendamment d'un HunterManager). Décoche si un HunterManager gère les tirs.")]
    [SerializeField] private bool autoFireOnStart = false;
    [SerializeField] private float delayBeforeFirstShot = 5f;

    void Start()
    {
        if (autoFireOnStart)
            Invoke(nameof(Shoot), delayBeforeFirstShot);
    }

    void Update()
    {

    }

    /// <summary>
    /// Fait tirer ce Hunter immédiatement. Peut être appelée par un HunterManager
    /// ou par n'importe quel autre script.
    /// </summary>
    public void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Hunter : aucun bulletPrefab assigné.", this);
            return;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Init(spawnPoint.forward);
        }
    }
}