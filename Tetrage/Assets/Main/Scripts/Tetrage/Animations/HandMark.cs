using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HandMark : MonoBehaviour
{
    [SerializeField] private Image handImage;  // 手形のImage
    private bool isPlaying = false;

    public void PlayHandEffectAtUI(RectTransform targetUI)
    {
        if (isPlaying) return; // 再生中ならスキップ

        isPlaying = true;
        // 手形を有効化
        handImage.gameObject.SetActive(true);

        // 対象UIの位置に合わせる
        handImage.rectTransform.anchoredPosition = targetUI.anchoredPosition;

        // 初期状態
        handImage.color = new Color(1, 1, 1, 1);
        handImage.rectTransform.localScale = Vector3.zero;

        // アニメーション
        Sequence seq = DOTween.Sequence();

        seq.Append(handImage.rectTransform.DOScale(1.2f, 0.4f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.5f);
        seq.Append(handImage.DOFade(0f, 1.5f));
        seq.OnComplete(() => {
            handImage.gameObject.SetActive(false);
            isPlaying = false;
        });
    }

    // 以下、テスト用（実際に使う場合は、他のスクリプトに分けた方が良いと思います

    [SerializeField] private HandMark handEffect;
    [SerializeField] private RectTransform targetButton;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            handEffect.PlayHandEffectAtUI(targetButton);
        }
    }
}
