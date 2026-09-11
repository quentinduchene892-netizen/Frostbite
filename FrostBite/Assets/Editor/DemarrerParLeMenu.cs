using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class DemarrerParLeMenu
{
    const string Cle = "Frostbite.DemarrerParLeMenu";
    const string Menu = "Frostbite/Toujours demarrer par le menu";
    const string Scene = "Assets/Scenes/MenuScene.unity";

    static DemarrerParLeMenu()
    {
        EditorApplication.delayCall += Appliquer;
    }

    static bool Actif
    {
        get => EditorPrefs.GetBool(Cle, true);
        set => EditorPrefs.SetBool(Cle, value);
    }

    [MenuItem(Menu)]
    static void Basculer()
    {
        Actif = !Actif;
        Appliquer();
    }

    [MenuItem(Menu, true)]
    static bool Cocher()
    {
        UnityEditor.Menu.SetChecked(Menu, Actif);
        return true;
    }

    static void Appliquer()
    {
        if (!Actif)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene);

        if (menu == null)
        {
            UnityEngine.Debug.LogWarning("DemarrerParLeMenu : " + Scene + " introuvable.");
            return;
        }

        EditorSceneManager.playModeStartScene = menu;
    }
}
