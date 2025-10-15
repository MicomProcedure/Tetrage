using UnityEngine;
using TMPro;

namespace Tetrage.Title
{
    public class NumberInputController : MonoBehaviour
{
    [SerializeField] public TMP_Text DisplayText;  // 表示用のテキスト
    [SerializeField] private int maxLength = 5;     // 最大桁数

    private void Start()
    {
        if (DisplayText == null)
        {
            Debug.LogError("DisplayTextがアサインされていません！");
        }
    }

    // 数字ボタンを押した時に呼ばれる
    public void OnNumberButtonPressed(string number)
    {
        if (DisplayText.text.Length >= maxLength) return; // 最大桁数に達していたら無視
        DisplayText.text += number;
    }

    // 消すボタンを押した時に呼ばれる
    public void OnDeleteButtonPressed()
    {
        if (DisplayText.text.Length > 0)
        {
            DisplayText.text = DisplayText.text.Substring(0, DisplayText.text.Length - 1);
        }
    }

    // 必要に応じてリセットボタン用
    public void OnClearButtonPressed()
    {
        DisplayText.text = "";
    }
    }
}

