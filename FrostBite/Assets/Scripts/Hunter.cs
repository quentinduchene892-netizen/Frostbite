using UnityEngine;

public class Hunter : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject muzzlePrefab;
    [SerializeField] private bool autoFireOnStart = false;
    [SerializeField] private float delayBeforeFirstShot = 5f;

    [Header("Visee")]
    [Tooltip("Vise le joueur au lieu de tirer droit devant.")]
    [SerializeField] private bool aimAtPlayer = true;
    [Tooltip("Distance a laquelle la balle passe du joueur, en metres. Independant de la distance de tir : le tireur est aussi juste a 500 m qu'a 50 m.")]
    [SerializeField] private Vector2 missDistance = new Vector2(1.2f, 4f);
    [Tooltip("Part des tirs reellement ajustes sur le joueur. Une touche est mortelle sur le coup : a laisser a 0 tant que ce n'est pas voulu.")]
    [Range(0f, 1f)] [SerializeField] private float lethalShotChance = 0f;
    [Tooltip("La balle apparait a cette distance du joueur, sur la ligne de tir. Le son et l'eclair de bouche restent chez le tireur : on entend d'ou ca vient, mais la balle arrive vite et la visee reste juste. 0 pour partir du canon.")]
    [SerializeField] private float bulletSpawnDistance = 80f;

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

        Vector3 dir = AimDirection(spawnPoint);
        Vector3 origin = spawnPoint.position;

        // A 150 m/s, une balle tiree de 300 m met 2 s a arriver : le joueur a le temps
        // de marcher 6 m, bien plus que l'ecart de visee. On la fait donc apparaitre
        // pres de lui, sur la meme ligne de tir, pour qu'elle arrive en une demi-seconde.
        if (bulletSpawnDistance > 0f && PlayerStat.Instance != null)
        {
            float toPlayer = Vector3.Distance(origin, PlayerStat.Instance.transform.position);
            if (toPlayer > bulletSpawnDistance)
                origin += dir * (toPlayer - bulletSpawnDistance);
        }

        shot = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(dir, Vector3.up));
        bullet = shot.GetComponent<Bullet>();

        if (bullet != null) bullet.Init(dir);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayHunterShot(spawnPoint.position);
    }

    private Vector3 AimDirection(Transform from)
    {
        if (!aimAtPlayer || PlayerStat.Instance == null) return from.forward;

        Vector3 target = PlayerStat.Instance.transform.position + Vector3.up * 1.1f;
        Vector3 toPlayer = target - from.position;
        if (toPlayer.sqrMagnitude < 0.01f) return from.forward;

        if (Random.value >= lethalShotChance)
        {
            // Le tireur vise volontairement a cote, a quelques metres du joueur :
            // la balle le frole a coup sur, donc sifflement et secousse camera,
            // sans que la mort dependre d'un tirage a chaque coup de feu.
            Vector3 dir = toPlayer.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 up = Vector3.Cross(dir, right);

            float angle = Random.value * Mathf.PI * 2f;
            float r = Random.Range(missDistance.x, missDistance.y);
            target += (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * r;
        }

        return (target - from.position).normalized;
    }
}
