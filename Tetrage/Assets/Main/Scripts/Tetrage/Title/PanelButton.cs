using UnityEngine;

namespace Tetrage.Title
{
    public class PanelButton : MonoBehaviour
{
    public GameObject[] targetPanelS; // 表示したいパネル
    public GameObject[] targetPanelH; // 隠したいパネル

    public void ShowPanel()
    {
        foreach(var targetPanelS in targetPanelS)
        targetPanelS.SetActive(true);
    }

    public void HidePanel()
    {
        foreach(var targetPanelH in targetPanelH)
        targetPanelH.SetActive(false);
    }
    }
}
