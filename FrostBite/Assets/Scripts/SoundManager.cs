using UnityEngine;

[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] AudioSource ambientSource;
    [SerializeField] AudioSource breathSource;
    [SerializeField] AudioClip forestAmbiance;
    [SerializeField] AudioClip breathing;
    [SerializeField] AudioClip hunterShoot;
    [Tooltip("Sifflement d'une balle qui passe pres du joueur. A defaut, le son de tir est reutilise.")]
    [SerializeField] AudioClip bulletWhiz;
    [SerializeField][Range(0f, 1f)] float whizVolume = 0.75f;
    [SerializeField] AudioClip pickup;
    [SerializeField] AudioClip craft;
    [SerializeField] AudioClip wolfTrapSnap;
    [SerializeField] AudioClip wolfTrapRelease;
    [SerializeField] AudioClip[] snowSteps;
    [SerializeField] AudioClip[] woodSteps;
    [SerializeField] AudioClip[] iceSteps;
    [SerializeField] float sfxVolume = 1f;
    [SerializeField] float stepVolume = 0.6f;
    [SerializeField] float shotMinDistance = 20f;
    [SerializeField] float shotMaxDistance = 160f;

    [Header("Hurlement de loup")]
    [SerializeField] AudioClip wolfHowl;
    [SerializeField] float wolfHowlMinInterval = 10f;
    [SerializeField] float wolfHowlMaxInterval = 40f;
    [SerializeField] float wolfHowlVolume = 1f;
    [SerializeField] float wolfMinDistance = 20f;
    [SerializeField] float wolfMaxDistance = 160f;

    int pick;
    bool breathingOn;
    GameObject shotObject;
    AudioSource shotSource;
    float wolfHowlTimer;

    void Awake()
    {
        Instance = this;

        ResetWolfHowlTimer();

        if (ambientSource == null || forestAmbiance == null) return;

        ambientSource.clip = forestAmbiance;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    void Update()
    {
        if (wolfHowl == null) return;

        wolfHowlTimer -= Time.deltaTime;

        if (wolfHowlTimer <= 0f)
        {
            PlayWolfHowl();
            ResetWolfHowlTimer();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayHunterShot(Vector3 position)
    {
        PlayFar(hunterShoot, position, sfxVolume, shotMinDistance, shotMaxDistance);
    }

    public void PlayWhiz(Vector3 position)
    {
        PlayAt(bulletWhiz != null ? bulletWhiz : hunterShoot, position, whizVolume);
    }

    public void PlayPickup(Vector3 position)
    {
        PlayAt(pickup, position, sfxVolume);
    }

    public void PlayCraft(Vector3 position)
    {
        PlayAt(craft, position, sfxVolume);
    }

    public void PlayWolfTrapSnap(Vector3 position)
    {
        PlayAt(wolfTrapSnap, position, sfxVolume);
    }

    public void PlayWolfTrapRelease(Vector3 position)
    {
        PlayAt(wolfTrapRelease, position, sfxVolume);
    }

    public void PlayFootstep(Vector3 position, bool onWood, bool onIce = false)
    {
        AudioClip[] clips = onWood ? woodSteps : (onIce ? iceSteps : snowSteps);

        PlayAt(Pick(clips), position, stepVolume);
    }

    public void PlayWolfHowl()
    {
        Vector3 position = Camera.main != null ? Camera.main.transform.position : transform.position;
        PlayFar(wolfHowl, position, wolfHowlVolume, wolfMinDistance, wolfMaxDistance);
    }

    void ResetWolfHowlTimer()
    {
        wolfHowlTimer = Random.Range(wolfHowlMinInterval, wolfHowlMaxInterval);
    }

    public void SetBreathing(bool active)
    {
        if (active == breathingOn || breathSource == null || breathing == null) return;

        breathingOn = active;

        if (active)
        {
            breathSource.clip = breathing;
            breathSource.loop = true;
            breathSource.Play();
        }
        else
        {
            breathSource.Stop();
        }
    }

    AudioClip Pick(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;

        pick = Random.Range(0, clips.Length);

        return clips[pick];
    }

    void PlayAt(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    void PlayFar(AudioClip clip, Vector3 position, float volume, float minDistance, float maxDistance)
    {
        if (clip == null) return;

        shotObject = new GameObject("ShotAudio");
        shotObject.transform.position = position;

        shotSource = shotObject.AddComponent<AudioSource>();
        shotSource.clip = clip;
        shotSource.volume = volume;
        shotSource.spatialBlend = 1f;
        shotSource.rolloffMode = AudioRolloffMode.Linear;
        shotSource.minDistance = minDistance;
        shotSource.maxDistance = maxDistance;
        shotSource.Play();

        Destroy(shotObject, clip.length);
    }
}