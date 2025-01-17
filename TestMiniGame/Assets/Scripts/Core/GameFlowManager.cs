using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks; // �� �������� ���������� UniTask
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
    /// ���������� ��������� ����� � ��������� ���� (MainMenu, Clicker, TriPeaks)
    /// </summary>
    public static async UniTask LoadScene(GameScene scene)
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;

        // ����� �������� ����� ������ �� ������ Loading UI � �.�.

        // ��������, �������� ���� ��������� � enum
        // (���� �������� ������� map: GameScene -> string SceneName)
        string sceneName = scene.ToString();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            await UniTask.Yield();
        }

        _isLoadingScene = false;
    }
}
