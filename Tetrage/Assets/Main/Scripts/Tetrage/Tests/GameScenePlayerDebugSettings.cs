using UnityEngine;
using System.Collections.Generic;
using Tetrage.Core.DTO;
using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
using Tetrage.Tests.Data;

namespace Tetrage.Tests
{
    public class GameScenePlayerDebugSettings : MonoBehaviour
    {
        [SerializeField] private List<DebugPlayerInfo> _debugPlayerInfos = new List<DebugPlayerInfo>();

        public List<DebugPlayerInfo> DebugPlayerInfos => _debugPlayerInfos;

        /// <summary>
        /// デバッグ設定に基づいてPlayerInfoのリストを作成
        /// </summary>
        public List<PlayerInfo> CreatePlayerInfos(int playerCount, int localPlayerIndex)
        {
            var playerInfos = new List<PlayerInfo>();

            for (int i = 0; i < playerCount; i++)
            {
                DebugPlayerInfo debugInfo = null;
                if (i < _debugPlayerInfos.Count)
                {
                    debugInfo = _debugPlayerInfos[i];
                }

                var playerInfo = new PlayerInfo
                {
                    Id = new PlayerId(i + 1), // ActorNumber starts at 1 usually
                    UserId = debugInfo?.PlayerName ?? $"Player {i + 1}",
                    PlayerType = (i == localPlayerIndex) ? PlayerType.Local : PlayerType.Remote,
                    PlayerIconIndex = debugInfo?.IconIndex ?? (i % 4)
                };

                playerInfos.Add(playerInfo);
            }

            return playerInfos;
        }
    }
}
