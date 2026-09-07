using UnityEngine;

public class WolfTrap : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private HUD hud; 

    [Header("Options")]
    [Tooltip("Tag utilisé pour identifier le joueur")]
    [SerializeField] private string playerTag = "Player";

    private bool playerTrapped = false;
    private GameObject trappedPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (playerTrapped) return; 
        if (!other.CompareTag(playerTag)) return;

        TrapPlayer(other.gameObject);
    }

    private void TrapPlayer(GameObject player)
    {
        playerTrapped = true;
        trappedPlayer = player;

        Debug.Log("Joueur piégé dans le Wolf Trap !");

        if (hud != null)
        {
            hud.isWolfTrapTriggered = true;
        }
    }

    private void Update()
    {
        if (playerTrapped && hud != null && hud.isWolfTrapActivated)
        {
            ReleasePlayer();
        }
    }

    private void ReleasePlayer()
    {
        Debug.Log("Joueur libéré du Wolf Trap !");


        playerTrapped = false;
        trappedPlayer = null;

        if (hud != null)
        {
            hud.isWolfTrapActivated = false;
        }
    }
}