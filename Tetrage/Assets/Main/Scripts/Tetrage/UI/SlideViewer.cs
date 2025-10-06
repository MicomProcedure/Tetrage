using UnityEngine;
using UnityEngine.UI;

public class SlideViewer : MonoBehaviour
{
    public Sprite[] slides;      // スライド画像を入れる
    public Image slideImage;     // 表示するImage
    public GameObject panel;     // このスライド用Panel
    private int currentIndex = 0;

    void OnEnable()
    {
        // パネルがアクティブになった時に最初のスライドを表示
        currentIndex = 0;
        ShowSlide(0);
    }

    void Update()
    {
        // 画面タップ / マウスクリックで進める
        if (Input.GetMouseButtonDown(0))  // 左クリック or タップ
        {
            NextSlide();
        }
    }

    void NextSlide()
    {
        currentIndex++;
        if (currentIndex < slides.Length)
        {
            ShowSlide(currentIndex);
        }
        else
        {
            // 最後のスライドを超えたらパネルを閉じる
            panel.SetActive(false);
        }
    }

    void ShowSlide(int index)
    {
        slideImage.sprite = slides[index];
    }
}