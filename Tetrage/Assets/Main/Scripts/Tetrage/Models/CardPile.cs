using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using System.Linq;
using System;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;

namespace Tetrage.Models
{
    public class CardPile : IEnumerable<Card>, IIdentifiable<PileId>
    {
        /// <summary>
        /// パイル一意ID（不変）
        /// </summary>
        public PileId Id { get; }


        // 内部のカードリスト
        private readonly List<Card> _cards;
        // 内部のカードリストのプロパティ
        public IReadOnlyList<Card> Cards => _cards;

        // カードの束の最大枚数
        private readonly int _maxCount;
        public int MaxCount => _maxCount;

        // 束の名称
        public string Name { get; }


        // IEnumerableを実装するためのメンバその１：IEnumerator<T> を返す GetEnumerator()
        public IEnumerator<Card> GetEnumerator()
        {
            return _cards.GetEnumerator();
        }

        // IEnumerableを実装するためのメンバその2：非ジェネリック版 IEnumerator を返す GetEnumerator()
        // 明示的インターフェイスの実装なので、privateになっています（ここら辺よく分かんないけど、よく分かんなくていいっぽい）
        // 重要なのは、これによってCardPileが列挙可能になり、LINQが仕えるようになるということ、だと思う
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <summary>
        /// この束に含まれるカードの枚数
        /// </summary>
        public int Count => _cards.Count;


        /// <summary>
        /// コンストラクタで初期カードをまとめて設定した直後に発行されるイベント
        /// </summary>
        public event Action<IEnumerable<Card>> CardsInitialized;

        public event Action<Card> CardAdded;
        public event Action<Card> CardRemoved;
        public event Action<Card, CardPile /*from*/, CardPile /*to*/> CardTransferred;

        internal void NotifyCardAdded(Card c)
        {
            CardAdded?.Invoke(c);
        }

        internal void NotifyCardRemoved(Card c)
        {
            CardRemoved?.Invoke(c);
        }

        internal void NotifyCardTransferred(Card c, CardPile from, CardPile to)
        {
            CardTransferred?.Invoke(c, from, to);
        }

        internal void NotifyCardsInitialized()
        {
            // UnityEngine.Debug.Log($"CardPile: NotifyCardsInitialized {Name}");
            CardsInitialized?.Invoke(_cards);
        }
        /*
         * 【コンストラクタのオーバーロードについて】
         * 
         * CardPile には2種類のコンストラクタがあります。
         * 
         * 1. CardPile(string name, int maxCount = int.MaxValue)
         *    → 空の山札を生成し、後からカードを追加する用途向けです。
         *    → 例えばゲーム開始時に空の山を作り、後でカードを配る場合などに利用します。
         * 
         * 2. CardPile(string name, IEnumerable<Card> initialCards, int maxCount = int.MaxValue)
         *    → 生成時に初期カードをまとめてセットしたい場合に使います。
         *    → 例えばデッキ構築やテスト用の山札を一括生成したい場合に便利です。
         *    → Add/Removeがprivateなため、外部から直接カードを追加できない設計でも、
         *       このコンストラクタを使えば初期化時のみカードを安全に追加できます。
         */

        /// <summary>
        /// 初期カードを含む CardPile を生成します（ID必須）。
        /// </summary>
        /// <param name="id">束のID（未設定相当は PileId(0) を使用可）</param>
        /// <param name="name">束の名前（デバッグ用）</param>
        /// <param name="initialCards">初期に含めるカードのコレクション</param>
        /// <param name="maxCount">この束の最大枚数</param>
        public CardPile(PileId id, string name, IEnumerable<Card> initialCards, int maxCount = int.MaxValue)
            : this(id, name, maxCount)
        {
            if (initialCards == null) return;

            foreach (var card in initialCards)
            {
                // private Add を使って初期カードを追加
                if (!Add(card))
                {
                    Debug.LogWarning($"[{Name}] 初期カード追加に失敗: 上限 {maxCount} を超えました。");
                    break;
                }
            }
            // 初期カード設定完了を通知
            NotifyCardsInitialized();
        }


        // 旧: ID省略コンストラクタは廃止（ID必須化）

        /// <summary>
        /// ID付きコンストラクタ（推奨）
        /// </summary>
        public CardPile(PileId id, string name, int maxCount = int.MaxValue)
        {
            Id = id;
            Name = name;
            // カードの束の上限が負だった場合、規定値に設定
            if (maxCount < 0)
            {
                Debug.LogWarning($"[CardPile:{Name}] 不正な maxCount が指定されました: {maxCount}。既定値 int.MaxValue を使用します。");
                _maxCount = int.MaxValue;
            }
            else
            {
                _maxCount = maxCount;
            }
            _cards = new List<Card>(); // ここで実際にカードの束が代入される
        }



