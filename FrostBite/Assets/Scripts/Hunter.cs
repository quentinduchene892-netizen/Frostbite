using UnityEngine;

public class Hunter : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject muzzlePrefab;
    [SerializeField] private bool autoFireOnStart = false;
    [SerializeField] private float delayBeforeFirstShot = 5f;

    private Transform spawnPoint;
    private GameObject shot;
    private Bullet bullet;

    void Start()
    {
        if (autoFireOnStart) Invoke(nameof(Shoot), delayBeforeFirstShot);
    }

    public void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Hunter : aucun bulletPrefab assigné.", this);
            return;
        }

        spawnPoint = firePoint != null ? firePoint : transform;

        if (muzzlePrefab != null)
            Instantiate(muzzlePrefab, spawnPoint.position, spawnPoint.rotation);

        shot = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        bullet = shot.GetComponent<Bullet>();

        if (bullet != null) bullet.Init(spawnPoint.forward);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayHunterShot(spawnPoint.position);
    }
}
