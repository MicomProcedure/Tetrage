using NUnit.Framework;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// CardPile.TransferService が Tmp 入退場時の表裏制御を持たないことを検証するテストクラス
    /// </summary>
    public class CardPileTmpTransferEditorTests
    {
        #region Tmp Transfer FaceUp Tests
        [Test]
        public void Transfer_IntoTmp_DoesNotChangeFaceUp()
        {
            var card = CreateCard(cardValue: 1, isFaceUp: false);
            var from = new CardPile(PileIds.PlayerHands(1), "Hands-1", new[] { card });
            var tmp = new CardPile(PileIds.PlayerTmp(1), "Tmp-1");

            var transferred = CardPile.TransferService.Transfer(from, tmp, card);

            Assert.IsTrue(transferred, "Tmp への移動は成功するべき");
            Assert.IsFalse(card.IsFaceUp, "TransferService は Tmp 入場時に IsFaceUp を変更しないべき");
        }

        [Test]
        public void Transfer_OutFromTmp_DoesNotChangeFaceDownCard()
        {
            var card = CreateCard(cardValue: 2, isFaceUp: false);
            var from = new CardPile(PileIds.PlayerHands(1), "Hands-1", new[] { card });
            var tmp = new CardPile(PileIds.PlayerTmp(1), "Tmp-1");
            var to = new CardPile(PileIds.PlayerTarget(1), "Target-1");

            Assert.IsTrue(CardPile.TransferService.Transfer(from, tmp, card), "Tmp への移動は成功するべき");
            Assert.IsFalse(card.IsFaceUp, "TransferService は Tmp 入場時に IsFaceUp を変更しないべき");

            var transferred = CardPile.TransferService.Transfer(tmp, to, card);

            Assert.IsTrue(transferred, "Tmp からの移動は成功するべき");
            Assert.IsFalse(card.IsFaceUp, "TransferService は Tmp 退場時に IsFaceUp を変更しないべき");
        }

        [Test]
        public void Transfer_OutFromTmp_DoesNotChangeFaceUpCard()
        {
            var card = CreateCard(cardValue: 3, isFaceUp: true);
            var from = new CardPile(PileIds.PlayerHands(1), "Hands-1", new[] { card });
            var tmp = new CardPile(PileIds.PlayerTmp(1), "Tmp-1");
            var to = new CardPile(PileIds.PlayerTarget(1), "Target-1");

            Assert.IsTrue(CardPile.TransferService.Transfer(from, tmp, card), "Tmp への移動は成功するべき");
            Assert.IsTrue(card.IsFaceUp, "TransferService は Tmp 入場時に IsFaceUp を変更しないべき");

            var transferred = CardPile.TransferService.Transfer(tmp, to, card);

            Assert.IsTrue(transferred, "Tmp からの移動は成功するべき");
            Assert.IsTrue(card.IsFaceUp, "TransferService は Tmp 退場時に IsFaceUp を変更しないべき");
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
