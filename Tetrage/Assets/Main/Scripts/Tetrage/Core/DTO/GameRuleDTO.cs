using System;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.DTO
{
    #region GameRuleDTO
    /// <summary>
    /// ゲームルール設定を表すDTO。プレイヤー数や各スートの構成情報を保持する。
    /// </summary>
    public sealed record GameRuleDTO
    {
        /// <summary>参加プレイヤー数。</summary>
        public int PlayerCount { get; init; }

        /// <summary>スートごとのカード枚数。</summary>
        public IReadOnlyDictionary<Suit, int> CardsPerSuit { get; init; }

        /// <summary>ゲーム内で使用するスートの種類数。</summary>
        public int SuitTypeCount { get; init; }

        /// <summary>ゲームに参加するプレイヤー情報一覧。</summary>
        public IReadOnlyList<PlayerInfo> Participants { get; init; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public GameRuleDTO(
            int playerCount,
            IReadOnlyDictionary<Suit, int> cardsPerSuit,
            int suitTypeCount,
            IReadOnlyList<PlayerInfo> players)
        {
            if (playerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "プレイヤー数は1以上で指定してください。");
            }

            PlayerCount = playerCount;
            CardsPerSuit = cardsPerSuit ?? throw new ArgumentNullException(nameof(cardsPerSuit));
            SuitTypeCount = suitTypeCount;
            Participants = players ?? throw new ArgumentNullException(nameof(players));
        }
    }
    #endregion
}

