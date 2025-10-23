using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class GameStartAnimation : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform gameStartText; // 「GameStart」テキストのRectTransform
    [SerializeField] private Image backgroundImage;       // 背景イメージ

    [Header("Animation Settings")]
    [SerializeField] private float dropDistance = 500f;    // テキストの落下距離
    [SerializeField] private float moveDuration = 0.8f;    // 移動の速さ
    [SerializeField] private float stopTime = 1.0f;        // 停止時間（インスペクタで設定可能）
    [SerializeField] private float fadeDuration = 0.5f;    // 背景フェード時間
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameStartSE;

    private Sequence currentSequence; // 再生中のDOTweenシーケンス
    private Vector2 originalPosition; // 初期位置の保持

    public void PlayGameStartAnimation()
    {
        // 🎯 1. 既存のアニメーションを停止
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        // 🎯 2. 初期状態リセット
        backgroundImage.gameObject.SetActive(true);
        gameStartText.gameObject.SetActive(true);

        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);
        gameStartText.anchoredPosition = originalPosition + new Vector2(0, dropDistance);

        // 🎵 SE再生（PlayOneShotで途中停止を防ぐ）
        if (gameStartSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameStartSE);
        }

        // 🎬 3. DOTweenアニメーションを再構築
        currentSequence = DOTween.Sequence();

        currentSequence.Append(backgroundImage.DOFade(0.6f, fadeDuration)) // 背景フェードイン
            .Join(gameStartText.DOAnchorPosY(originalPosition.y, moveDuration).SetEase(Ease.OutCubic))
            .AppendInterval(stopTime) // 停止時間
            .Append(gameStartText.DOAnchorPosY(originalPosition.y - dropDistance, moveDuration).SetEase(Ease.InCubic))
            .Join(backgroundImage.DOFade(0f, fadeDuration)) // 背景フェードアウト
            .OnComplete(() =>
            {
                // 終了時に非アクティブ化
                backgroundImage.gameObject.SetActive(false);
                gameStartText.gameObject.SetActive(false);
            });
    }
}
