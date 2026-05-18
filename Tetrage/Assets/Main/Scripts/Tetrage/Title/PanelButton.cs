using UnityEngine;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;

namespace Tetrage.Title
{
    public class PanelButton : MonoBehaviour
{
    [FormerlySerializedAs("targetPanelS")]
    public GameObject[] targetPanelShow; // 表示したいパネル
    [FormerlySerializedAs("targetPanelH")]
    public GameObject[] targetPanelHide; // 隠したいパネル

    public async void ShowPanel()
    {
        foreach(var targetPanelS in targetPanelShow){
        await UniTask.Yield();
        targetPanelS.SetActive(true);
        }
    }

    public async void HidePanel()
    {
        foreach(var targetPanelH in targetPanelHide)
        {
        await UniTask.Yield();
        targetPanelH.SetActive(false);
        }
    }
    }
}
