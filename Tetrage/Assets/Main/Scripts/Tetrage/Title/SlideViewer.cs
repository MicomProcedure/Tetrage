using UnityEngine;
using UnityEngine.UI;

public class SlideViewer : MonoBehaviour
{
    [Header("スライド設定")]
    public Sprite[] slides;          // スライド画像を入れる
    public Image slideImage;         // 表示するImage
    public GameObject panel;         // このスライド用Panel

    [Header("UIボタン設定")]
    public Button nextButton;        // 「次へ」ボタン
    public Button backButton;        // 「戻る」ボタン
    public Button closeButton;       // 「終了」ボタン

    private int currentIndex = 0;

    void OnEnable()
    {
        currentIndex = 0;
        ShowSlide(currentIndex);

        // ボタンのイベント登録
        nextButton.onClick.AddListener(NextSlide);
        backButton.onClick.AddListener(PreviousSlide);
        closeButton.onClick.AddListener(HideAll);

        UpdateButtonVisibility();
    }

    void OnDisable()
    {
        // 多重登録防止のため削除
        nextButton.onClick.RemoveListener(NextSlide);
        backButton.onClick.RemoveListener(PreviousSlide);
        closeButton.onClick.RemoveListener(HideAll);
    }

    /// <summary>
    /// 次のスライドへ進む
    /// </summary>
    void NextSlide()
    {
        if (currentIndex < slides.Length - 1)
        {
            currentIndex++;
            ShowSlide(currentIndex);
            UpdateButtonVisibility();
        }
    }

    /// <summary>
    /// 前のスライドに戻る
    /// </summary>
    void PreviousSlide()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowSlide(currentIndex);
            UpdateButtonVisibility();
        }
    }

    /// <summary>
    /// 指定インデックスのスライドを表示
    /// </summary>
    void ShowSlide(int index)
    {
        if (slides.Length == 0 || index < 0 || index >= slides.Length) return;
        slideImage.sprite = slides[index];
    }

    /// <summary>
    /// ボタンの表示・非表示をスライド位置に応じて更新
    /// </summary>
    void UpdateButtonVisibility()
    {
        // 最初のスライド → 戻るボタン非表示
        backButton.gameObject.SetActive(currentIndex > 0);

        // 最後のスライド → 次へボタン非表示
        nextButton.gameObject.SetActive(currentIndex < slides.Length - 1);

        // 終了：常に表示
        closeButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 全スライドとボタンを非表示にする
    /// </summary>
    void HideAll()
    {
        panel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }
}
