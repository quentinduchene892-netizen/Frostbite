using UnityEngine;

[DisallowMultipleComponent]
public class TreeWind : MonoBehaviour
{
    [SerializeField] Transform[] swayers;
    [SerializeField] WindStorm storm;
    [SerializeField] float range = 28f;
    [SerializeField] float idleTilt = 0.6f;
    [SerializeField] float gustTilt = 5f;
    [SerializeField] float speed = 1.3f;
    [SerializeField] float refresh = 0.4f;

    PlayerStat stat;
    Quaternion[] rest;
    int[] near;
    int nearCount;
    float left;
    float tilt;
    float wave;
    float phase;
    int i;
    int k;

    void Start()
    {
        rest = new Quaternion[swayers.Length];
        near = new int[swayers.Length];

        for (i = 0; i < swayers.Length; i++)
            if (swayers[i] != null) rest[i] = swayers[i].rotation;

        if (storm == null) storm = FindFirstObjectByType<WindStorm>();
    }

    void Update()
    {
        stat = PlayerStat.Instance;
        left -= Time.deltaTime;

        if (left <= 0f)
        {
            left = refresh;
            Gather();
        }

        tilt = idleTilt + (storm != null ? storm.Power : 0f) * gustTilt;

        for (k = 0; k < nearCount; k++)
        {
            if (swayers[near[k]] == null) continue;

            phase = swayers[near[k]].position.x * 0.35f + swayers[near[k]].position.z * 0.21f;
            wave = Mathf.Sin(Time.time * speed + phase);

            swayers[near[k]].rotation = rest[near[k]] * Quaternion.Euler(
                wave * tilt, 0f, Mathf.Cos(Time.time * speed * 0.8f + phase) * tilt * 0.55f);
        }
    }

    void Gather()
    {
        for (k = 0; k < nearCount; k++)
            if (swayers[near[k]] != null) swayers[near[k]].rotation = rest[near[k]];

        nearCount = 0;

        if (stat == null) return;

        for (i = 0; i < swayers.Length; i++)
        {
            if (swayers[i] == null) continue;
            if ((swayers[i].position - stat.transform.position).sqrMagnitude > range * range) continue;

            near[nearCount] = i;
            nearCount++;
        }
    }
}
