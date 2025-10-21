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
        private readonly INetworkBroadcaster _broadcaster;

        public DefaultHostActionProcessor(IGameplayNetworkController netCtl)
        {
            _broadcaster = netCtl.Broadcaster;
        }

        public void Process(ActionRequestedEvent e)
        {
            var result = new ActionResultEvent
            {
                sequence = e.sequence, // TODO: ホスト連番に置換
                clientSequence = e.clientSequence,
                actorPlayerId = e.actorPlayerId,
                actionType = e.actionType,
                accepted = true,
                reason = string.Empty,
                targetCardIds = e.targetCardIds,
            };
            _broadcaster.Raise(EventCode.ActionResult, result);
        }
    }
}


