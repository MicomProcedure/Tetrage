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
        // null チェック
        if (errorMessagePanel == null || errorMessageText == null)
        {
            Debug.LogError($"[NetworkErrorUI] エラーパネルまたはテキストの参照が未設定です", this);
            return;
        }

        Debug.Log($"[NetworkErrorUI] エラーメッセージ表示: {message}");

        // エラーパネルが属する親Canvasを探して有効化（非アクティブだと表示されない）
        var parentCanvas = errorMessagePanel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
        {
            Debug.Log($"[NetworkErrorUI] 親Canvas '{parentCanvas.name}' を有効化します");
            parentCanvas.gameObject.SetActive(true);
        }

        // canvas フィールドが設定されている場合も有効化
        if (canvas != null && !canvas.gameObject.activeSelf)
        {
            Debug.Log($"[NetworkErrorUI] 指定Canvas '{canvas.name}' を有効化します");
            canvas.gameObject.SetActive(true);
        }

        errorMessageText.text = message;
        errorMessagePanel.SetActive(true);
    }

    public void Close()
    {
        if (errorMessagePanel != null)
        {
            errorMessagePanel.SetActive(false);
        }
    }
}