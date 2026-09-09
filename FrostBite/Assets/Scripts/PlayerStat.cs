using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] float coldStress = 1f;
    [SerializeField] float faintTime = 4f;
    [SerializeField] float freezeHit = 8f;
    [Tooltip("Degats d'une balle. A 100, une touche tue sur le coup.")]
    [SerializeField] float gunshotDamage = 25f;
    [SerializeField] float gunshotStress = 20f;
    [Header("Trempe")]
    [Tooltip("Froid par seconde tant qu'on est mouille, meme hors de l'eau.")]
    [SerializeField] float wetColdRate = 6f;
    [Tooltip("Secondes au feu pour secher completement.")]
    [SerializeField] float dryTime = 5f;
    [SerializeField] float deathDelay = 2f;

    float deathLeft;
    float waterSlow = 1f;
    float wetness;
    float mult;
    float over;
    float slow;
    bool fainted;
    bool frozen;
    bool shot;
    bool fell;
    bool dead;
    bool reloading;

    public float Life => life;
    public float Stress => stress;
    public float Cold => cold;
    public float LifePart => life / maxLife;
    public float StressPart => stress / maxStress;
    public float ColdPart => cold / maxCold;
    public float Slow => slow;
    public bool Fainted => fainted;
    public bool Frozen => frozen;
    public bool Shot => shot;
    public bool Dead => dead;
    public bool Wet => wetness > 0.01f;
    public float Wetness => wetness;

    public string Cause => shot ? "Vous avez été tué par balle."
        : fell ? "Vous êtes tombé dans le gouffre."
        : fainted && frozen ? "Vous vous êtes évanoui et vous êtes mort de froid."
        : fainted ? "Vous vous êtes évanoui."
        : frozen ? "Vous êtes mort de froid."
        : "Vous êtes mort.";

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
        if (dead)
        {
            if (reloading) return;

            deathLeft -= Time.deltaTime;

            if (deathLeft > 0f) return;

            reloading = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (!fainted && stress >= maxStress) fainted = true;

        cold = Mathf.Clamp(cold + (coldRate + wetColdRate * wetness) * Time.deltaTime, 0f, maxCold);
        over = Mathf.InverseLerp(coldPoint, maxCold, cold);
        stress = Mathf.Clamp(stress + (coldStress * over - calmRate) * Time.deltaTime, 0f, maxStress);

        if (cold >= maxCold)
        {
            frozen = true;
            Hurt(freezeHit * Time.deltaTime);
        }

        if (fainted) Hurt(maxLife / Mathf.Max(faintTime, 0.01f) * Time.deltaTime);

        if (dead) return;

        slow = fainted ? 0f : Mathf.Lerp(1f, slowFloor, over) * waterSlow;
    }

    public void Soak()
    {
        wetness = 1f;
    }

    public void Dry(float seconds)
    {
        if (wetness <= 0f) return;
        wetness = Mathf.Clamp01(wetness - seconds / Mathf.Max(dryTime, 0.01f));
    }

    public void SetWaterSlow(float value)
    {
        waterSlow = Mathf.Clamp01(value);
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

    public void Gunshot()
    {
        if (dead) return;

        AddStress(gunshotStress);
        Hurt(gunshotDamage);

        if (life <= 0f) shot = true;
    }

    public void Fall()
    {
        if (dead) return;

        fell = true;
        Hurt(maxLife);
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
        waterSlow = 1f;
        wetness = 0f;
        fainted = false;
        frozen = false;
        shot = false;
        fell = false;
        dead = false;
        reloading = false;
        deathLeft = 0f;
    }

    void Die()
    {
        dead = true;
        slow = 0f;
        deathLeft = deathDelay;
    }
}
