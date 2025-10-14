using UnityEngine;
using TMPro;

public class NumberInputController : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;  // 表示用のテキスト
    [SerializeField] private int maxLength = 5;     // 最大桁数

    private void Start()
    {
        if (displayText == null)
        {
            Debug.LogError("DisplayTextがアサインされていません！");
        }
    }

    // 数字ボタンを押した時に呼ばれる
    public void OnNumberButtonPressed(string number)
    {
        if (displayText.text.Length >= maxLength) return; // 最大桁数に達していたら無視
        displayText.text += number;
    }

    // 消すボタンを押した時に呼ばれる
    public void OnDeleteButtonPressed()
    {
        if (displayText.text.Length > 0)
        {
            displayText.text = displayText.text.Substring(0, displayText.text.Length - 1);
        }
    }

    // 必要に応じてリセットボタン用
    public void OnClearButtonPressed()
    {
        displayText.text = "";
    }
}

