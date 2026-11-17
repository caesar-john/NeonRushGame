using UnityEngine;
using UnityEngine.SceneManagement;

// Makes the "Play" button of the main menu open the integrated map scene
// ("Game") which already contains the designer's city map.
public static class NeonCityMenuBootstrap
{
    private const string MenuScene = "MainMenu";
    private const string GameplayScene = "Game";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name != MenuScene) return;

        UIManager[] managers = Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            UIManager manager = managers[i];
            if (manager == null) continue;
            if (manager.gameObject.scene != scene) continue;
            manager.SetGameScene(GameplayScene);
        }
    }
}
