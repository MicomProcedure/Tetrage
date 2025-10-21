using Cysharp.Threading.Tasks;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// TurnStarted 適用完了まで待機するためのゲート。
    /// </summary>
    public sealed class TurnGate
    {
        private UniTaskCompletionSource<int> _tcs;
        public UniTask<int> WaitNextAsync()
        {
            _tcs ??= new UniTaskCompletionSource<int>();
            return _tcs.Task;
        }
        public void Release(int currentPlayerActorNumber)
        {
            if (_tcs == null) return;
            var t = _tcs;
            _tcs = null;
            t.TrySetResult(currentPlayerActorNumber);
        }
    }
}


