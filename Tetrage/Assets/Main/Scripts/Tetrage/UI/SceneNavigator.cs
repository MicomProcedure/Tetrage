using UnityEngine;
using Cysharp.Threading.Tasks;
using MackySoft.Navigathena.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    private GameObject loadingPanelInstance;

    // private async UniTask ShowLoadingPanelAsync()
    // {
    //     LoadingScreenController.Instance?.CreateLoadingCanvas();
    //     LoadingScreenController.Instance?.Show();
    // }

    // private void HideAndDestroyLoadingPanel()
    // {
    //     LoadingScreenController.Instance?.Hide();
    //     LoadingScreenController.Instance?.DestroyLoadingCanvas();
    // }

    public async void GoToTitle()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        // シーン遷移前に「今のシーン名」を記録
        var sceneToUnload = SceneManager.GetActiveScene().name;

        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("TitleScene"));
        await sceneTask;

        // 新しいシーンがアクティブになった後、「もともとあったシーン」だけをアンロード
        if (SceneManager.GetSceneByName(sceneToUnload).isLoaded)
        {
            await SceneManager.UnloadSceneAsync(sceneToUnload);
        }

        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    public async void GoToGame()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneToUnload = SceneManager.GetActiveScene().name;

        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("GameScene"));
        await sceneTask;

        if (SceneManager.GetSceneByName(sceneToUnload).isLoaded)
        {
            await SceneManager.UnloadSceneAsync(sceneToUnload);
        }

        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    public async void GoToResult()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneToUnload = SceneManager.GetActiveScene().name;

        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("ResultScene"));
        await sceneTask;

        if (SceneManager.GetSceneByName(sceneToUnload).isLoaded)
        {
            await SceneManager.UnloadSceneAsync(sceneToUnload);
        }

        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }

    public async void GoBack()
    {
        LoadingScreenController.Instance?.CreateLoadingCanvas();
        LoadingScreenController.Instance?.Show();

        var sceneToUnload = SceneManager.GetActiveScene().name;

        var sceneTask = GlobalSceneNavigator.Instance.Pop();
        await sceneTask;

        if (SceneManager.GetSceneByName(sceneToUnload).isLoaded)
        {
            await SceneManager.UnloadSceneAsync(sceneToUnload);
        }

        LoadingScreenController.Instance?.DestroyLoadingCanvas();
    }
}
