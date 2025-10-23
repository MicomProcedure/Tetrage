using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Tetrage.Core.Contracts;

public class TargetCheckText : MonoBehaviour
{
    #region Private Fields
    private IGameContextProvider _gameContext;
    #endregion

    #region Serialized Fields
    [SerializeField] private TMP_Text targetText; // 変更対象のTextMeshPro
    [SerializeField] private GameObject button; 
    [TextArea(2, 5)]
    [SerializeField] private string newText = "新しいテキスト"; // インスペクタから変更可能なテキスト

    #endregion

    public void Initialize(IGameContextProvider gameContext)
    {
        _gameContext = gameContext;
    }
    // 関数を呼ぶとテキストを変更
    public void ChangeText()
    {
        if (targetText != null)
        {
            targetText.text = newText;
        }
        else
        {
            Debug.LogWarning("TMP_Text が設定されていません！", this);
        }
    }
    public void Finish()
    {
        button.SetActive(false);
    }
}
