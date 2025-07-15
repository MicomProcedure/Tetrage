using UnityEngine;
using Cysharp.Threading.Tasks;
using MackySoft.Navigathena.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    private GameObject loadingPanelInstance;

    private async UniTask ShowLoadingPanelAsync()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();
    }

    private void HideAndDestroyLoadingPanel()
    {
        LoadingScreenController.Instance?.Hide();
        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    private async UniTask UnloadAllOtherScenes(string exceptScene)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != exceptScene && scene.isLoaded)
            {
                await SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    public async void GoToTitle()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("TitleScene"));
        await sceneTask;

        var newScene = SceneManager.GetActiveScene().name;
        await UnloadAllOtherScenes(newScene);

        LoadingScreenController.Instance?.Hide();
        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    public async void GoToGame()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("GameScene"));
        await sceneTask;

        var newScene = SceneManager.GetActiveScene().name;
        await UnloadAllOtherScenes(newScene);

        LoadingScreenController.Instance?.Hide();
        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    public async void GoToResult()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("ResultScene"));
        await sceneTask;

        var newScene = SceneManager.GetActiveScene().name;
        await UnloadAllOtherScenes(newScene);

        LoadingScreenController.Instance?.Hide();
        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    public async void GoBack()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneTask = GlobalSceneNavigator.Instance.Pop();
        await sceneTask;

        var newScene = SceneManager.GetActiveScene().name;
        await UnloadAllOtherScenes(newScene);

        LoadingScreenController.Instance?.Hide();
        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }
}
