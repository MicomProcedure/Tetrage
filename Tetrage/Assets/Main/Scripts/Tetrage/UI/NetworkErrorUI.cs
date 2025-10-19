using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NetworkErrorUI : MonoBehaviour
{

    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private GameObject errorMessagePanel;
    [SerializeField] private Canvas canvas;


    public void ShowErrorMessagePanel(string message)
    {
        Debug.Log(message);
        canvas.gameObject.SetActive(false);
        errorMessageText.text = message;
        errorMessagePanel.SetActive(true);
    }

    private void Close()
    {
        errorMessagePanel.SetActive(false);
    }
}