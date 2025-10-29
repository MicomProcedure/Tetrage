using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Cysharp.Threading.Tasks;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }
    [SerializeField] private GameObject dialogPrefab;
    private GameObject currentDialog;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async UniTask ShowDialog(string title, string message, Action onOk = null, Action onCancel = null)
    {
        if (currentDialog != null) Destroy(currentDialog);

        currentDialog = Instantiate(dialogPrefab, transform);
        // ダイアログUIの各要素を取得してセット
        currentDialog.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = title;
        currentDialog.transform.Find("Message").GetComponent<TextMeshProUGUI>().text = message;

        var okButton = currentDialog.transform.Find("OkButton").GetComponent<Button>();
        var cancelButton = currentDialog.transform.Find("CancelButton").GetComponent<Button>();

        okButton.onClick.AddListener(() => {
            Debug.Log(okButton != null ? "OKボタン取得成功" : "OKボタン取得失敗");
            onOk?.Invoke();
            Destroy(currentDialog);
        });
        cancelButton.onClick.AddListener(() => {
            onCancel?.Invoke();
            Destroy(currentDialog);
        });

        // UniTaskでダイアログの完了を待つ場合はTaskCompletionSourceを使う
        await UniTask.WaitUntil(() => currentDialog == null);
    }
}
