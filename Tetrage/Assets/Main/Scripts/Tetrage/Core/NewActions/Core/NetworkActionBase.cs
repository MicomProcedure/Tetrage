using Cysharp.Threading.Tasks;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// ネットワーク送信（ActionRequestedEvent）を共通処理として担うアクション基底。
    /// 各Executorは ActionResult.AdditionalData に ActionRequestDescriptor を詰める。
    /// </summary>
    public abstract class NetworkActionBase : ActionBase
    {
        protected NetworkActionBase(
            Tetrage.Core.Contracts.IPlayer requester,
            IActionValidator validator,
            IActionExecutor executor)
            : base(requester, validator, executor)
        {
        }

        #region Hook
        protected override async UniTask OnAfterExecute(IActionContext context, ActionResult result)
        {
            // AdditionalData から送信情報を取得して送信。
            if (context?.Network != null && result?.AdditionalData is ActionRequestDescriptor desc)
            {
                var request = new ActionRequestedEvent
                {
                    sequence = 0, // Host側で採番
                    clientSequence = context.Network.NextClientSequence(),
                    actorPlayerId = desc.actorPlayerId,
                    actionType = desc.actionType,
                    targetCardIds = desc.targetCardIds,
                };
                context.Network.Request(request);
            }

            await UniTask.Yield();
        }
        #endregion
    }
}


