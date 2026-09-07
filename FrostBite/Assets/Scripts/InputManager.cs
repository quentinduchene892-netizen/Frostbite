using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    [SerializeField] InputActionAsset actions;
    [SerializeField] string mapName = "Player";
    [SerializeField] string moveName = "Move";
    [SerializeField] string lookName = "Look";
    [SerializeField] string runName = "Sprint";

    InputActionMap map;
    InputAction moveAction;
    InputAction lookAction;
    InputAction runAction;

    public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Look => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
    public bool Run => runAction != null && runAction.IsPressed();
    public bool MouseLook => lookAction != null && lookAction.activeControl?.device is Pointer;

    void Awake()
    {
        Instance = this;

        if (actions == null)
        {
            Debug.LogError("InputManager : aucun InputActionAsset assigne.", this);
            return;
        }

        map = actions.FindActionMap(mapName, false);

        if (map == null)
        {
            Debug.LogError($"InputManager : action map '{mapName}' introuvable.", this);
            return;
        }

        moveAction = map.FindAction(moveName, false);
        lookAction = map.FindAction(lookName, false);
        runAction = map.FindAction(runName, false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (map != null) map.Enable();
    }

    void OnDisable()
    {
        if (map != null) map.Disable();
    }
}
