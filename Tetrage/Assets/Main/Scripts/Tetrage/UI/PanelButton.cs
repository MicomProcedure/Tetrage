using UnityEngine;

public class PanelButton : MonoBehaviour
{
    public GameObject targetPanel; // 表示したいパネル

    public void ShowPanel()
    {
        targetPanel.SetActive(true);
    }

    public void HidePanel()
    {
        targetPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        targetPanel.SetActive(!targetPanel.activeSelf);
    }
}
