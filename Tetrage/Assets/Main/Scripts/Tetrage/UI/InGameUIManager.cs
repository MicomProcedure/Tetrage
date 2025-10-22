using UnityEngine;
using Tetrage.Core;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Contracts;
using Tetrage.UI;
using Tetrage.Core.DTO;

namespace Tetrage.Managers
{
	/// <summary>
	/// ゲーム中UIの集約管理。GameplayEventBusを購読して各UIへ反映する。
	/// </summary>
	public class InGameUIManager : MonoBehaviour
	{
		#region Serialized Fields
		[SerializeField] private PlayerUIPanelManager _playerUIPanelManager;
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
			_subscribed = true;
		}

		private void Unsubscribe()
		{
			if (!_subscribed || _events == null) return;
			_events.TurnStartedApplied -= OnTurnStarted;
			_events.GameStartedApplied -= OnGameStarted;
			_subscribed = false;
		}
		#endregion

		#region Event Handlers
		private void OnGameStarted(GameStartedEvent e)
		{
			if (_playerUIPanelManager != null && _gameContext?.CurrentPlayer != null)
			{
				_playerUIPanelManager.SetCurrentPlayer(_gameContext.CurrentPlayer.PlayerId);
			}
		}

		private void OnTurnStarted(TurnStartedEvent e)
		{
			if (_playerUIPanelManager == null) return;
			// e.currentPlayerActorNumber を使ってハイライト
			_playerUIPanelManager.SetCurrentPlayer(e.currentPlayerActorNumber);
		}
		#endregion
	}
}
