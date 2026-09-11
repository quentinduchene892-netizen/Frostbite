using UnityEngine;

[DisallowMultipleComponent]
public class Bullet : MonoBehaviour
{
    [SerializeField] GameObject impactPrefab;
    [SerializeField] string playerTag = "Player";
    [SerializeField] float speed = 220f;
    [SerializeField] float lifeTime = 5f;
    [SerializeField] float whizRange = 3.5f;
    [SerializeField] float whizKick = 0.6f;
    [SerializeField] float hitKick = 1f;

    CameraPlayer target;
    RaycastHit hit;
    Vector3 direction = Vector3.forward;
    float step;
    bool whizzed;

    public void Init(Vector3 shootDirection)
    {
        direction = shootDirection.normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        Whiz();

        step = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, direction, out hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
            Impact();
            return;
        }

        transform.position += direction * step;
    }

    void Whiz()
    {
        if (whizzed || PlayerStat.Instance == null) return;
        if (Vector3.Distance(transform.position, PlayerStat.Instance.transform.position) > whizRange) return;

        whizzed = true;
        Kick(whizKick);
    }

    void Impact()
    {
        if (impactPrefab != null)
            Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

        if (hit.collider.CompareTag(playerTag))
        {
            Kick(hitKick);

            if (PlayerStat.Instance != null) PlayerStat.Instance.Gunshot();
        }

        Destroy(gameObject);
    }

    void Kick(float amount)
    {
        if (PlayerStat.Instance == null) return;
        if (target == null) target = PlayerStat.Instance.GetComponent<CameraPlayer>();
        if (target != null) target.Kick(amount);

        PlayerStat.Instance.AddStress(10);
    }
}
