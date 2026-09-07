using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStat : MonoBehaviour
{
    public static PlayerStat Instance;

    [SerializeField] float maxLife = 100f;
    [SerializeField] float maxStress = 100f;
    [SerializeField] float maxCold = 100f;
    [SerializeField] float life = 100f;
    [SerializeField] float stress;
    [SerializeField] float cold;
    [SerializeField] float calmRate = 0.2f;
    [SerializeField] float coldRate = 0.35f;
    [SerializeField] float coldPoint = 50f;
    [SerializeField] float slowFloor = 0.55f;
    [SerializeField] float coldRatio = 1.5f;
    [SerializeField] float faintTime = 4f;
    [SerializeField] float freezeHit = 8f;

    float mult;
    float over;
    float slow;
    bool fainted;
    bool dead;

    public float Life => life;
    public float Stress => stress;
    public float Cold => cold;
    public float LifePart => life / maxLife;
    public float StressPart => stress / maxStress;
    public float ColdPart => cold / maxCold;
    public float Slow => slow;
    public bool Fainted => fainted;
    public bool Dead => dead;

    void Awake()
    {
        Instance = this;
        life = maxLife;
        slow = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (dead) return;

        cold = Mathf.Clamp(cold + coldRate * Time.deltaTime, 0f, maxCold);
        stress = Mathf.Clamp(stress - calmRate * Time.deltaTime, 0f, maxStress);

        if (cold >= maxCold) Hurt(freezeHit * Time.deltaTime);

        if (!fainted && stress >= maxStress) fainted = true;

        if (fainted) Hurt(maxLife / Mathf.Max(faintTime, 0.01f) * Time.deltaTime);

        if (dead) return;

        over = Mathf.InverseLerp(coldPoint, maxCold, cold);
        slow = fainted ? 0f : Mathf.Lerp(1f, slowFloor, over);
    }

    public void AddStress(float amount)
    {
        if (dead) return;

        over = Mathf.InverseLerp(coldPoint, maxCold, cold);
        mult = 1f + over * coldRatio;
        stress = Mathf.Clamp(stress + amount * mult, 0f, maxStress);
    }

    public void Calm(float amount)
    {
        stress = Mathf.Clamp(stress - amount, 0f, maxStress);
    }

    public void AddCold(float amount)
    {
        if (dead) return;

        cold = Mathf.Clamp(cold + amount, 0f, maxCold);
    }

    public void Warm(float amount)
    {
        cold = Mathf.Clamp(cold - amount, 0f, maxCold);
    }

    public void Fire(float warm, float calm)
    {
        Warm(warm);
        Calm(calm);
    }

    public void Hurt(float amount)
    {
        if (dead) return;

        life = Mathf.Clamp(life - amount, 0f, maxLife);

        if (life <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (dead) return;

        life = Mathf.Clamp(life + amount, 0f, maxLife);
    }

    public void Restart()
    {
        life = maxLife;
        stress = 0f;
        cold = 0f;
        slow = 1f;
        fainted = false;
        dead = false;
    }

    void Die()
    {
        dead = true;
        slow = 0f;
    }
}
