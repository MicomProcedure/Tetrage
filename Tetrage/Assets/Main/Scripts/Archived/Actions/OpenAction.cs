// using Cysharp.Threading.Tasks; // UniTask を使うために追加
// using UnityEngine;
// using Tetrage.Models;
// using Tetrage.Managers;
// using Tetrage.UI;
// using System.Collections.Generic;
// using System.Linq;
// using Tetrage.Core.Contracts;
// // using System.Collections; // IEnumerator が不要になるため削除

// namespace Tetrage.Actions
// {
//     public class OpenAction : GameAction
//     {
//         private IReadOnlyList<IPlayer> _others; //他のプレイヤークラスが入ってるリスト
//         private Card _card; // この変数は Run() メソッド内でしか使われていないため、ローカル変数にできる可能性もあります。
//         private IGameContextProvider _provider; // 基本はDealer、テスト用にそれ以外

//         public OpenAction(IPlayer requester, IGameContextProvider provider = null) : base(requester) // providerはデフォルト引数なので省略可能
//         {
//             _provider = provider ?? Dealer.Instance; // Dealerインスタンスを直接取得

//             if (_provider == null)
//             {
//                 throw new System.InvalidOperationException("IGameContextProvider が取得できません。Dealer.Instance が設定されているか確認してください。");
//             }

//             // provider（Dealer）を使って他プレイヤーの参照を書き込み
//             _others = _provider.Players
//                                  ?.Where(p => !ReferenceEquals(p, requester)).ToList()
//                                  ?? new List<IPlayer>();
//         }

//         public override bool Validate()
//         {
//             // Openの条件判定
//             // 他プレイヤーの手札に一枚でも裏のカードがあれば実行可能
//             return _others.Any(p => p.Hands.Any(c => !c.IsVisible));
//         }

//         // 基底クラスにvirtualな Execute() 関数が存在しているため、何も書かずとも Execute()は実行可能

//         protected override async UniTask Run() // IEnumerator から async UniTask に変更
//         {
//             // クリック可能カードをハイライト
//             var selectable = _others          // 自分以外のプレイヤー
//                 .SelectMany(p => p.Hands.Where(c => !c.IsVisible)) // 自分以外のプレイヤーの手札の家、裏のカードを選択
//                 .ToList(); // selectableに入れる

//             // selectable.ForEach(c => c.Hilight(true)); // もしHighlightメソッドがあるならコメント解除してください

//             // クリック待ちの処理
//             Card clickedCard = null;

//             // ローカル関数として定義
//             void OnClick(Card c)
//             {
//                 if (selectable.Contains(c)) clickedCard = c; // clickedCardがクリックされたカード
//             }

//             try
//             {
//                 CardClickDispatcher.OnCardClicked += OnClick;

//                 // yield return new WaitUntil(() => clickedCard != null); の代替
//                 // clickedCard が null でなくなるまで、毎フレーム待機
//                 await UniTask.WaitUntil(() => clickedCard != null); 
//             }
//             finally
//             {
//                 // try-finally を使うことで、待機中にエラーが発生しても必ずイベントを解除する
//                 CardClickDispatcher.OnCardClicked -= OnClick;
//             }

//             // 処理の実行
//             if (clickedCard != null) // clickedCard が null でないことを確認 (念のため)
//             {
//                 clickedCard.Flip(); // カードを裏返す
//             }
//             // selectable.ForEach(c => c.Highlight(false)); // もしHighlightメソッドがあるならコメント解除してください
//         }
//     }
// }