using Cysharp.Threading.Tasks;
using Tetrage.Core.Ids;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// TurnStarted 適用完了まで待機するためのゲート。
    /// PlayerId ベースで動作し、ActorNumber 依存を排除。
    /// </summary>
    public sealed class TurnGate
    {
        private UniTaskCompletionSource<PlayerId> _tcs;
        public UniTask<PlayerId> WaitNextAsync()
        {
            _tcs ??= new UniTaskCompletionSource<PlayerId>();
            return _tcs.Task;
        }
        public void Release(PlayerId currentPlayerId)
        {
            if (_tcs == null) return;
            var t = _tcs;
            _tcs = null;
            t.TrySetResult(currentPlayerId);
        }
    }
}


