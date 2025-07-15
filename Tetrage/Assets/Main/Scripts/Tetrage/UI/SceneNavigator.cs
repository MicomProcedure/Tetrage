using UnityEngine;
using Cysharp.Threading.Tasks;
using MackySoft.Navigathena.SceneManagement;

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

    public async void GoToTitle()
    {
        await ShowLoadingPanelAsync();
        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("TitleScene"));
        var delayTask = UniTask.Delay(3000);
        await UniTask.WhenAll(sceneTask, delayTask);
        HideAndDestroyLoadingPanel();
    }

    public async void GoToGame()
    {
        await ShowLoadingPanelAsync();
        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("GameScene"));
        var delayTask = UniTask.Delay(3000);
        await UniTask.WhenAll(sceneTask, delayTask);
        HideAndDestroyLoadingPanel();
    }

    public async void GoToResult()
    {
        await ShowLoadingPanelAsync();
        var sceneTask = GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("ResultScene"));
        var delayTask = UniTask.Delay(3000);
        await UniTask.WhenAll(sceneTask, delayTask);
        HideAndDestroyLoadingPanel();
    }

    public async void GoBack()
    {
        await ShowLoadingPanelAsync();
        var sceneTask = GlobalSceneNavigator.Instance.Pop();
        var delayTask = UniTask.Delay(3000);
        await UniTask.WhenAll(sceneTask, delayTask);
        HideAndDestroyLoadingPanel();
    }
}
