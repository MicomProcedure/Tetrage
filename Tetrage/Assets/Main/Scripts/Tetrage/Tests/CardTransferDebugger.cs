using UnityEngine;
using System.Linq;
using Tetrage.Models;

namespace Tetrage.Tests
{
    /// <summary>
    /// CardPile 間のカード転送ロジックをデバッグするクラスだ
    /// </summary>
    public class CardTransferDebugger : MonoBehaviour
    {
        private FactoryDebugRunner _runner;

        private void Start()
        {
            // FactoryDebugRunner が同じ GameObject にアタッチされていることを想定
            _runner = GetComponent<FactoryDebugRunner>();
            if (_runner == null)
            {
                Debug.LogWarning("CardTransferDebugger: FactoryDebugRunner がアタッチされていない");
            }
        }

        [ContextMenu("Transfer First Card To Next Pile")]
        private void TransferFirstCardToNextPile()
        {
            // 最初の山札から次の山札へ転送
            TransferFirstCard(0, 1);
        }

        [ContextMenu("Transfer First Card To Previous Pile")]
        private void TransferFirstCardToPreviousPile()
        {
            // 次の山札から前の山札へ転送
            TransferFirstCard(1, 0);
        }

        /// <summary>
        /// 指定したインデックスの山札間でカードを転送する
        /// </summary>
        /// <param name="fromIndex">転送元の山札インデックス</param>
        /// <param name="toIndex">転送先の山札インデックス</param>
        private void TransferFirstCard(int fromIndex, int toIndex)
        {
            if (_runner == null)
            {
                Debug.LogWarning("FactoryDebugRunner が見つからないため実行できない");
                return;
            }

            var piles = _runner.CardPiles;
            if (piles == null || piles.Count < 2)
            {
                Debug.LogWarning("2つ以上のカードパイルを生成してから実行してください");
                return;
            }

            var from = piles[fromIndex];
            var to = piles[toIndex];
            var card = from.Cards.FirstOrDefault();
            if (card == null)
            {
                Debug.LogWarning($"[{from.Name}] 転送するカードが存在しない");
                return;
            }

            bool success = CardPile.TransferService.Transfer(from, to, card);
            if (success)
            {
                Debug.Log($"CardTransferDebugger: カード {card.Suit} {card.Number} を '{from.Name}' から '{to.Name}' へ転送した");
            }
            else
            {
                Debug.LogWarning("CardTransferDebugger: カード転送に失敗した");
            }
        }
        
    }
} 