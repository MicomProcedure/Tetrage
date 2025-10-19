using System.Threading;
using UnityEngine;

public class CutInAnimationPresenter : MonoBehaviour
{
    [SerializeField] private CutInAnimationView view;

    private async void Start()
    {
        // CancellationTokenSourceを作成
        var cts = new CancellationTokenSource();
        // CancellationTokenを取得 
        var token = cts.Token;

        await view.StartCutIn(token);

        cts.Cancel();
    }
}
