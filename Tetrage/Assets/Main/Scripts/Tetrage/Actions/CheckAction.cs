using Cysharp.Threading.Tasks; // UniTask を使うために追加
using UnityEngine;
using Tetrage.Models;
using Tetrage.Managers;
using Tetrage.UI;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
// using System.Collections; // IEnumerator が不要になるため削除

namespace Tetrage.Actions
{
    public class CheckAction : GameAction
    {
        private IReadOnlyList<IPlayer> _others; //他のプレイヤークラスが入ってるリスト
        private Card _card; // この変数は Run() メソッド内でしか使われていないため、ローカル変数にできる可能性もあります。
        private IGameContextProvider _provider; // 基本はDealer、テスト用にそれ以外

        public CheckAction(IPlayer requester, IGameContextProvider provider = null) : base(requester) // providerはデフォルト引数なので省略可能
        {
            _provider = provider ?? Dealer.Instance; // Dealerインスタンスを直接取得

            if (_provider == null)
            {
                throw new System.InvalidOperationException("IGameContextProvider が取得できません。Dealer.Instance が設定されているか確認してください。");
            }

            // provider（Dealer）を使って他プレイヤーの参照を書き込み
            _others = _provider.Players
                                 ?.Where(p => !ReferenceEquals(p, requester)).ToList()
                                 ?? new List<IPlayer>();
        }

        public override bool Validate()
        {
            // 全てのカードが一致しているかを返す
            // 手札が空の場合は例外を避けるため、First() の前に Any() でチェックする方が安全です。
            return _requester.Hands.Any() && _requester.Hands.All(c => c.Suit == _requester.Hands.First().Suit);
        }

        protected override async UniTask Run() // IEnumerator から async UniTask に変更
        {
            var selectable = _others
                .Select(p => p.Target.FirstOrDefault()) // 各プレイヤーのターゲットを取得し CardPile にキャスト
                .Where(c => c != null && c.IsVisible) // Nullや表向きカードは除外
                .ToList();

            // クリック待ちの処理
            Card clickedCard = null;
            
            // ローカル関数として定義
            void OnClick(Card c)
            {
                // クリックされたカードが選択可能なカードリストに含まれているかチェック
                if (selectable.Contains(c)) 
                {
                    clickedCard = c;
                }
            }

            try
            {
                CardClickDispatcher.OnCardClicked += OnClick;

                // yield return new WaitUntil(() => clickedCard != null); の代替
                // clickedCard が null でなくなるまで、毎フレーム待機
                await UniTask.WaitUntil(() => clickedCard != null); 
            }
            finally
            {
                // try-finally を使うことで、待機中にエラーが発生しても必ずイベントを解除する
                CardClickDispatcher.OnCardClicked -= OnClick;
            }

            // ここでclickedCardにクリックしたカードが入ってるからそのカードと，自分の手札3枚が一致してるかを確かめる
            // Validate() で手札が空でないことを確認していますが、念のため First() の前に Any() を再確認するかもしれません。
            // あるいは、手札が空の場合の挙動を明確にする必要があります。
            if (_requester.Hands.Any() && _requester.Hands.First().Suit == clickedCard.Suit)
            {
                //チェック成功
                Debug.Log("チェック成功");
            }
            else
            {
                //チェック失敗
                Debug.Log("チェック失敗");
            }
        }
    }
}