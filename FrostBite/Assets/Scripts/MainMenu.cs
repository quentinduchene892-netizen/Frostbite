using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameScene = "LD";
    [Tooltip("Texte clignotant. Le battement dit que l'ecran attend une entree, sans bouton a viser.")]
    [SerializeField] CanvasGroup prompt;
    [Tooltip("Voile noir du fondu de sortie.")]
    [SerializeField] CanvasGroup fade;
    [SerializeField] float blinkSpeed = 0.8f;
    [Range(0f, 1f)] [SerializeField] float blinkFloor = 0.25f;
    [SerializeField] float fadeTime = 0.9f;
    [Tooltip("Delai avant d'accepter une touche : evite de lancer la partie sur le clic qui vient de fermer le jeu precedent.")]
    [SerializeField] float armDelay = 0.4f;

    float age;
    bool leaving;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fade != null) fade.alpha = 0f;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;

        if (leaving)
        {
            Leave();
            return;
        }

        if (prompt != null)
            prompt.alpha = Mathf.Lerp(blinkFloor, 1f, 0.5f + 0.5f * Mathf.Sin(age * blinkSpeed * Mathf.PI * 2f));

        if (age >= armDelay && Pressed()) leaving = true;
    }

    void Leave()
    {
        if (fade == null)
        {
            Go();
            return;
        }

        fade.alpha += Time.unscaledDeltaTime / Mathf.Max(fadeTime, 0.01f);

        if (fade.alpha >= 1f) Go();
    }

    void Go()
    {
        enabled = false;

        if (string.IsNullOrEmpty(gameScene))
        {
            Debug.LogError("MainMenu : aucune scene de jeu renseignee.", this);
            return;
        }

        SceneManager.LoadScene(gameScene);
    }

    static bool Pressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;

        return false;
    }
}
