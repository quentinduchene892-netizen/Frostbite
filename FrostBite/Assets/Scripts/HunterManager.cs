using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quand le joueur entre dans le collider (Trigger) de ce GameObject,
/// un Hunter aléatoire de la liste tire toutes les 5 à 8 secondes.
/// Le cycle s'arrête si le joueur ressort de la zone.
/// </summary>
public class HunterManager : MonoBehaviour
{
    [Header("Hunters")]
    [SerializeField] private List<Hunter> hunters = new List<Hunter>();

    [Header("Options")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 8f;

    private Coroutine shootingRoutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (shootingRoutine != null) return; // déjà en cours

        shootingRoutine = StartCoroutine(ShootingLoop());
    }

    private void OnTriggerExit(Collider other)
    {
        if (this == null) return; // sécurité : cet objet a déjà été détruit
        if (!other.CompareTag(playerTag)) return;
        if (shootingRoutine == null) return;

        StopCoroutine(shootingRoutine);
        shootingRoutine = null;
    }

    private void OnDisable()
    {
        // Coupe proprement la coroutine si l'objet est désactivé ou détruit
        if (shootingRoutine != null)
        {
            StopCoroutine(shootingRoutine);
            shootingRoutine = null;
        }
    }

    private IEnumerator ShootingLoop()
    {
        while (true)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            FireRandomHunter();
        }
    }

    private void FireRandomHunter()
    {
        if (this == null) return; // sécurité : cet objet a déjà été détruit

        if (hunters == null || hunters.Count == 0)
        {
            Debug.LogWarning("HunterManager : aucune liste de hunters assignée.", this);
            return;
        }

        // On filtre les entrées vides ou détruites au cas où
        List<Hunter> validHunters = hunters.FindAll(h => h != null);
        if (validHunters.Count == 0) return;

        Hunter chosen = validHunters[Random.Range(0, validHunters.Count)];
        if (chosen != null) chosen.Shoot();
    }
}