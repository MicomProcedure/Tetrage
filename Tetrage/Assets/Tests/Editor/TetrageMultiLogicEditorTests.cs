using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core.Actions;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// TetrageMulti の純粋ロジックを検証する。
    /// 対象: TetrageMultiResultCalculator（応答収集フォールバック・勝敗判定）
    /// UI/ネットワーク/アニメーションは対象外。
    /// </summary>
    public class TetrageMultiLogicEditorTests
    {
        // ─── テスト用 PlayerId ───────────────────────────────────────────────
        private static readonly PlayerId Requester = new PlayerId(1);
        private static readonly PlayerId Player2   = new PlayerId(2);
        private static readonly PlayerId Player3   = new PlayerId(3);
        private static readonly PlayerId Player4   = new PlayerId(4);

        // ═══════════════════════════════════════════════════════════════════
        // DetermineOpenPlayers
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void DetermineOpenPlayers_ResponseOpen_IsIncluded()
        {
            var selected  = new List<PlayerId> { Player2, Player3 };
            var responses = new Dictionary<PlayerId, bool>
            {
                { Player2, true },
                { Player3, true },
            };

            var result = TetrageMultiResultCalculator.DetermineOpenPlayers(selected, responses);

            Assert.Contains(Player2, result);
            Assert.Contains(Player3, result);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void DetermineOpenPlayers_ResponseDecline_IsExcluded()
        {
            var selected  = new List<PlayerId> { Player2, Player3 };
            var responses = new Dictionary<PlayerId, bool>
            {
                { Player2, false }, // 出さない
                { Player3, true },
            };

            var result = TetrageMultiResultCalculator.DetermineOpenPlayers(selected, responses);

            CollectionAssert.DoesNotContain(result, Player2);
            Assert.Contains(Player3, result);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void DetermineOpenPlayers_NoResponse_FallsBackToOpen()
        {
            var selected  = new List<PlayerId> { Player2, Player3 };
            // Player3 は未応答（タイムアウト）
            var responses = new Dictionary<PlayerId, bool>
            {
                { Player2, true },
            };

            var result = TetrageMultiResultCalculator.DetermineOpenPlayers(selected, responses);

            // 未応答は「出す」扱いのためどちらも含まれる
            Assert.Contains(Player2, result);
            Assert.Contains(Player3, result);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void DetermineOpenPlayers_AllDecline_ReturnsEmpty()
        {
            var selected  = new List<PlayerId> { Player2, Player3 };
            var responses = new Dictionary<PlayerId, bool>
            {
                { Player2, false },
                { Player3, false },
            };

            var result = TetrageMultiResultCalculator.DetermineOpenPlayers(selected, responses);

            Assert.AreEqual(0, result.Count);
        }

        // ═══════════════════════════════════════════════════════════════════
        // IsSuccess
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void IsSuccess_AllSameSuit_ReturnsTrue()
        {
            var openSuits = new List<Suit> { Suit.Heart, Suit.Heart };

            Assert.IsTrue(TetrageMultiResultCalculator.IsSuccess(Suit.Heart, openSuits));
        }

        [Test]
        public void IsSuccess_DifferentSuit_ReturnsFalse()
        {
            var openSuits = new List<Suit> { Suit.Heart, Suit.Spade };

            Assert.IsFalse(TetrageMultiResultCalculator.IsSuccess(Suit.Heart, openSuits));
        }

        [Test]
        public void IsSuccess_EmptyOpenPlayers_ReturnsFalse()
        {
            Assert.IsFalse(TetrageMultiResultCalculator.IsSuccess(Suit.Heart, new List<Suit>()));
        }

        // ═══════════════════════════════════════════════════════════════════
        // CalculateSuccessWinners
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void CalculateSuccessWinners_ReturnsRequesterAndOpenPlayers()
        {
            var open = new List<PlayerId> { Player2, Player3 };

            var result = TetrageMultiResultCalculator.CalculateSuccessWinners(Requester, open);

            Assert.Contains(Requester, result);
            Assert.Contains(Player2, result);
            Assert.Contains(Player3, result);
            Assert.AreEqual(3, result.Count);
        }

        // ═══════════════════════════════════════════════════════════════════
        // CalculateFailureWinners
        // ═══════════════════════════════════════════════════════════════════

        [Test]
        public void CalculateFailureWinners_OpenPlayerDifferentSuit_Wins()
        {
            // Requester=Heart, Player2=Spade(出す), Player3=Heart(非選択)
            var all = new List<(PlayerId, Suit)>
            {
                (Requester, Suit.Heart),
                (Player2,   Suit.Spade),
                (Player3,   Suit.Heart),
            };
            var openPlayers = new List<PlayerId> { Player2 };

            var result = TetrageMultiResultCalculator.CalculateFailureWinners(
                Requester, Suit.Heart, all, openPlayers);

            // Player2: 出した & Requesterとスート違い → 勝者
            Assert.Contains(Player2, result);
            // Player3: 出さなかった & open参加者スート(Heart, Spade)のいずれかに一致(Heart) → 勝者にならない
            CollectionAssert.DoesNotContain(result, Player3);
            // Requester: 失敗時は勝者にならない
            CollectionAssert.DoesNotContain(result, Requester);
        }

        [Test]
        public void CalculateFailureWinners_NonOpenPlayerDistinctSuit_Wins()
        {
            // Requester=Heart, Player2=Heart(出す), Player3=Heart(出さない), Player4=Diamond(出さない)
            // openSuits = { Heart }
            var all = new List<(PlayerId, Suit)>
            {
                (Requester, Suit.Heart),
                (Player2,   Suit.Heart),
                (Player3,   Suit.Heart),   // Heart = openSuitsに含まれる → 勝者にならない
                (Player4,   Suit.Diamond), // Diamond = openSuitsに含まれない → 勝者
            };
            var openPlayers = new List<PlayerId> { Player2 }; // Player2 のみ出す

            var result = TetrageMultiResultCalculator.CalculateFailureWinners(
                Requester, Suit.Heart, all, openPlayers);

            // Player2: 出した & Requesterと同スート(Heart) → 勝者にならない
            CollectionAssert.DoesNotContain(result, Player2);
            // Player3: 出さなかった & openスート(Heart)に一致 → 勝者にならない
            CollectionAssert.DoesNotContain(result, Player3);
            // Player4: 出さなかった & openスート(Heart)と異なる(Diamond) → 勝者
            Assert.Contains(Player4, result);
            // Requester: 失敗時は対象外
            CollectionAssert.DoesNotContain(result, Requester);
        }

        [Test]
        public void CalculateFailureWinners_AllDeclineSameSuitAsOpen_NoWinners()
        {
            // Requester=Heart, Player2=Heart(出す), Player3=Heart(出さない)
            var all = new List<(PlayerId, Suit)>
            {
                (Requester, Suit.Heart),
                (Player2,   Suit.Heart),
                (Player3,   Suit.Heart),
            };
            var openPlayers = new List<PlayerId> { Player2 };

            var result = TetrageMultiResultCalculator.CalculateFailureWinners(
                Requester, Suit.Heart, all, openPlayers);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CalculateFailureWinners_RequesterNotInWinners()
        {
            // 宣言者は失敗時に絶対に勝者にならない
            var all = new List<(PlayerId, Suit)>
            {
                (Requester, Suit.Heart),
                (Player2,   Suit.Spade),
            };
            var openPlayers = new List<PlayerId> { Player2 };

            var result = TetrageMultiResultCalculator.CalculateFailureWinners(
                Requester, Suit.Heart, all, openPlayers);

            CollectionAssert.DoesNotContain(result, Requester);
        }
    }
}
