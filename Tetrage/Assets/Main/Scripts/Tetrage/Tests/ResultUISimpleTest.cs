using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Tetrage.UI;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Tests
{
    /// <summary>
    /// ResultUI の動作確認用シンプルテストスクリプト
    /// GameManager や Dealer に依存せず、必要な情報を直接指定してテスト可能
    /// </summary>
    public class ResultUISimpleTest : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private ResultUI resultUI;

        [Header("Test Data Settings")]
        [SerializeField]
        private List<TestPlayerData> testPlayers = new List<TestPlayerData>
        {
            new TestPlayerData { UserId = "Mira.", TargetSuit = Suit.Spade, IconIndex = 0 },
            new TestPlayerData { UserId = "Takakusaki", TargetSuit = Suit.Heart, IconIndex = 1 },
            new TestPlayerData { UserId = "Yuto77", TargetSuit = Suit.Diamond, IconIndex = 2 },
            new TestPlayerData { UserId = "283", TargetSuit = Suit.Club, IconIndex = 3 }
        };

        #endregion

        #region Test Player Data Class

        /// <summary>
        /// テスト用プレイヤーデータ
        /// </summary>
        [System.Serializable]
        public class TestPlayerData
        {
            public string UserId;
            public Suit TargetSuit;
            public int IconIndex;
        }

        /// <summary>
        /// IPlayer のテストダブル（モック）
        /// ResultUI のテストに必要な最小限の情報のみ実装
        /// </summary>
        private class MockPlayer : IPlayer
        {
            public string UserId { get; set; }
            public int PlayerId { get; set; }
            public PlayerId Id { get; set; }
            public int IconIndex { get; set; }
            public CardPile Target { get; set; }
            public CardPile Hands { get; set; }
            public CardPile Tmp { get; set; }
            public bool IsReach { get; set; }

            public void Reach() { }

            public MockPlayer(string userId, int playerId, Suit targetSuit)
            {
                UserId = userId;
                PlayerId = playerId;

                // Target カードパイルを作成（1枚のカードを持つ）
                var card = new Card(new CardId(playerId * 100), targetSuit, 1, true); // スートのみ重要、数字は仮で1
                Target = new CardPile(Tetrage.Core.Ids.PileIds.PlayerTarget(playerId), $"Target_{userId}", new[] { card }, 1);

                // Hands と Tmp は空で初期化（ResultUI では使用しない）
                Hands = new CardPile(Tetrage.Core.Ids.PileIds.PlayerHands(playerId), $"Hands_{userId}", 0);
                Tmp = new CardPile(Tetrage.Core.Ids.PileIds.PlayerTmp(playerId), $"Tmp_{userId}", 0);
            }
        }

        #endregion

        #region Context Menu Tests

        /// <summary>
        /// テスト: プレイヤー1,2が勝者、プレイヤー3,4が敗者
        /// </summary>
        [ContextMenu("Test: 2 Winners, 2 Losers")]
        public void Test_TwoWinners_TwoLosers()
        {
            if (!ValidateReferences()) return;

            if (testPlayers.Count < 4)
            {
                Debug.LogError("ResultUISimpleTest: テストプレイヤーが4人必要です");
                return;
            }

            var players = CreateMockPlayers();
            int[] winners = new int[]
            {
                players[0].Id.Value,
                players[1].Id.Value
            };

            Debug.Log("=== Test: 2 Winners, 2 Losers ===");
            Debug.Log($"勝者: {string.Join(", ", winners)}");
            LogPlayerInfo();

            resultUI.DisplayResult(winners, players);

            Debug.Log("=== Test Complete ===");
        }

        /// <summary>
        /// テスト: 全員勝者
        /// </summary>
        [ContextMenu("Test: All Winners")]
        public void Test_AllWinners()
        {
            if (!ValidateReferences()) return;

            var players = CreateMockPlayers();
            int[] winners = players.Select(p => p.Id.Value).ToArray();

            Debug.Log("=== Test: All Winners ===");
            LogPlayerInfo();
            resultUI.DisplayResult(winners, players);
            Debug.Log("=== Test Complete ===");
        }

        /// <summary>
        /// テスト: 全員敗者
        /// </summary>
        [ContextMenu("Test: All Losers")]
        public void Test_AllLosers()
        {
            if (!ValidateReferences()) return;

            var players = CreateMockPlayers();

            Debug.Log("=== Test: All Losers ===");
            LogPlayerInfo();
            resultUI.DisplayResult(System.Array.Empty<int>(), players);
            Debug.Log("=== Test Complete ===");
        }

        /// <summary>
        /// 結果をクリア
        /// </summary>
        [ContextMenu("Clear Result")]
        public void ClearResult()
        {
            if (resultUI == null)
            {
                Debug.LogError("ResultUISimpleTest: ResultUI が設定されていません");
                return;
            }

            Debug.Log("結果をクリアします");
            resultUI.ClearAllLists();
            resultUI.HideResult();
        }

        /// <summary>
        /// 結果を表示
        /// </summary>
        [ContextMenu("Show Result")]
        public void ShowResult()
        {
            if (resultUI == null)
            {
                Debug.LogError("ResultUISimpleTest: ResultUI が設定されていません");
                return;
            }

            resultUI.gameObject.SetActive(true);
            Debug.Log("結果を表示しました");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 参照が正しく設定されているかチェック
        /// </summary>
        private bool ValidateReferences()
        {
            if (resultUI == null)
            {
                Debug.LogError("ResultUISimpleTest: ResultUI が設定されていません");
                return false;
            }

            if (testPlayers == null || testPlayers.Count == 0)
            {
                Debug.LogError("ResultUISimpleTest: testPlayers が設定されていません");
                return false;
            }

            return true;
        }

        /// <summary>
        /// テストデータからモックプレイヤーを作成
        /// </summary>
        private IReadOnlyList<IPlayer> CreateMockPlayers()
        {
            var players = new List<IPlayer>();

            for (int i = 0; i < testPlayers.Count; i++)
            {
                var testData = testPlayers[i];
                var mockPlayer = new MockPlayer(testData.UserId, i + 1, testData.TargetSuit);
                mockPlayer.IconIndex = testData.IconIndex;
                players.Add(mockPlayer);
            }

            return players;
        }

        // アイコンマップは不要（ResultUIがIPlayer.IconIndexを参照）

        /// <summary>
        /// 現在のテストプレイヤー情報をログ出力（デバッグ用）
        /// </summary>
        [ContextMenu("Log Player Info")]
        private void LogPlayerInfo()
        {
            Debug.Log("=== Test Player Info ===");
            for (int i = 0; i < testPlayers.Count; i++)
            {
                var player = testPlayers[i];
                Debug.Log($"Player {i}: UserId={player.UserId}, Target Suit={player.TargetSuit}, IconIndex={player.IconIndex}");
            }
        }

        #endregion
    }
}