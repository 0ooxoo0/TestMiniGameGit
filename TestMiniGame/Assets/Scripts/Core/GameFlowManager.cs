using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks; // Не забудьте подключить UniTask
using System;

public static class GameFlowManager
{
    public enum GameScene
    {
        MainMenu,
        Clicker,
        TriPeaks
    }

    private static bool _isLoadingScene = false;

    /// <summary>
    /// Асинхронно загружает сцену с указанием типа (MainMenu, Clicker, TriPeaks)
    /// </summary>
    public static async UniTask LoadScene(GameScene scene)
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;

        // Можно добавить любую логику по показу Loading UI и т.п.

        // Допустим, названия сцен совпадают с enum
        // (либо сделайте словарь map: GameScene -> string SceneName)
        string sceneName = scene.ToString();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            await UniTask.Yield();
        }

        _isLoadingScene = false;
    }
}
