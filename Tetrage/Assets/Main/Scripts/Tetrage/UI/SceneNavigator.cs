using UnityEngine;
using Cysharp.Threading.Tasks;
using MackySoft.Navigathena.SceneManagement;

public class SceneNavigator : MonoBehaviour
{


    public async void GoToTitle()
    {
        await GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("TitleScene"));
    }

    public async void GoToGame()
    {
        await GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("GameScene"));
    }

    public async void GoToResult()
    {
        await GlobalSceneNavigator.Instance.Push(new BuiltInSceneIdentifier("ResultScene"));
    }

    public async void GoBack()
    {
        await GlobalSceneNavigator.Instance.Pop();
    }
}
