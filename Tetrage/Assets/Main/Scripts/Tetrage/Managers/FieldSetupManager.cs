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
                .WithCardPileLayoutSettings(CardPileType.Trash, _settings.CardPileLayoutSettingsDict[CardPileType.Trash])
                .WithCardPileLayoutSettings(CardPileType.Stack, _settings.CardPileLayoutSettingsDict[CardPileType.Stack])
                .Build();

            return stage;
        }
        private List<IPlayer> SetupPlayers()
        {
            Debug.Log("プレイヤーセットアップを開始します");

            // return予定のプレイヤーリストを作成
            var players = new List<IPlayer>();

            // 設定の妥当性チェック
            if (_settings.ParticipantInfoList == null)
            {
                Debug.LogError("ParticipantInfoListがnullです");
                throw new System.NullReferenceException("ParticipantInfoList is null");
            }

            if (_settings.PlayerViewPrefabDict == null)
            {
                Debug.LogError("PlayerViewPrefabDictがnullです");
                throw new System.NullReferenceException("PlayerViewPrefabDict is null");
            }

            if (_settings.PlayerLocations == null)
            {
                Debug.LogError("PlayerLocationsがnullです");
                throw new System.NullReferenceException("PlayerLocations is null");
            }

            if (_settings.PlayerRoot == null)
            {
                Debug.LogError("PlayerRootがnullです");
                throw new System.NullReferenceException("PlayerRoot is null");
            }

            if (_settings.PileViewPrefabDict == null)
            {
                Debug.LogError("PileViewPrefabDictがnullです");
                throw new System.NullReferenceException("PileViewPrefabDict is null");
            }

            Debug.Log($"参加者数: {_settings.ParticipantInfoList.Count}, プレイヤー位置数: {_settings.PlayerLocations.Count}");

            // 依存性注入されたPlayerModelFactoryを使用
            var playerBuilder = new PlayerBuilder((PlayerModelFactory)_dependencies.PlayerModelFactory);

            // 要請に応じてプレイヤーを作成する
            int playerIndex = 0;
            foreach (var participantInfo in _settings.ParticipantInfoList)
            {
                Debug.Log($"プレイヤー {playerIndex} を作成中 - PlayerType: {participantInfo.PlayerType}, UserId: {participantInfo.UserId}");

                // プレイヤータイプに対応するPrefabが存在するかチェック
                if (!_settings.PlayerViewPrefabDict.ContainsKey(participantInfo.PlayerType))
                {
                    Debug.LogError($"PlayerType {participantInfo.PlayerType} に対応するPlayerViewPrefabが見つかりません");
                    throw new System.ArgumentException($"PlayerViewPrefab for {participantInfo.PlayerType} not found");
                }

                // プレイヤー位置が範囲内かチェック
                if (playerIndex >= _settings.PlayerLocations.Count)
                {
                    Debug.LogError($"プレイヤーインデックス {playerIndex} がPlayerLocations配列の範囲外です（配列サイズ: {_settings.PlayerLocations.Count}）");
                    throw new System.IndexOutOfRangeException($"PlayerIndex {playerIndex} is out of range for PlayerLocations");
                }

                var playerViewPrefab = _settings.PlayerViewPrefabDict[participantInfo.PlayerType];
                if (playerViewPrefab == null)
                {
                    Debug.LogError($"PlayerType {participantInfo.PlayerType} のPlayerViewPrefabがnullです");
                    throw new System.NullReferenceException($"PlayerViewPrefab for {participantInfo.PlayerType} is null");
                }

                Debug.Log($"プレイヤー {playerIndex} のビューを作成中...");

                var player = playerBuilder
                    .WithUserId(participantInfo.UserId)
                    .WithPlayerType(participantInfo.PlayerType)
                    .UseView(
                        _settings.PlayerViewPrefabDict[participantInfo.PlayerType],
                        _settings.PlayerLocations[playerIndex],
                        _settings.PlayerRoot,
                        _settings.PileViewPrefabDict
                    )
                    .WithCardPileLayoutSettings(CardPileType.Hands, _settings.CardPileLayoutSettingsDict[CardPileType.Hands])
                    .WithCardPileLayoutSettings(CardPileType.Tmp, _settings.CardPileLayoutSettingsDict[CardPileType.Tmp])
                    .WithCardPileLayoutSettings(CardPileType.Target, _settings.CardPileLayoutSettingsDict[CardPileType.Target])
                    .Build();
                players.Add(player);
                playerIndex++;

                Debug.Log($"プレイヤー {playerIndex - 1} の作成が完了しました");
            }

            Debug.Log($"全プレイヤーの作成が完了しました。プレイヤー数: {players.Count}");
            return players;
        }
    }
}