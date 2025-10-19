using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CutInAnimationView : MonoBehaviour
{
    [SerializeField] private GameObject cutInUI;
    [SerializeField] private Image cutInImage;
    [SerializeField] private Image cutInBackgroundMask;

    public async UniTask StartCutIn(CancellationToken token)
    {
        var backgroundMaskRect = cutInBackgroundMask.GetComponent<RectTransform>();
        var characterRect = cutInImage.GetComponent<RectTransform>();
        cutInUI.SetActive(true);

        // 位置を初期化
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(1600, 0), 0.0f).ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOAnchorPos(new Vector2(300, 375), 0.0f).ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOSizeDelta(new Vector2(0, 2500), 0.0f).ToUniTask(cancellationToken: token)
        );

        // カットインアニメーションの再生
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(-400, 0), 0.5f).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOSizeDelta(new Vector2(2000, 2500), 0.5f).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token)
        );

        // カットイン待機
        await UniTask.Delay(700, cancellationToken: token); // 700ミリ秒待機

        // カットインの終了アニメーションの再生
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(-1500, 0), 0.3f).SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOAnchorPos(new Vector2(-1500, 375), 0.3f).SetEase(Ease.InCubic)
                .OnComplete(() => { cutInUI.SetActive(false); })
                .ToUniTask(cancellationToken: token)
        );
    }
}
