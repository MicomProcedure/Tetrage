using UnityEngine;
using Tetrage.Models;
using Tetrage.Managers;
using Tetrage.UI;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using System.Collections;

namespace Tetrage.Actions
{
    public class OpenAction : GameAction
    {
        private IReadOnlyList<Player> _others; //他のプレイヤークラスが入ってるリスト
        private Card _card;
        private IPlayerProvider _provider; // 基本はDealer、テスト用にそれ以外

        public OpenAction(Player requester, IPlayerProvider provider = null) : base(requester) // providerはデフォルト引数なので省略可能
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

        // 基底クラスにvirtualな Execute() 関数が存在しているため、何も書かずとも Execute()は実行可能

        protected override IEnumerator Run()
        {
            // クリック可能カードをハイライト
            var selectable = _others    // 自分以外のプレイヤー
                .SelectMany(p => p.Hands.Where(c => !c.isVisible)) // 自分以外のプレイヤーの手札の家、裏のカードを選択
                .ToList(); // selectableに入れる

            //selectable.ForEach(c => c.Hilight(true));

            // クリック待ちの処理
            Card clickedCard = null;
            try
            {
                CardClickDispatcher.OnCardClicked += OnClick;

                yield return new WaitUntil(() => clickedCard != null); // クリックされるまで毎フレームチェック
            }
            finally
            {
                // try.finallyを使うことで、待機中にエラーが発生しても必ずイベントを解除する
                CardClickDispatcher.OnCardClicked -= OnClick;
            }
            // 処理の実行
            clickedCard.Flip();
            //selectable.ForEach(c => c.Highlight(false));



            // ローカル関数
            void OnClick(Card c)
            {
                if (selectable.Contains(c)) clickedCard = c; //clickedCardがクリックされたカード
            }
        }


    }
}
