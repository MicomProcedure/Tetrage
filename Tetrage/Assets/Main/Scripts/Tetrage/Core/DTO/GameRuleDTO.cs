using System;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Core.Constants;

namespace Tetrage.Core.DTO
{
    #region GameRuleDTO
    /// <summary>
    /// ゲームルール設定を表すDTO。プレイヤー数や各スートの構成情報を保持する。
    /// </summary>
    public sealed record GameRuleDTO
    {
        /// <summary>参加プレイヤー数。</summary>
        public int PlayerCount { get; private set; }

        /// <summary>スートごとのカード枚数。</summary>
        public int CardCountPerSuit { get; private set; }

        /// <summary>ゲーム内で使用するスートの種類数。</summary>
        public int SuitTypeCount { get; private set; }


        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public GameRuleDTO(
            int playerCount,
            int cardCountPerSuit,
            int suitTypeCount)
        {
            ValidateGameRule(playerCount, cardCountPerSuit, suitTypeCount);

            PlayerCount = playerCount;
            CardCountPerSuit = cardCountPerSuit;
            SuitTypeCount = suitTypeCount;
        }
        #endregion

        #region Factory Methods

        public GameRuleDTO Default => new GameRuleDTO(
            playerCount: InGameConsts.DEFAULT_GAME_PLAYER_COUNT,
            cardCountPerSuit: InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT,
            suitTypeCount: InGameConsts.DEFAULT_INITIAL_SUITS.Length
        );

        #endregion

        #region Private Validation Methods

        private void ValidateGameRule(int playerCount, int cardCountPerSuit, int suitTypeCount)
        {
            ValidatePlayerCount(playerCount);
            ValidateCardCountPerSuit(cardCountPerSuit);
            ValidateSuitTypeCount(suitTypeCount);
        }

        private void ValidatePlayerCount(int playerCount)
        {
            if (playerCount <= SettingConsts.MIN_PLAYER_COUNT || playerCount > SettingConsts.MAX_PLAYER_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "プレイヤー数は1以上で指定してください。");
            }
        }

        private void ValidateCardCountPerSuit(int cardCountPerSuit)
        {
            if (cardCountPerSuit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardCountPerSuit), "カード枚数は1以上で指定してください。");
            }
        }

        private void ValidateSuitTypeCount(int suitTypeCount)
        {
            if (suitTypeCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(suitTypeCount), "スートの種類数は1以上で指定してください。");
            }
        }

        #endregion
    }
}
