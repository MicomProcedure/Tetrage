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
        /// IsUserPlayerがtrueのプレイヤーのインデックスを取得（最初の一人のみ）
        /// 複数指定されている場合は最初の一人を返す
        /// </summary>
        public int? GetUserPlayerIndex(int playerCount)
        {
            for (int i = 0; i < playerCount && i < _debugPlayerInfos.Count; i++)
            {
                if (_debugPlayerInfos[i].IsUserPlayer)
                {
                    return i;
                }
            }
            return null;
        }

        /// <summary>
        /// IsUserPlayerがtrueのプレイヤーの数を取得
        /// </summary>
        public int GetUserPlayerCount(int playerCount)
        {
            int count = 0;
            for (int i = 0; i < playerCount && i < _debugPlayerInfos.Count; i++)
            {
                if (_debugPlayerInfos[i].IsUserPlayer)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// デバッグ設定に基づいてPlayerInfoのリストを作成
        /// </summary>
        public List<PlayerInfo> CreatePlayerInfos(int playerCount, int? userPlayerIndex = null)
        {
            var playerInfos = new List<PlayerInfo>();

            // userPlayerIndexが指定されていない場合は、IsUserPlayerから自動検出
            if (userPlayerIndex == null)
            {
                userPlayerIndex = GetUserPlayerIndex(playerCount);
            }

            // 使用済みPlayerIdを追跡するためのHashSet（ループの外で定義）
            var usedPlayerIds = new HashSet<int>();

            for (int i = 0; i < playerCount; i++)
            {
                DebugPlayerInfo debugInfo = null;
                if (i < _debugPlayerInfos.Count)
                {
                    debugInfo = _debugPlayerInfos[i];
                }

                // PlayerIdはDebugPlayerInfoから取得、0の場合はインデックスベースで自動生成
                int playerIdValue;
                if (debugInfo != null && debugInfo.PlayerId > 0)
                {
                    playerIdValue = debugInfo.PlayerId;
                    if (usedPlayerIds.Contains(playerIdValue))
                    {
                        Debug.LogWarning($"GameScenePlayerDebugSettings: PlayerId {playerIdValue} が重複しています。自動生成に切り替えます。");
                        // 重複している場合は自動生成に切り替え
                        playerIdValue = FindNextAvailablePlayerId(usedPlayerIds, playerCount);
                    }
                }
                else
                {
                    // 自動生成: インデックス + 1から開始し、使用済みでない最初の値を探す
                    int candidateValue = i + 1;
                    if (usedPlayerIds.Contains(candidateValue))
                    {
                        playerIdValue = FindNextAvailablePlayerId(usedPlayerIds, playerCount);
                    }
                    else
                    {
                        playerIdValue = candidateValue;
                    }
                }

                // 使用済みとして登録
                usedPlayerIds.Add(playerIdValue);

                // PlayerTypeはDebugPlayerInfoから取得、なければデフォルト値
                var playerType = debugInfo?.PlayerType ?? PlayerType.Remote;

                // UserPlayerの場合はPlayerTypeをLocalに上書き
                if (i == userPlayerIndex)
                {
                    playerType = PlayerType.Local;
                }

                var playerInfo = new PlayerInfo
                {
                    Id = new PlayerId(playerIdValue),
                    UserId = debugInfo?.PlayerName ?? $"Player {i + 1}",
                    PlayerType = playerType,
                    PlayerIconIndex = debugInfo?.IconIndex ?? (i % 4)
                };

                playerInfos.Add(playerInfo);
            }

            return playerInfos;
        }

        /// <summary>
        /// 使用可能な次のPlayerIdを検索
        /// </summary>
        private int FindNextAvailablePlayerId(HashSet<int> usedIds, int playerCount)
        {
            // 1から playerCount までの範囲で使用可能なIDを探す
            for (int candidate = 1; candidate <= playerCount; candidate++)
            {
                if (!usedIds.Contains(candidate))
                {
                    return candidate;
                }
            }

            // すべて使用済みの場合は、playerCount + 1以降を探す
            int nextId = playerCount + 1;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }
            return nextId;
        }
    }
}