        /// <summary>
        /// カードの参照をカードパイルに追加する。上限を超える場合は false を返す。
        /// </summary>
        private bool Add(Card card)
        {
            if (_cards.Count >= _maxCount)
            {
                // 上限超過時の処理
                Debug.LogWarning($"[{Name}] cannot add card: reached maxCount {_maxCount}");
                return false;
            }

            _cards.Add(card);
            NotifyCardAdded(card); // カードが追加されたことをPresenterに通知

            return true;
        }

        /// <summary>
        /// リストからカードの参照を削除する。インスタンスが削除されるわけではない
        /// </summary>
        private bool Remove(Card card)
        {
            if (!_cards.Remove(card))
            {
                Debug.LogWarning($"[{Name}] cannot remove card: card not found");
                return false;
            }
            NotifyCardRemoved(card);
            return true;

        }

        /// <summary>
        /// シャッフル
        /// </summary>
        public void RandomShuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }
        }

        /// <summary>
        /// 決定論的シャッフル（Fisher–Yates）。同じ seed で同じ順序になります。
        /// </summary>
        /// <param name="seed">乱数シード</param>
        public void RandomShuffle(int seed)
        {
            var rng = new System.Random(seed);
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                var tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }
        }

        /// <summary>
        /// カード順序を指定されたリストの順序で再構築する
        /// FixedPlayerStrategyでの特定順序シャッフルなどに使用
        /// </summary>
        /// <param name="orderedCards">新しい順序のカードリスト</param>
        /// <returns>再構築が成功した場合true</returns>
        public bool Reconstruct(IEnumerable<Card> orderedCards)
        {
            if (orderedCards == null)
            {
                Debug.LogWarning($"[{Name}] Reconstruct: orderedCardsがnullです");
                return false;
            }

            var orderedList = orderedCards.ToList();

            // 枚数チェック
            if (orderedList.Count != _cards.Count)
            {
                Debug.LogWarning($"[{Name}] Reconstruct: カード枚数が一致しません。現在:{_cards.Count}枚, 指定:{orderedList.Count}枚");
                return false;
            }

            // 全てのカードが元々このパイルに含まれているかチェック
            foreach (var card in orderedList)
            {
                if (!_cards.Contains(card))
                {
                    Debug.LogWarning($"[{Name}] Reconstruct: 指定されたカードがこのパイルに含まれていません - {card.Suit} {card.Number}");
                    return false;
                }
            }

            // 重複チェック
            if (orderedList.Count != orderedList.Distinct().Count())
            {
                Debug.LogWarning($"[{Name}] Reconstruct: 指定されたリストに重複があります");
                return false;
            }

            // 再構築実行
            _cards.Clear();
            _cards.AddRange(orderedList);

            Debug.Log($"[{Name}] Reconstruct: カード順序を再構築しました - {_cards.Count}枚");

            // 再構築完了を通知
            NotifyCardsInitialized();

            return true;
        }

        /// <summary>
        /// カード順序をカスタムComparisonで並び替える
        /// FixedPlayerStrategyでのスート・ランク順ソートなどに使用
        /// </summary>
        /// <param name="comparison">カードの比較関数</param>
        public void SortCards(Comparison<Card> comparison)
        {
            if (comparison == null)
            {
                Debug.LogWarning($"[{Name}] SortCards: comparisonがnullです");
                return;
            }

            _cards.Sort(comparison);

            Debug.Log($"[{Name}] SortCards: カードをソートしました - {_cards.Count}枚");

            // ソート完了を通知
            NotifyCardsInitialized();
        }

        /// <summary>
        /// 先頭 count 枚を覗く
        /// </summary>
        public IReadOnlyList<Card> Peek(int count)
        {
            return _cards.Take(count).ToList();
        }

        /// <summary>
        /// ドメインサービス: 山札間でカードを移動する
        /// </summary>
        public static class TransferService
        {
            /// <summary>
            /// from から to へ card を移動します。
            /// </summary>
            public static bool Transfer(CardPile from, CardPile to, Card card)
            {
                // 削除
                if (!from.Remove(card))
                {
                    Debug.LogWarning($"[CardPile.TransferService] Failed to remove card from pile '{from.Name}'");
                    return false;
                }
                // 追加
                bool added = to.Add(card);
                if (!added)
                {
                    Debug.LogWarning($"[CardPile.TransferService] Failed to add card to pile '{to.Name}'");
                    from.Add(card);
                    return false;
                }
                // 移動完了通知
                to.NotifyCardTransferred(card, from, to);
                return true;
            }
        }
    }
}
