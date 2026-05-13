using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Tetrage.Core.Constants;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// カットイン演出の表示とTween再生を担当するView。
/// </summary>
public class CutInAnimationView : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] private GameObject cutInUI;
    [SerializeField] private Image cutInImage;
    [SerializeField] private GameObject cutInText;
    [SerializeField] private Image cutInBackgroundMask;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutInSE;

    #endregion

    #region Public Methods

    /// <summary>
    /// 指定した合計Durationに収まるようにカットインを再生する。
    /// </summary>
    public async UniTask StartCutIn(int totalDurationMs, CancellationToken token)
    {
        var backgroundMaskRect = cutInBackgroundMask.GetComponent<RectTransform>();
        var characterRect = cutInImage.GetComponent<RectTransform>();
        var textRect = cutInText.GetComponent<RectTransform>();
        var waitDurationMs = CalculateWaitDurationMs(totalDurationMs);
        cutInUI.SetActive(true);

        // 位置を初期化
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(1400, 0), 0.0f).ToUniTask(cancellationToken: token),
            textRect.DOAnchorPos(new Vector2(1900, 50), 0.0f).ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOAnchorPos(new Vector2(300, 375), 0.0f).ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOSizeDelta(new Vector2(0, 2500), 0.0f).ToUniTask(cancellationToken: token)
        );

        audioSource.PlayOneShot(cutInSE);

        // カットインアニメーションの再生
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(-600, 0), InGameConsts.CutInAnimationDuration.ENTRY_DURATION_SECONDS).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token),
            textRect.DOAnchorPos(new Vector2(-100, 50), InGameConsts.CutInAnimationDuration.ENTRY_DURATION_SECONDS).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOSizeDelta(new Vector2(2000, 2500), InGameConsts.CutInAnimationDuration.ENTRY_DURATION_SECONDS).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token)
        );

        // 合計Durationに収まるように中央の表示時間を調整する
        await UniTask.Delay(waitDurationMs, cancellationToken: token);

        // カットインの終了アニメーションの再生
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(-1700, 0), InGameConsts.CutInAnimationDuration.EXIT_DURATION_SECONDS).SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: token),
            textRect.DOAnchorPos(new Vector2(-1200, 50), InGameConsts.CutInAnimationDuration.EXIT_DURATION_SECONDS).SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOAnchorPos(new Vector2(-1500, 375), InGameConsts.CutInAnimationDuration.EXIT_DURATION_SECONDS).SetEase(Ease.InCubic)
                .OnComplete(() => { cutInUI.SetActive(false); })
                .ToUniTask(cancellationToken: token)
        );
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 合計Durationから入退場Tweenを除いた表示待機時間を算出する。
    /// </summary>
    private static int CalculateWaitDurationMs(int totalDurationMs)
    {
        var transitionDurationMs =
            Mathf.RoundToInt(
                (InGameConsts.CutInAnimationDuration.ENTRY_DURATION_SECONDS
                    + InGameConsts.CutInAnimationDuration.EXIT_DURATION_SECONDS)
                * InGameConsts.CutInAnimationDuration.MILLISECONDS_PER_SECOND);

        return Mathf.Max(0, totalDurationMs - transitionDurationMs);
    }

    #endregion
}
