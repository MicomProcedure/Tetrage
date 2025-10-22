using Tetrage.Factories;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Models;
using System;
using UnityEngine;
using Tetrage.Core.DTO;

namespace Tetrage.Managers
{
    /// <summary>
    /// フィールドセットアップ管理クラス
    /// 静的設定と動的参加者情報を組み合わせてフィールドを構築
    /// </summary>
    public class FieldSetupManager
    {
        private readonly FieldSetupSettings _settings;
        private readonly FieldSetupDependencies _dependencies;
        private List<IPlayer> _players;
        private Stage _stage;

        // プレイヤーのリストを取得可能なプロパティ
        public List<IPlayer> Players
        {
            get
            {
                if (!_isSetup)
                {
                    throw new InvalidOperationException("FieldSetupManager がまだセットアップされていません。");
                }
                return _players;
            }
        }

        // ステージのインスタンスを取得可能なプロパティ
        public Stage Stage
        {
            get
            {
                if (!_isSetup)
                {
                    throw new InvalidOperationException("FieldSetupManager がまだセットアップされていません。");
                }
                return _stage;
            }
        }



        // フィールドのセットアップが完了したかどうかを示すフラグ
        private bool _isSetup = false;

        /* --- コンストラクタ --- */

        /// <summary>
        /// FieldSetupManagerのコンストラクタ
        /// </summary>
        /// <param name="settings">フィールドセットアップ設定（静的設定のみ）</param>
        /// <param name="dependencies">フィールドセットアップ依存性</param>
        public FieldSetupManager(FieldSetupSettings settings, FieldSetupDependencies dependencies)
        {
            _settings = settings;
            _dependencies = dependencies;
        }

        /// <summary>
        /// フィールドのセットアップを行う。動作後、StageとPlayersのプロパティが有効になります。
        /// </summary>
        /// <param name="participantInfoList">参加者情報リスト</param>
        public void SetupField(List<PlayerInfo> participantInfoList)
        {
            if (participantInfoList == null || participantInfoList.Count == 0)
            {
                throw new ArgumentException("参加者情報リストが空です", nameof(participantInfoList));
            }

            try
            {
                // フィールド要素の構築
                _stage = SetupStage();
                _players = SetupPlayers(participantInfoList);

                _isSetup = true;
                Debug.Log("FieldSetupManager: フィールドのセットアップが完了しました");
            }
            catch (Exception e)
            {
                Debug.LogError($"FieldSetupManager のセットアップに失敗しました: {e.Message}");
                throw;
            }
        }

        public Stage GetStageInstance()
        {
            return Stage;
        }

        public List<IPlayer> GetPlayers()
        {
            return Players;
        }

        private Stage SetupStage()
        {
            // 依存性注入されたFactoryを使用してStageBuilderを作成
            var stageBuilder = new StageBuilder(
                (StageModelFactory)_dependencies.StageModelFactory,
                _dependencies.CardPileFactory,
                _dependencies.CardFactory);

            var stage = stageBuilder
                .UseView(
                    _settings.StageViewPrefab,
                    _settings.StageSpawnPosition,
                    _settings.StageRoot,
                    _settings.CardViewPrefab,
                    _settings.PileViewPrefabDict
                )
                .WithCardPileLayoutSettings(CardPileType.Trash, _settings.TrashPileLayoutSettings)
                .WithCardPileLayoutSettings(CardPileType.Stack, _settings.StackPileLayoutSettings)
                .Build();

            return stage;
        }

        /// <summary>
        /// プレイヤーを動的参加者情報に基づいてセットアップ
        /// </summary>
        /// <param name="participantInfoList">参加者情報</param>
        private List<IPlayer> SetupPlayers(List<PlayerInfo> participantInfoList)
        {
            // return予定のプレイヤーリストを作成
            var players = new List<IPlayer>();

            // 依存性注入されたIPlayerFactoryを使用（RegisteringPlayerFactory対応）
            var playerBuilder = new PlayerBuilder(_dependencies.PlayerModelFactory);

            // 位置数の検証
            if (_settings.PlayerLocations.Count < participantInfoList.Count)
            {
                throw new InvalidOperationException($"プレイヤー位置数({_settings.PlayerLocations.Count})が参加者数({participantInfoList.Count})より少ないです");
            }

            // 要請に応じてプレイヤーを作成する
            int playerIndex = 0;
            foreach (var participantInfo in participantInfoList)
            {
                var player = playerBuilder
                    .WithUserId(participantInfo.UserId)
                    .WithIconIndex(participantInfo.PlayerIconIndex)
                    .WithPlayerId(participantInfo.Id)
                    .WithPlayerType(participantInfo.PlayerType)
                    .UseView(
                        _settings.PlayerViewPrefabDict[participantInfo.PlayerType],
                        _settings.PlayerLocations[playerIndex],
                        _settings.PlayerRoot,
                        _settings.PileViewPrefabDict
                    )
                    .WithCardPileLayoutSettings(CardPileType.Hands, _settings.PlayerPilesLayoutSettings[CardPileType.Hands])
                    .WithCardPileLayoutSettings(CardPileType.Tmp, _settings.PlayerPilesLayoutSettings[CardPileType.Tmp])
                    .WithCardPileLayoutSettings(CardPileType.Target, _settings.PlayerPilesLayoutSettings[CardPileType.Target])
                    .Build();
                players.Add(player);
                playerIndex++;
            }
            Debug.Log("FieldSetupManager: プレイヤーのセットアップが完了しました");
            foreach (var player in players)
            {
                Debug.Log($"FieldSetupManager: プレイヤーID: {player.Id}, ユーザーID: {player.UserId}");
            }
            return players;
        }
    }
}