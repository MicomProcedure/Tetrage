using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CutInAnimationView : MonoBehaviour
{
    [SerializeField] private GameObject cutInUI;
    [SerializeField] private Image cutInImage;
    [SerializeField] private GameObject cutInText;
    [SerializeField] private Image cutInBackgroundMask;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutInSE;

    public async UniTask StartCutIn(CancellationToken token)
    {
        var backgroundMaskRect = cutInBackgroundMask.GetComponent<RectTransform>();
        var characterRect = cutInImage.GetComponent<RectTransform>();
        var textRect = cutInText.GetComponent<RectTransform>();
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
            characterRect.DOAnchorPos(new Vector2(-600, 0), 0.5f).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token),
            textRect.DOAnchorPos(new Vector2(-100, 50), 0.5f).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOSizeDelta(new Vector2(2000, 2500), 0.5f).SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: token)
        );

        // カットイン待機
        await UniTask.Delay(700, cancellationToken: token); // 700ミリ秒待機

        // カットインの終了アニメーションの再生
        await UniTask.WhenAll(
            characterRect.DOAnchorPos(new Vector2(-1700, 0), 0.3f).SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: token),
            textRect.DOAnchorPos(new Vector2(-1200, 50), 0.3f).SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: token),
            backgroundMaskRect.DOAnchorPos(new Vector2(-1500, 375), 0.3f).SetEase(Ease.InCubic)
                .OnComplete(() => { cutInUI.SetActive(false); })
                .ToUniTask(cancellationToken: token)
        );
    }
}
