using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Contracts;
using Tetrage.UI;
using Tetrage.Presenters;
using System;

namespace Tetrage.Factories
{
    /// <summary>
    /// プレイヤーモデル生成後にViewとPresenterを構築するデコレーターファクトリ
    /// </summary>
    public class PlayerWithViewFactory : IPlayerFactory
    {
        private readonly IPlayerFactory _innerFactory;
        private readonly BasicPlayerView _viewPrefab;
        private readonly Transform _parentTransform;
        private readonly IDictionary<IPlayer, BasicPlayerView> _playerViewsDict;

        // 必ずコンストラクタに引数を持たせないとコンパイルエラーになる
        [Obsolete("Use PlayerWithViewFactory(IPlayerFactory innerFactory, BasicPlayerView viewPrefab, Transform parentTransform, IDictionary<IPlayer,BasicPlayerView> playerViewsDict) instead", true)]
        public PlayerWithViewFactory() { }

        /// <param name="innerFactory">プレイヤーモデル生成を委譲するIPlayerFactory</param>
        /// <param name="viewPrefab">プレイヤー表示用Viewプレハブ</param>
        /// <param name="parentTransform">生成したViewの親Transform</param>
        /// <param name="playerViewsDict">プレイヤーモデルとビューの対応辞書</param>
        public PlayerWithViewFactory(IPlayerFactory innerFactory, BasicPlayerView viewPrefab, Transform parentTransform, IDictionary<IPlayer, BasicPlayerView> playerViewsDict)
        {
            // 必須パラメータのnullチェック
            // プレイヤーモデル生成を委譲するファクトリのnullチェック
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            // プレイヤービュープレハブのnullチェック
            Assert.IsNotNull(viewPrefab, "PlayerViewPrefab が null です");
            // 生成したビューの親となるTransformのnullチェック
            Assert.IsNotNull(parentTransform, "ParentTransform が null です");
            // プレイヤーモデルとビューの対応辞書のnullチェック
            Assert.IsNotNull(playerViewsDict, "playerViewsDict が null です");
            _innerFactory = innerFactory;
            _viewPrefab = viewPrefab;
            _parentTransform = parentTransform;
            _playerViewsDict = playerViewsDict;
        }

        /// <param name="innerFactory">プレイヤーモデル生成を委譲するIPlayerFactory</param>
        /// <param name="viewPrefab">プレイヤー表示用Viewプレハブ</param>
        /// <param name="playerViewsDict">プレイヤーモデルとビューの対応辞書</param>
        public PlayerWithViewFactory(IPlayerFactory innerFactory, BasicPlayerView viewPrefab, IDictionary<IPlayer, BasicPlayerView> playerViewsDict)
        {
            // 必須パラメータのnullチェック
            // プレイヤーモデル生成を委譲するファクトリのnullチェック
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            // プレイヤービュープレハブのnullチェック
            Assert.IsNotNull(viewPrefab, "PlayerViewPrefab が null です");
            // プレイヤーモデルとビューの対応辞書のnullチェック
            Assert.IsNotNull(playerViewsDict, "playerViewsDict が null です");
            _innerFactory = innerFactory;
            _viewPrefab = viewPrefab;
            _parentTransform = null; // 親を指定しない
            _playerViewsDict = playerViewsDict;
        }

        /// <inheritdoc/>
        public IPlayer CreatePlayer(string userId, Card target)
        {
            // プレイヤーモデル生成
            var playerModel = _innerFactory.CreatePlayer(userId, target);

            // プレイヤーモデルに対応するViewとPresenterを生成
            CreateViewAndPresenter(playerModel);

            return playerModel;
        }

        /// <summary>
        /// プレイヤーモデルに対応するViewとPresenterを生成します
        /// </summary>
        /// <param name="player">対象のプレイヤーモデル</param>
        private void CreateViewAndPresenter(IPlayer player)
        {
            // View生成（parentTransformがnullの場合はシーンルートに配置）
            var view = _parentTransform != null
                ? UnityEngine.Object.Instantiate(_viewPrefab, _parentTransform)
                : UnityEngine.Object.Instantiate(_viewPrefab);

            // プレイヤーモデルとビューの対応を辞書に登録
            _playerViewsDict.Add(player, view);

            // Presenter生成
            var presenter = new PlayerPresenter(view, player);
        }
    }
}