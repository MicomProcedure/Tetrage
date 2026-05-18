using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Audio
{
    /// <summary>
    /// IGameplayEventBus の購読と、ドメインイベントから音声再生への変換を担当する。
    /// </summary>
    public sealed class InGameAudioEventPresenter
    {
        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly List<PileId> _targetPiles = new();
        private const int _delayOffsetMS = 1000;

        #endregion

        #region Public Methods

        /// <summary>
        /// イベント購読を開始する。
        /// </summary>
        public void Bind(
            IGameplayEventBus events,
            IReadOnlyList<IPlayer> players,
            InGameAudioCatalog catalog,
            SEAudioChannel seChannel,
            InGameBgmStateMachine bgmState,
            CancellationToken destroyToken)
        {
            Unbind();

            BuildTargetPileFilter(players);

            if (catalog.GetSe(InGameSEId.CardMove) != null)
            {
                // 移動先が PlayerTarget のときは鳴らさない（フィールド初期化時のノイズ防止）
                events.CardMoved
                    .Where(e => !_targetPiles.Contains(e.ToPileId))
                    .Subscribe(_ => seChannel.TryPlayGated(InGameSEId.CardMove, catalog.GetSe(InGameSEId.CardMove)))
                    .AddTo(_disposables);
            }

            events.ScanPhaseEnded
                .Subscribe(_ => OnScanPhaseEnded(catalog, seChannel, bgmState, destroyToken))
                .AddTo(_disposables);

            if (catalog.GetBgm(InGameBgmId.AfterReach) != null)
            {
                events.ActionResult
                    .Where(e => e.ActionType == ActionType.Reach && e.Accepted)
                    .Subscribe(_ => bgmState.TrySwitchToAfterReach())
                    .AddTo(_disposables);
            }
        }

        /// <summary>
        /// 購読を解除する。
        /// </summary>
        public void Unbind()
        {
            _disposables.Clear();
            _targetPiles.Clear();
        }

        #endregion

        #region Private Methods

        private void BuildTargetPileFilter(IReadOnlyList<IPlayer> players)
        {
            _targetPiles.Clear();
            if (players == null)
            {
                return;
            }

            foreach (var player in players)
            {
                _targetPiles.Add(PileIds.PlayerTarget(player.Id));
            }
        }

        private void OnScanPhaseEnded(
            InGameAudioCatalog catalog,
            SEAudioChannel seChannel,
            InGameBgmStateMachine bgmState,
            CancellationToken destroyToken)
        {
            OnScanPhaseEndedAsync(catalog, seChannel, bgmState, destroyToken).Forget();
        }

        private async UniTaskVoid OnScanPhaseEndedAsync(
            InGameAudioCatalog catalog,
            SEAudioChannel seChannel,
            InGameBgmStateMachine bgmState,
            CancellationToken destroyToken)
        {
            seChannel.TryPlay(catalog.GetSe(InGameSEId.GameStart));

            var delayMs = catalog.GetSeLengthMilliseconds(InGameSEId.GameStart) - _delayOffsetMS;
            if (delayMs > 0)
            {
                await UniTask.Delay(delayMs, cancellationToken: destroyToken);
            }

            bgmState.PlayBeforeReach();
        }

        #endregion
    }
}
