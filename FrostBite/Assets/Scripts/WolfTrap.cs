using UnityEngine;

public class WolfTrap : MonoBehaviour
{
    [SerializeField] private HUD hud;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float stressOnTrap = 20f;

    private static HUD shared;

    private HUD Screen
    {
        get
        {
            if (hud != null) return hud;
            if (shared == null) shared = FindAnyObjectByType<HUD>();

            return shared;
        }
    }

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

    private void Awake()
    {
        if (Screen == null)
            Debug.LogError("WolfTrap : aucun HUD dans la scene, le joueur pris ne pourra pas se degager.", this);
    }

    private void Update()
    {
        if (!playerTrapped) return;

        HUD screen = Screen;

        if (screen == null || !screen.isWolfTrapActivated) return;

        ReleasePlayer();
    }

    private void TrapPlayer(GameObject player)
    {
        playerTrapped = true;
        trappedPlayer = player;

        if (PlayerStat.Instance != null) PlayerStat.Instance.AddStress(stressOnTrap);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWolfTrapSnap(transform.position);

        view = player.GetComponent<CameraPlayer>();
        if (view != null) view.SetTrapped(true);

        CenterPlayerOnTrap(player);

        if (Screen != null) Screen.isWolfTrapTriggered = true;
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
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWolfTrapRelease(transform.position);

        playerTrapped = false;
        trappedPlayer = null;

        if (Screen != null) Screen.isWolfTrapActivated = false;
    }
}
