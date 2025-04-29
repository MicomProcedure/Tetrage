using System.Collections;
using Tetrage.Models;
using Tetrage.Managers;
using Tetrage.Core.Contracts;

namespace Tetrage.Actions
{
    public class CheckAction : GameAction
    {
        // requesterは基底クラスで定義
        private Player _targetPlayer; 
        private IPlayerProvider _provider; // 基本はDealer、テスト用にそれ以外


        public CheckAction(Player requester, IPlayerProvider provider = null) : base(requester)
        {
            _provider = provider ?? Dealer.Instance; // providerを受け取るが、デフォルトではDealerの単一なインスタンスとなる

        }

        public override bool Validate()
        {

            return _targetPlayer.Target != null;

            return true;
        }

        // Executeが基底クラスにあります

        protected override IEnumerator Run()
        {
            // ここに動機的に遣りたい処理を書く

            yield break;
        }
    }
}
