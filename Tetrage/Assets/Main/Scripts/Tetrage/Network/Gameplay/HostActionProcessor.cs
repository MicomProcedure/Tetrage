namespace Tetrage.Network.Gameplay
{
    public interface IHostActionProcessor
    {
        void Process(ActionRequestedEvent e);
    }

    /// <summary>
    /// 既定のホスト側アクション処理。現状は簡易acceptで結果配信のみ。
    /// 後にDealer/戦略での検証・適用に差し替える。
    /// </summary>
    public sealed class DefaultHostActionProcessor : IHostActionProcessor
    {
        private readonly IGameplayNetworkController _netCtl;

        public DefaultHostActionProcessor(IGameplayNetworkController netCtl)
        {
            _netCtl = netCtl;
        }

        public void Process(ActionRequestedEvent e)
        {
            // 最小実装: リクエスターのみに結果を返す（Othersへの効果配信はこの後の実装で追加）
            var seq = _netCtl.Sequence;
            var result = new ActionResultEvent
            {
                sequence = seq.NextSequence(),
                clientSequence = e.clientSequence,
                actorPlayerId = e.actorPlayerId,
                actionType = e.actionType,
                accepted = true,
                reason = string.Empty,
                targetCardIds = e.targetCardIds,
            };
            _netCtl.Broadcaster.RaiseToActor(EventCode.ActionResult, result, e.actorPlayerId);
        }
    }
}


