using UnityEngine;

/// <summary>
/// Piège à loup réutilisable : quand le joueur entre dans le collider (Trigger),
/// il est bloqué et doit spammer E (via la jauge du HUD) pour se libérer.
/// Une fois libéré, le piège se réarme automatiquement pour un futur passage.
/// </summary>
public class WolfTrap : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private HUD hud; // glisse l'objet qui porte le script HUD

    [Header("Options")]
    [Tooltip("Tag utilisé pour identifier le joueur")]
    [SerializeField] private string playerTag = "Player";

    private bool playerTrapped = false;
    private GameObject trappedPlayer;

    // Optionnel : référence à un script de mouvement du joueur pour le bloquer
    // Adapte le nom si ton script de contrôle du joueur s'appelle autrement.
    // [SerializeField] private PlayerMovement playerMovement;

    private void OnTriggerEnter(Collider other)
    {
        if (playerTrapped) return; // déjà occupé, on ignore
        if (!other.CompareTag(playerTag)) return;

        TrapPlayer(other.gameObject);
    }

    private void TrapPlayer(GameObject player)
    {
        playerTrapped = true;
        trappedPlayer = player;

        var cameraPlayer = player.GetComponent<CameraPlayer>();
        if (cameraPlayer != null) cameraPlayer.SetTrapped(true);

        CenterPlayerOnTrap(player);

        Debug.Log("Joueur piégé dans le Wolf Trap !");

        if (hud != null)
        {
            hud.isWolfTrapTriggered = true;
        }
    }

    private void CenterPlayerOnTrap(GameObject player)
    {
        // Le CharacterController réécrase transform.position à chaque frame
        // en fonction de la physique, donc on doit le désactiver le temps du déplacement
        var controller = player.GetComponent<CharacterController>();
        bool hadController = controller != null && controller.enabled;

        if (hadController) controller.enabled = false;

        Vector3 targetPos = player.transform.position;
        targetPos.x = transform.position.x;
        targetPos.z = transform.position.z;
        // On garde le Y du joueur (pas de téléportation verticale, il reste au sol où il est)
        player.transform.position = targetPos;

        if (hadController) controller.enabled = true;
    }

    private void Update()
    {
        // Tant que le joueur est piégé, on attend que la jauge du HUD soit remplie
        if (playerTrapped && hud != null && hud.isWolfTrapActivated)
        {
            ReleasePlayer();
        }
    }

    private void ReleasePlayer()
    {
        Debug.Log("Joueur libéré du Wolf Trap !");

        var cameraPlayer = trappedPlayer != null ? trappedPlayer.GetComponent<CameraPlayer>() : null;
        if (cameraPlayer != null) cameraPlayer.SetTrapped(false);

        playerTrapped = false;
        trappedPlayer = null;

        // On réarme le piège : on remet isWolfTrapActivated à false côté HUD
        // pour permettre un futur déclenchement propre
        if (hud != null)
        {
            hud.isWolfTrapActivated = false;
        }
    }
}