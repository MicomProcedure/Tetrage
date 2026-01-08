using System.Collections.Generic;
using UnityEngine;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Managers;
using Tetrage.Network;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Constants;

namespace Tetrage.Tests.PlayMode
{
    /// <summary>
    /// PlayModeテスト用の静的ヘルパークラス。
    /// テストコード側でPlayerInfo等を自由に構築し、必要な初期化処理を提供する。
    /// PlayModeTestHarnessよりも柔軟性が高く、詳細な制御が可能。
    /// </summary>
    public static class PlayModeTestHelper
    {
        #region GameManager管理

        /// <summary>
        /// シーン内のGameManagerを検索して取得
        /// </summary>
        public static GameManager FindGameManager()
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                throw new System.Exception("PlayModeTestHelper: GameManagerがシーンに見つかりません");
            }
            Debug.Log("PlayModeTestHelper: GameManagerを発見しました");
            return gm;
        }

        #endregion

        #region NetworkContext生成

        /// <summary>
        /// NetworkContextを生成
        /// </summary>
        /// <param name="mode">ネットワークモード</param>
        /// <param name="localActorNumber">ローカルプレイヤーのActorNumber（デフォルト: 1）</param>
        /// <param name="playerCount">プレイヤー総数（デフォルト: 4）</param>
        /// <param name="isHost">ホストかどうか（デフォルト: true）</param>
        public static INetworkContext CreateNetworkContext(
            NetworkMode mode,
            int localActorNumber = 1,
            int playerCount = 4,
            bool isHost = true)
        {
            switch (mode)
            {
                case NetworkMode.VirtualTransport:
                case NetworkMode.LogicInjection:
                    Debug.Log($"PlayModeTestHelper: VirtualNetworkContext生成 (ActorNumber: {localActorNumber}, PlayerCount: {playerCount})");
                    return new VirtualNetworkContext(
                        actorNumber: localActorNumber,
                        isHost: isHost,
                        playerCount: playerCount,
                        isReady: true,
                        isInRoom: true
                    );

                case NetworkMode.RealPhoton:
                    Debug.LogWarning("PlayModeTestHelper: RealPhotonモードはテストでは非推奨です");
                    return new PhotonNetworkContext();

                default:
                    throw new System.ArgumentException($"未対応のNetworkMode: {mode}");
            }
        }

        #endregion

        #region PlayerIdMapper生成

        /// <summary>
        /// PlayerIdMapperを生成（PlayerInfo配列から自動生成）
        /// PlayerId.Value と ActorNumber を1:1でマッピング
        /// </summary>
        public static IPlayerIdMapper CreatePlayerIdMapper(List<PlayerInfo> players)
        {
            var mapper = new PlayerIdMapper();
            foreach (var player in players)
            {
                // PlayerInfo.IdをPlayerIdとして、ActorNumberは同じ値を使用
                mapper.Register(player.Id, player.Id.Value);
            }
            Debug.Log($"PlayModeTestHelper: PlayerIdMapper生成完了 ({players.Count}人)");
            return mapper;
        }

        /// <summary>
        /// PlayerIdMapperを生成（カスタムマッピング用）
        /// </summary>
        /// <param name="mappings">PlayerId.Value と ActorNumber のペア</param>
        public static IPlayerIdMapper CreatePlayerIdMapper(params (int playerId, int actorNumber)[] mappings)
        {
            var mapper = new PlayerIdMapper();
            foreach (var (playerId, actorNumber) in mappings)
            {
                mapper.Register(new PlayerId(playerId), actorNumber);
            }
            Debug.Log($"PlayModeTestHelper: PlayerIdMapper生成完了 ({mappings.Length}マッピング)");
            return mapper;
        }

        #endregion

        #region PlayerInfo生成

        /// <summary>
        /// デフォルトのPlayerInfo配列を生成（簡易テスト用）
        /// </summary>
        /// <param name="count">プレイヤー数（2-4）</param>
        /// <param name="localPlayerIndex">ローカルプレイヤーのインデックス（0始まり、デフォルト: 0）</param>
        public static List<PlayerInfo> CreateDefaultPlayers(
            int count,
            int localPlayerIndex = 0)
        {
            if (count < SettingConsts.MIN_PLAYER_COUNT || count > SettingConsts.MAX_PLAYER_COUNT)
            {
                throw new System.ArgumentException(
                    $"プレイヤー数は{SettingConsts.MIN_PLAYER_COUNT}-{SettingConsts.MAX_PLAYER_COUNT}の範囲で指定してください");
            }

            if (localPlayerIndex < 0 || localPlayerIndex >= count)
            {
                throw new System.ArgumentException($"localPlayerIndexは0-{count - 1}の範囲で指定してください");
            }

            var list = new List<PlayerInfo>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new PlayerInfo
                {
                    Id = new PlayerId(i + 1),
                    UserId = $"TestPlayer_{i + 1}",
                    PlayerType = i == localPlayerIndex ? PlayerType.Local : PlayerType.Remote,
                    PlayerIconIndex = i % 4
                });
            }

            Debug.Log($"PlayModeTestHelper: デフォルトPlayerInfo生成完了 ({count}人, Local: Player{localPlayerIndex + 1})");
            return list;
        }

        /// <summary>
        /// カスタムPlayerInfo配列を生成（詳細制御用）
        /// </summary>
        /// <param name="players">各プレイヤーの設定（UserId, PlayerType, IconIndex）</param>
        public static List<PlayerInfo> CreateCustomPlayers(params (string userId, PlayerType type, int iconIndex)[] players)
        {
            if (players.Length < SettingConsts.MIN_PLAYER_COUNT || players.Length > SettingConsts.MAX_PLAYER_COUNT)
            {
                throw new System.ArgumentException(
                    $"プレイヤー数は{SettingConsts.MIN_PLAYER_COUNT}-{SettingConsts.MAX_PLAYER_COUNT}の範囲で指定してください");
            }

            var list = new List<PlayerInfo>();
            for (int i = 0; i < players.Length; i++)
            {
                var (userId, type, iconIndex) = players[i];
                list.Add(new PlayerInfo
                {
                    Id = new PlayerId(i + 1),
                    UserId = userId,
                    PlayerType = type,
                    PlayerIconIndex = iconIndex
                });
            }

            Debug.Log($"PlayModeTestHelper: カスタムPlayerInfo生成完了 ({players.Length}人)");
            return list;
        }

        /// <summary>
        /// UserPlayerを取得（INetworkContext.UserActorNumberから特定）
        /// </summary>
        /// <param name="players">PlayerInfoリスト</param>
        /// <param name="networkContext">ネットワークコンテキスト</param>
        /// <param name="mapper">PlayerIdMapper</param>
        public static PlayerInfo GetUserPlayer(
            List<PlayerInfo> players,
            INetworkContext networkContext,
            IPlayerIdMapper mapper)
        {
            // UserActorNumberからPlayerIdを取得
            var localActorNumber = networkContext.UserActorNumber;
            if (!mapper.TryGetPlayerId(localActorNumber, out var localPlayerId))
            {
                throw new System.Exception($"PlayModeTestHelper: ActorNumber={localActorNumber}に対応するPlayerIdが見つかりません");
            }

            // PlayerIdでPlayerInfoを検索
            var userPlayer = players.Find(p => p.Id.Equals(localPlayerId));
            if (userPlayer.Id.Value == 0)  // PlayerInfoはstructなので、デフォルト値チェック
            {
                throw new System.Exception($"PlayModeTestHelper: PlayerId={localPlayerId.Value}のPlayerInfoが見つかりません");
            }

            Debug.Log($"PlayModeTestHelper: UserPlayer特定 (ActorNumber: {localActorNumber} → PlayerId: {localPlayerId.Value})");
            return userPlayer;
        }

        /// <summary>
        /// UserPlayerを取得（PlayerType.Localで検索、簡易版）
        /// 注意: この方法は不正確なため、可能な限り上記のオーバーロードを使用してください
        /// </summary>
        [System.Obsolete("PlayerType.Localによる検索は不正確です。GetUserPlayer(players, networkContext, mapper)を使用してください")]
        public static PlayerInfo GetUserPlayerByType(List<PlayerInfo> players)
        {
            var userPlayer = players.Find(p => p.PlayerType == PlayerType.Local);
            if (userPlayer.Id.Value == 0)  // PlayerInfoはstructなので、デフォルト値チェック
            {
                throw new System.Exception("PlayModeTestHelper: PlayerType.Localのプレイヤーが見つかりません");
            }
            return userPlayer;
        }

        #endregion

        #region ユーティリティ

        /// <summary>
        /// 乱数シードを固定（再現性テスト用）
        /// </summary>
        public static void SetRandomSeed(int seed)
        {
            Random.InitState(seed);
            Debug.Log($"PlayModeTestHelper: 乱数シード固定 (Seed: {seed})");
        }

        /// <summary>
        /// VirtualTransportHubのクリーンアップ
        /// </summary>
        public static void CleanupVirtualTransport()
        {
            VirtualTransportHub.DestroyInstance();
            Debug.Log("PlayModeTestHelper: VirtualTransportHubクリーンアップ完了");
        }

        /// <summary>
        /// GameManagerのクリーンアップとリセット
        /// </summary>
        public static void CleanupGameManager(GameManager gameManager)
        {
            if (gameManager != null)
            {
                gameManager.StopAndReset();
                Debug.Log("PlayModeTestHelper: GameManagerクリーンアップ完了");
            }
        }

        #endregion

        #region オールインワン初期化（簡易テスト用）

        /// <summary>
        /// デフォルト設定で一括初期化（最もシンプルなテスト用）
        /// </summary>
        /// <param name="playerCount">プレイヤー数（デフォルト: 4）</param>
        /// <param name="mode">ネットワークモード（デフォルト: VirtualTransport）</param>
        /// <param name="localPlayerIndex">ローカルプレイヤーのインデックス（デフォルト: 0）</param>
        /// <param name="randomSeed">乱数シード（-1で無効、デフォルト: -1）</param>
        /// <returns>初期化されたGameManager</returns>
        public static GameManager QuickSetup(
            int playerCount = 4,
            NetworkMode mode = NetworkMode.VirtualTransport,
            int localPlayerIndex = 0,
            int randomSeed = -1)
        {
            Debug.Log($"<color=cyan>PlayModeTestHelper: クイックセットアップ開始 (Players: {playerCount}, Mode: {mode})</color>");

            // 乱数シード固定
            if (randomSeed >= 0)
            {
                SetRandomSeed(randomSeed);
            }

            // PlayerInfo生成
            var players = CreateDefaultPlayers(playerCount, localPlayerIndex);

            // NetworkContext生成
            var networkContext = CreateNetworkContext(
                mode,
                localActorNumber: localPlayerIndex + 1,
                playerCount: playerCount,
                isHost: true
            );

            // PlayerIdMapper生成
            var mapper = CreatePlayerIdMapper(players);

            // UserPlayer特定（NetworkContextとMapperから正確に特定）
            var userInfo = GetUserPlayer(players, networkContext, mapper);

            // GameManager初期化
            var gameManager = FindGameManager();
            gameManager.Initialize(players, userInfo, networkContext, mode, mapper);

            Debug.Log("<color=green>PlayModeTestHelper: クイックセットアップ完了</color>");
            return gameManager;
        }

        /// <summary>
        /// クイックセットアップのクリーンアップ
        /// </summary>
        public static void QuickCleanup(GameManager gameManager, NetworkMode mode)
        {
            CleanupGameManager(gameManager);

            if (mode == NetworkMode.VirtualTransport)
            {
                CleanupVirtualTransport();
            }
        }

        #endregion
    }
}

