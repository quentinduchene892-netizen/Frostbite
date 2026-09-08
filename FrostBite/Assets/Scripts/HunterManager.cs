using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterManager : MonoBehaviour
{
    [SerializeField] private List<Hunter> hunters = new List<Hunter>();
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 8f;

    private Coroutine shootingRoutine;
    private List<Hunter> ready = new List<Hunter>();
    private Hunter chosen;
    private float delay;
    private int i;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (shootingRoutine != null) return;

        shootingRoutine = StartCoroutine(ShootingLoop());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (shootingRoutine == null) return;

        StopCoroutine(shootingRoutine);
        shootingRoutine = null;
    }

    private void OnDisable()
    {
        if (shootingRoutine == null) return;

        StopCoroutine(shootingRoutine);
        shootingRoutine = null;
    }

    private IEnumerator ShootingLoop()
    {
        while (true)
        {
            delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            FireRandomHunter();
        }
    }

    private void FireRandomHunter()
    {
        if (hunters == null || hunters.Count == 0)
        {
            Debug.LogWarning("HunterManager : aucun chasseur dans la liste.", this);
            return;
        }

        ready.Clear();

        for (i = 0; i < hunters.Count; i++)
            if (hunters[i] != null) ready.Add(hunters[i]);

        if (ready.Count == 0) return;

        chosen = ready[Random.Range(0, ready.Count)];
        chosen.Shoot();
    }
}
