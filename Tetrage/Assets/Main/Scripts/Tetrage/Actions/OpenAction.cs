using UnityEngine;
using Tetrage.Models;
using Tetrage.Managers;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;

namespace Tetrage.Actions
{
    public class OpenAction : GameAction
    {
        private IReadOnlyList<Player> _others;
        private Card _card;
        private IPlayerProvider _provider; // 基本はDealer、テスト用にそれ以外

        public OpenAction(Player requester, IPlayerProvider provider) : base(requester)
        {
            _provider = provider ?? Dealer.Instance; // providerを受け取るが、デフォルトではDealerの単一なインスタンスとなる

            // provider（Dealer）を使って他プレイヤーの参照を書き込み
            _others = _provider.Players
                                .Where(p => !ReferenceEquals(p, requester)).ToList();
        }

        public override bool Validate()
        {
            // Openの条件判定

            /* もし他プレイヤーの手札に一枚でも裏のカードがあれば実行可能、つまりtrueを返す
             * _othersはrequester以外のプレイヤーの列挙
             * CardPileであるHandsは直接列挙可能（IEnumerable<Card>を実装している）なので.Anyが使える
             */
            return _others.Any(p => p.Hands.Any(c => !c.isVisible));
        }

        public override void Execute()
        {

        }
    }
}
