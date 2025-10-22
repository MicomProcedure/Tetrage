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
        handImage.gameObject.SetActive(true);
        handImage.rectTransform.anchoredPosition = targetUI.anchoredPosition;

        // 初期状態：大きく・透明
        handImage.rectTransform.localScale = Vector3.one * 7.0f;
        handImage.color = new Color(1, 1, 1, 0); // 透明

        // シーケンス作成
        Sequence seq = DOTween.Sequence();

        // 縮小しながら不透明に（出現）
        seq.Append(handImage.rectTransform.DOScale(1.0f, 0.25f).SetEase(Ease.OutExpo));
        seq.Join(handImage.DOFade(0.8f, 0.25f));

        // 少し止める（見せ時間）
        seq.AppendInterval(0.8f);

        // フェードアウト（消える）
        seq.Append(handImage.DOFade(0f, 0.1f).SetEase(Ease.InQuad));

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
