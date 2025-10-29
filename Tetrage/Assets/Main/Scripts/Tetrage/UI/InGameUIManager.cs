using UnityEngine;
using Tetrage.Core;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.UI;
using Tetrage.Core.DTO;
using System.Collections.Generic;
using Tetrage.Core.Actions;
using Tetrage.Animations;

namespace Tetrage.Managers
{
	/// <summary>
	/// ゲーム中UIの集約管理。GameplayEventBusを購読して各UIへ反映する。
	/// </summary>
	public class InGameUIManager : MonoBehaviour
	{
		#region Serialized Fields
		[Header("UI Controllers")]
		[SerializeField] private PlayerUIPanelManager _playerUIPanelManager;
		[SerializeField] private ActionPanelController _actionPanelController;
		[SerializeField] private GameStartAnimation _gameStartAnimation;
		[SerializeField] private ScanUIController _ScanUIController;
		[SerializeField] private ResultUI _resultUI;

		[Header("Animations")]
		[SerializeField] private CutInAnimationController _TetrageSoloCutInAnimCtl;

		#endregion
		#region Private Fields
		private IGameContextProvider _gameContext;
		private IGameplayEventBus _events;
		private bool _subscribed;
		#endregion

		#region Public API
		/// <summary>
		/// GameContextを受け取り、イベント購読を開始する。
		/// </summary>
		public void Initialize(IGameContextProvider context)
		{
			_gameContext = context;
			_events = context?.Events;

			if (_events == null)
			{
				Debug.LogWarning("InGameUIManager: Eventsが見つかりません");
				return;
			}

			if (_ScanUIController != null)
			{
				_ScanUIController.Initialize(_gameContext);
			}

			if (_actionPanelController != null)
			{
				_actionPanelController.Initialize(_gameContext);
			}

			if (_playerUIPanelManager != null && _gameContext?.Players != null)
			{
				Debug.Log($"InGameUIManager: Initialize時にPlayerUIパネルをセットアップします（プレイヤー数: {_gameContext.Players.Count}）");
				_playerUIPanelManager.SetupPanels(CreatePlayerInfoList(_gameContext.Players));
			}

			Subscribe();

			// 初期パネル構築はGameManager等から明示的に渡す想定
		}

		/// <summary>
		/// 終了時に購読解除する。
		/// </summary>
		public void Teardown()
		{
			Unsubscribe();
		}
		#endregion

		#region Subscription
		private void Subscribe()
		{
			if (_subscribed || _events == null) return;
			_events.TurnStartedApplied += OnTurnStarted;
			_events.GameStartedApplied += OnGameStarted;
			_events.FinishingGameApplied += OnFinishingGame;
			_subscribed = true;
		}

		private void Unsubscribe()
		{
			if (!_subscribed || _events == null) return;
			_events.TurnStartedApplied -= OnTurnStarted;
			_events.GameStartedApplied -= OnGameStarted;
			_events.FinishingGameApplied -= OnFinishingGame;
			_subscribed = false;
		}
		#endregion

		#region Event Handlers
		private void OnGameStarted(GameStartedEvent e)
		{
			Debug.Log($"InGameUIManager: OnGameStarted");
			if (_playerUIPanelManager != null && _gameContext?.CurrentPlayer != null)
			{
				_playerUIPanelManager.SetupPanels(CreatePlayerInfoList(_gameContext.Players));
				_playerUIPanelManager.SetCurrentPlayer(_gameContext.CurrentPlayer.PlayerId);
			}
			if (_gameStartAnimation != null)
			{
				_gameStartAnimation.PlayGameStartAnimation();
			}
		}

		private void OnTurnStarted(TurnStartedEvent e)
		{
			Debug.Log($"InGameUIManager: OnTurnStarted, currentPlayerActorNumber: {e.currentPlayerActorNumber}");
			if (_playerUIPanelManager == null) return;
			// e.currentPlayerActorNumber を使ってハイライト
			_playerUIPanelManager.SetCurrentPlayer(e.currentPlayerActorNumber);
			// _actionPanelController.OnTurnStartedEvent(e);
		}

		private void OnActionResult(ActionResultEvent e)
		{
			Debug.Log($"InGameUIManager: OnActionResult");
			if (_TetrageSoloCutInAnimCtl != null && e.actionType == ActionType.TetrageSolo)
			{
				_TetrageSoloCutInAnimCtl.PlayCutIn();
			}
		}

		private void OnFinishingGame(FinishingGameEvent e)
		{
			Debug.Log($"InGameUIManager: OnFinishingGame");
			_resultUI.DisplayResult(e.winnerActorNumbers, _gameContext.Players);
		}
		#endregion

		#region ヘルパー
		private List<PlayerInfo> CreatePlayerInfoList(IReadOnlyList<IPlayer> players)
		{
			var playerInfoList = new List<PlayerInfo>();
			foreach (var player in players)
			{
				playerInfoList.Add(new PlayerInfo { Id = player.Id, UserId = player.UserId, PlayerType = PlayerType.Local, PlayerIconIndex = player.IconIndex });
			}
			return playerInfoList;
		}

		#endregion
	}

}