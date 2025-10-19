using System.Threading;
using UnityEngine;

public class CutInAnimationController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private CutInAnimationView view;

    private CancellationTokenSource cts;

    // 外部から呼び出してカットインを再生
    public void PlayCutIn()
    {
        // 前回の再生中ならキャンセル
        cts?.Cancel();
        cts?.Dispose();

        // 新しいCancellationTokenを作成
        cts = new CancellationTokenSource();
        var token = cts.Token;

        // 非同期でアニメーションを開始（待たずに実行）
        _ = view.StartCutIn(token);
    }

    // オブジェクトが無効化されたときにアニメーションをキャンセル
    private void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }
}
