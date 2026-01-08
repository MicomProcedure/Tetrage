using System;
using System.Collections.Generic;

namespace Tetrage.Core.DTO
{
    public static class GameRuleDTOExtensions
    {
        // 参加者数とゲームルールのプレイヤー数が一致しているかを検証する
        public static void ValidateParticipantCount(this GameRuleDTO gameRuleDTO, IReadOnlyList<PlayerInfo> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                throw new ArgumentException("参加者情報が設定されていません。");
            }
            if (participants.Count != gameRuleDTO.PlayerCount)
            {
                throw new ArgumentException("参加者数がゲームルールのプレイヤー数と一致しません。");
            }
        }
    }
}