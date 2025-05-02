using UnityEngine;
using Tetrage.Models;
using Tetrage.Core;
using System.Collections;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using System.Linq;

namespace Tetrage.Models
{
    public class CardPile : IEnumerable<Card>
    {
        // 内部のカードリスト
        private readonly List<Card> _cards;

        // カードの束の最大枚数
        private readonly int _maxCount;

        // 束の名称と所有者（プロパティ。ローカルフィールドも自動生成される）
        public string Name { get; }
        public CardOwner OwnerType { get; }


        // IEnumerableを実装するためのメンバその１：IEnumerator<T> を返す GetEnumerator()
        public IEnumerator<Card> GetEnumerator()
        {
            return _cards.GetEnumerator();
        }

        // IEnumerableを実装するためのメンバその2：非ジェネリック版 IEnumerator を返す GetEnumerator()
        // 明示的インターフェイスの実装なので、privateになっています（ここら辺よく分かんないけど、よく分かんなくていいっぽい）
        // 重要なのは、これによってCardPileが列挙可能になり、LINQが仕えるようになるということ、だと思う
        IEnumerator IEnumerable.GetEnumerator() { 
            return GetEnumerator(); 
        }


        /// <summary>
        /// この束に含まれるカードの枚数
        /// </summary>
        public int Count => _cards.Count;


        /// <summary>
        /// コンストラクタで maxCount を指定
        /// </summary>
        /// <param name="name">束の名前（デバッグ用）</param>
        /// <param name="owner">所有者の判定（PlayerかStageかなど））</param>
        /// <param name="maxCount">この束の最大枚数（上限なしなら int.MaxValue）</param>
        public CardPile(string name,  CardOwner ownerType = CardOwner.Null, int maxCount = int.MaxValue)
        {
            Name = name;
            OwnerType = ownerType;
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
        /// カードを追加する上限を超える場合は false を返す。
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
            return true;
        }

        /// <summary>
        /// カードを削除する
        /// </summary>
        private bool Remove(Card card)
        {
            return _cards.Remove(card); // リストがからの場合はfalseが返されます。
        }

        /// <summary>
        /// 他の束へ移動する。カードの移動は必ずこの関数を用いる
        /// </summary>
        public bool TransferTo(CardPile targetPile, Card card)
        {
            if (!Remove(card)) return false;
            return targetPile.Add(card);
        }

        /// <summary>
        /// シャッフル
        /// </summary>
        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }
        }

        /// <summary>
        /// 先頭 count 枚を覗く
        /// </summary>
        public IReadOnlyList<Card> Peek(int count) // いらない可能性がある
        {
            return _cards.Take(count).ToList();
        }

    }
}
