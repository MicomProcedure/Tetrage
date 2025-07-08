using UnityEngine;
using UnityEngine.UI;
using System;
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
        currentDialog.transform.Find("Title").GetComponent<Text>().text = title;
        currentDialog.transform.Find("Message").GetComponent<Text>().text = message;

        var okButton = currentDialog.transform.Find("OkButton").GetComponent<Button>();
        var cancelButton = currentDialog.transform.Find("CancelButton").GetComponent<Button>();

        okButton.onClick.AddListener(() => {
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
