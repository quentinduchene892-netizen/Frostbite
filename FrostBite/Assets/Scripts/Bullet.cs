using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Réglages")]
    [SerializeField] private float speed = 40f;
    [SerializeField] private float lifeTime = 5f; // se détruit toute seule après ce délai (évite les balles perdues)

    private Vector3 direction = Vector3.forward;

    /// <summary>
    /// Appelée par le Hunter juste après l'instanciation pour définir la direction de tir.
    /// Oriente aussi visuellement la balle dans cette direction.
    /// </summary>
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
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Adapte selon ce que la balle doit faire à l'impact (dégâts, effet, etc.)
        Debug.Log("Bullet a touché : " + other.name);
        Destroy(gameObject);
    }
}