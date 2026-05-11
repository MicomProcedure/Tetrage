using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class LoadingAnimation : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform suitClub; // 「Club」ImageのRectTransform
    [SerializeField] private RectTransform suitDiamond; // 「Diamond」ImageのRectTransform
    [SerializeField] private RectTransform suitHeart; // 「Heart」ImageのRectTransform
    [SerializeField] private RectTransform suitSpade; // 「Spade」ImageのRectTransform

    [Header("Animation Settings")]
    [SerializeField] private float interval = 0.2f;    // 次のマークまでの間隔
    [SerializeField] private float rotationSpeed = 360f;   // 回転速度（度/秒）

    private Sequence currentSequence; // 再生中のDOTweenシーケンス
    private Vector3 clubOriginalRotation;
    private Vector3 diamondOriginalRotation;
    private Vector3 heartOriginalRotation;
    private Vector3 spadeOriginalRotation;


    private void OnEnable()
    {
        CacheOriginalTransforms();
        PlayLoadingAnimation();
    }

    private void OnDisable()
    {
        StopLoadingAnimation();
    }

    public void StopLoadingAnimation()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        if (suitClub != null) suitClub.localEulerAngles = clubOriginalRotation;
        if (suitDiamond != null) suitDiamond.localEulerAngles = diamondOriginalRotation;
        if (suitHeart != null) suitHeart.localEulerAngles = heartOriginalRotation;
        if (suitSpade != null) suitSpade.localEulerAngles = spadeOriginalRotation;
    }

    public void PlayLoadingAnimation()
    {
        // 🎯 1. 既存のアニメーションを停止
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        if (suitClub != null) suitClub.localEulerAngles = clubOriginalRotation;
        if (suitDiamond != null) suitDiamond.localEulerAngles = diamondOriginalRotation;
        if (suitHeart != null) suitHeart.localEulerAngles = heartOriginalRotation;
        if (suitSpade != null) suitSpade.localEulerAngles = spadeOriginalRotation;


        // 🎯 2. 初期状態リセット
        if (suitClub != null) suitClub.gameObject.SetActive(true);
        if (suitDiamond != null) suitDiamond.gameObject.SetActive(true);
        if (suitHeart != null) suitHeart.gameObject.SetActive(true);
        if (suitSpade != null) suitSpade.gameObject.SetActive(true);


        // 🎬 3. DOTweenアニメーションを再構築
        currentSequence = DOTween.Sequence();

        AppendRotate(currentSequence, suitClub);
        AppendRotate(currentSequence, suitDiamond);
        AppendRotate(currentSequence, suitHeart);
        AppendRotate(currentSequence, suitSpade);

        // ローディング中ずっと回す
        currentSequence.SetLoops(-1, LoopType.Restart);
    }

    private void AppendRotate(Sequence seq, RectTransform target)
    {
        if (seq == null || target == null) return;

        // 間を開ける → その場で1回転 → 間を空ける
        if (interval > 0f) seq.AppendInterval(interval);
        var rotateDuration = rotationSpeed > 0f ? (360f / rotationSpeed) : 0f;
        if (rotateDuration > 0f)
        {
            // 現在角度から相対的にY軸へ+360度（1回転）
            seq.Append(
                target.DOLocalRotate(new Vector3(0f, 360f, 0f), rotateDuration, RotateMode.FastBeyond360)
                    .SetRelative(true)
                    .SetEase(Ease.Linear)
            );
        }
        if (interval > 0f) seq.AppendInterval(interval);
    }

    private void CacheOriginalTransforms()
    {
        if (suitClub != null)
        {
            clubOriginalRotation = suitClub.localEulerAngles;
        }
        if (suitDiamond != null)
        {
            diamondOriginalRotation = suitDiamond.localEulerAngles;
        }
        if (suitHeart != null)
        {
            heartOriginalRotation = suitHeart.localEulerAngles;
        }
        if (suitSpade != null)
        {
            spadeOriginalRotation = suitSpade.localEulerAngles;
        }
    }

}
