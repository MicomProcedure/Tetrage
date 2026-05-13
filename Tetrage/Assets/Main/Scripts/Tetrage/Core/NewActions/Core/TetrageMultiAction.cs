using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageMulti 専用の Action クラス。
    /// TetrageMultiExecutor は ExecuteAsync 内で自ら StartRequest を送信し、
    /// Host の最終 ActionResult まで待機してから戻る設計になっている。
    /// そのため NetworkActionBase.OnAfterExecute が後から同じ descriptor を
    /// 再送信しないよう、ここで no-op にオーバーライドする。
    /// ※ Executor が「送信→待機→返却」の責務をすべて持つ設計を採用した結果のトレードオフ。
    /// </summary>
    public sealed class TetrageMultiAction : NetworkActionBase
    {
        public override ActionType ActionType => ActionType.TetrageMulti;

        public TetrageMultiAction(IPlayer requester, IActionValidator validator, IActionExecutor executor)
            : base(requester, validator, executor) { }

        /// <summary>
        /// Executor がリクエスト送信済みのため、追加のネットワーク送信は行わない。
        /// </summary>
        protected override async UniTask OnAfterExecute(IActionContext context, ActionResult result)
        {
            await UniTask.Yield();
        }
    }
}
