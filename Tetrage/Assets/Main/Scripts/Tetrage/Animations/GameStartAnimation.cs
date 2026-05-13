using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム開始時のUIアニメーションを再生するView。
/// </summary>
public class GameStartAnimation : MonoBehaviour
{
    #region Serialized Fields

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

    #endregion

    #region Private Fields

    private Sequence currentSequence; // 再生中のDOTweenシーケンス
    private Vector2 originalPosition; // 初期位置の保持

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateReferences();

        if (gameStartText != null)
        {
            originalPosition = gameStartText.anchoredPosition;
        }
    }

    private void OnDisable()
    {
        KillCurrentSequence();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ゲーム開始演出を再生する。
    /// </summary>
    public void PlayAnimation()
    {
        if (!ValidateReferences())
        {
            return;
        }

        KillCurrentSequence();
        ResetAnimationState();
        PlaySE();
        BuildSequence();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 初期状態へ戻す。
    /// </summary>
    private void ResetAnimationState()
    {
        backgroundImage.gameObject.SetActive(true);
        gameStartText.gameObject.SetActive(true);

        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);
        gameStartText.anchoredPosition = originalPosition + new Vector2(0, dropDistance);
    }

    /// <summary>
    /// ゲーム開始SEを再生する。
    /// </summary>
    private void PlaySE()
    {
        if (gameStartSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameStartSE);
        }
    }

    /// <summary>
    /// DOTweenシーケンスを構築して再生する。
    /// </summary>
    private void BuildSequence()
    {
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

    /// <summary>
    /// 再生中のシーケンスを停止する。
    /// </summary>
    private void KillCurrentSequence()
    {
        if (currentSequence == null || !currentSequence.IsActive())
        {
            return;
        }

        currentSequence.Kill();
        currentSequence = null;
    }

    /// <summary>
    /// Inspector参照の設定漏れを検出する。
    /// </summary>
    private bool ValidateReferences()
    {
        if (gameStartText == null)
        {
            Debug.LogWarning("GameStartAnimation: gameStartText が未設定です。ゲーム開始演出をスキップします。", this);
            return false;
        }

        if (backgroundImage == null)
        {
            Debug.LogWarning("GameStartAnimation: backgroundImage が未設定です。ゲーム開始演出をスキップします。", this);
            return false;
        }

        return true;
    }

    #endregion
}
