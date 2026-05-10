using NUnit.Framework;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// CardPile.TransferService の Hands 入退場時の IsSuitVisible 制御を検証するテストクラス
    /// </summary>
    public class CardPileHandsVisibilityEditorTests
    {
        #region Hands Visibility Tests
        [Test]
        public void Transfer_IntoHands_DoesNotChangeSuitVisible()
        {
            var card = CreateCard(cardValue: 11, isFaceUp: false);
            card.SetSuitVisible(false);
            var from = new CardPile(PileIds.PlayerTarget(1), "Target-1", new[] { card });
            var to = new CardPile(PileIds.PlayerHands(1), "Hands-1");

            var transferred = CardPile.TransferService.Transfer(from, to, card);

            Assert.IsTrue(transferred, "Hands への移動は成功するべき");
            Assert.IsFalse(card.IsSuitVisible, "Hands 入場では IsSuitVisible を変更しないべき");
        }

        [Test]
        public void Transfer_OutFromHands_DoesNotChangeSuitVisible()
        {
            var card = CreateCard(cardValue: 12, isFaceUp: false);
            card.SetSuitVisible(true);
            var from = new CardPile(PileIds.PlayerHands(1), "Hands-1", new[] { card });
            var to = new CardPile(PileIds.PlayerTarget(1), "Target-1");

            var transferred = CardPile.TransferService.Transfer(from, to, card);

            Assert.IsTrue(transferred, "Hands からの移動は成功するべき");
            Assert.IsTrue(card.IsSuitVisible, "Hands 退出では IsSuitVisible を変更しないべき");
        }

        [Test]
        public void Transfer_BetweenNonHandsPiles_DoesNotChangeSuitVisible()
        {
            var card = CreateCard(cardValue: 13, isFaceUp: false);
            card.SetSuitVisible(true);
            var from = new CardPile(PileIds.PlayerTarget(1), "Target-1", new[] { card });
            var to = new CardPile(PileIds.PlayerTmp(1), "Tmp-1");

            var transferred = CardPile.TransferService.Transfer(from, to, card);

            Assert.IsTrue(transferred, "非Hands間の移動は成功するべき");
            Assert.IsTrue(card.IsSuitVisible, "非Hands間の移動では IsSuitVisible を変更しないべき");
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// テスト用カードを生成します。
        /// </summary>
        private static Card CreateCard(int cardValue, bool isFaceUp)
        {
            return new Card(new CardId(cardValue), Suit.Spade, number: 1, isFaceUp);
        }
        #endregion
    }
}
