using UnityEngine;

public class WolfTrap : MonoBehaviour
{
    [SerializeField] private HUD hud;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float stressOnTrap = 20f;

    private GameObject trappedPlayer;
    private CameraPlayer view;
    private CharacterController controller;
    private Vector3 spot;
    private bool playerTrapped;
    private bool hadController;

    private void OnTriggerEnter(Collider other)
    {
        if (playerTrapped) return;
        if (!other.CompareTag(playerTag)) return;

        TrapPlayer(other.gameObject);
    }

    private void Update()
    {
        if (!playerTrapped || hud == null || !hud.isWolfTrapActivated) return;

        ReleasePlayer();
    }

    private void TrapPlayer(GameObject player)
    {
        playerTrapped = true;
        trappedPlayer = player;

        if (PlayerStat.Instance != null) PlayerStat.Instance.AddStress(stressOnTrap);

        view = player.GetComponent<CameraPlayer>();
        if (view != null) view.SetTrapped(true);

        CenterPlayerOnTrap(player);

        if (hud != null) hud.isWolfTrapTriggered = true;
    }

    private void CenterPlayerOnTrap(GameObject player)
    {
        controller = player.GetComponent<CharacterController>();
        hadController = controller != null && controller.enabled;

        if (hadController) controller.enabled = false;

        spot = player.transform.position;
        spot.x = transform.position.x;
        spot.z = transform.position.z;
        player.transform.position = spot;

        if (hadController) controller.enabled = true;
    }

    private void ReleasePlayer()
    {
        view = trappedPlayer != null ? trappedPlayer.GetComponent<CameraPlayer>() : null;
        if (view != null) view.SetTrapped(false);

        playerTrapped = false;
        trappedPlayer = null;

        if (hud != null) hud.isWolfTrapActivated = false;
    }
}
