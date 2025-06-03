using Tetrage.Factories;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Models;
using System;
using UnityEngine;

namespace Tetrage.Managers
{
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
        /// <param name="settings">フィールドセットアップ設定</param>
        /// <param name="dependencies">フィールドセットアップ依存性</param>
        public FieldSetupManager(FieldSetupSettings settings, FieldSetupDependencies dependencies)
        {
            _settings = settings;
            _dependencies = dependencies;
        }

        // フィールドのセットアップを行う。動作後、StageとPlayersのプロパティが有効になります。
        public void SetupField()
        {
            try
            {
                _stage = SetupStage();
                _players = SetupPlayers();
                _isSetup = true;
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
        private List<IPlayer> SetupPlayers()
        {
            // return予定のプレイヤーリストを作成
            var players = new List<IPlayer>();

            // 依存性注入されたPlayerModelFactoryを使用
            var playerBuilder = new PlayerBuilder((PlayerModelFactory)_dependencies.PlayerModelFactory);

            // 要請に応じてプレイヤーを作成する
            int playerIndex = 0;
            foreach (var participantInfo in _settings.ParticipantInfoList)
            {
                var player = playerBuilder
                    .WithUserId(participantInfo.UserId)
                    .WithPlayerType(participantInfo.PlayerType)
                    .UseView(
                        _settings.PlayerViewPrefabDict[participantInfo.PlayerType],
                        _settings.PlayerLocations[playerIndex],
                        _settings.PlayerRoot,
                        _settings.PileViewPrefabDict
                    )
                    .WithCardPileLayoutSettings(CardPileType.Hands, _settings.PlayerPilesLayoutSettings[participantInfo.PlayerType])
                    .WithCardPileLayoutSettings(CardPileType.Tmp, _settings.PlayerPilesLayoutSettings[participantInfo.PlayerType])
                    .WithCardPileLayoutSettings(CardPileType.Target, _settings.PlayerPilesLayoutSettings[participantInfo.PlayerType])
                    .Build();
                players.Add(player);
                playerIndex++;
            }
            return players;
        }
    }
}