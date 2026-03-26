using UnityEngine;
using Tetrage.Core;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.UI;
using Tetrage.Core.DTO;
using System.Collections.Generic;
using Tetrage.Core.Actions;
using R3;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Managers
{
	/// <summary>
	/// ゲーム中UIの集約管理。GameplayEventBusを購読して各UIへ反映する。
	/// 試合結果は専用シーンではなく <see cref="Tetrage.UI.ResultUI"/> オーバーレイ（FinishingGame 時に表示）。
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
		private IGameContext _gameContext;
		private IGameplayEventBus _events;
		private CompositeDisposable _disposables = new();
		#endregion

		#region Public API
		/// <summary>
		/// GameContextを受け取り、イベント購読を開始する。
		/// </summary>
		/// <param name="gameplayNetwork">ScanPhase で ScanTargetSelected を送るために ScanUI に渡す（未設定なら送信不可）</param>
		public void Initialize(IGameContext context, IGameplayNetworkController gameplayNetwork = null)
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
				_ScanUIController.Initialize(_gameContext, gameplayNetwork);
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
			ApplyInitialHudVisibility();

			// 初期パネル構築はGameManager等から明示的に渡す想定
		}

		/// <summary>
		/// 終了時に購読解除する。
		/// </summary>
		public void Teardown()
		{
			if (_ScanUIController != null)
			{
				_ScanUIController.TeardownScan();
			}
			Unsubscribe();
		}

		/// <summary>
		/// ゲーム中HUD（プレイヤーパネル・アクションパネル・スキャンUIなど）の表示切替。
		/// Result表示時は false にして結果オーバーレイだけ見えるようにする。
		/// </summary>
		public void SetGameplayHudVisible(bool visible)
		{
			if (_playerUIPanelManager != null)
			{
				_playerUIPanelManager.SetPlayerHudRootVisible(visible);
			}

			if (_actionPanelController != null)
			{
				_actionPanelController.gameObject.SetActive(visible);
			}

			if (_ScanUIController != null)
			{
				_ScanUIController.gameObject.SetActive(visible);
			}

			if (_TetrageSoloCutInAnimCtl != null)
			{
				_TetrageSoloCutInAnimCtl.gameObject.SetActive(visible);
			}
		}

		private void OnDestroy()
		{
			Unsubscribe();
		}
		#endregion

		#region Subscription
		private void Subscribe()
		{
			if (_events == null) return;

			// R3のObservableで購読
			_events.GameStarted
				.Subscribe(OnGameStarted)
				.AddTo(_disposables);

			_events.TurnStarted
				.Subscribe(OnTurnStarted)
				.AddTo(_disposables);

			_events.FinishingGame
				.Subscribe(OnFinishingGame)
				.AddTo(_disposables);

			_events.ActionResult
				.Subscribe(OnActionResult)
				.AddTo(_disposables);

			_events.ScanPhaseStarted
				.Subscribe(OnScanPhaseStarted)
				.AddTo(_disposables);

			_events.ScanPhaseEnded
				.Subscribe(OnScanPhaseEnded)
				.AddTo(_disposables);
		}

		private void Unsubscribe()
		{
			_disposables.Dispose();
			_disposables = new();
		}
		#endregion

		#region Event Handlers
		private void OnGameStarted(DomainEvents.GameStartedEvent e)
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

		private void OnTurnStarted(DomainEvents.TurnStartedEvent e)
		{
			Debug.Log($"InGameUIManager: OnTurnStarted, currentPlayerId: {e.CurrentPlayerId}");
			if (_playerUIPanelManager == null) return;
			// DomainEventのCurrentPlayerIdを使ってハイライト（intに変換）
			_playerUIPanelManager.SetCurrentPlayer(e.CurrentPlayerId.Value);

			// ScanPhase あり: OnScanPhaseEnded で既に有効化済み。スキップ時は ScanPhaseEnded が来ないためここで有効化する
			SetActionPanelActive(true);
		}

		private void OnActionResult(DomainEvents.ActionResultEvent e)
		{
			Debug.Log($"InGameUIManager: OnActionResult");
			if (_TetrageSoloCutInAnimCtl != null && e.ActionType == ActionType.TetrageSolo)
			{
				_TetrageSoloCutInAnimCtl.gameObject.SetActive(true);
				_TetrageSoloCutInAnimCtl.PlayCutIn();
			}
		}

		private void OnFinishingGame(DomainEvents.FinishingGameEvent e)
		{
			Debug.Log($"InGameUIManager: OnFinishingGame");
			if (_resultUI == null)
			{
				Debug.LogWarning("InGameUIManager: ResultUI が未設定のため結果を表示しません");
				return;
			}

			if (_gameContext?.Players == null)
			{
				Debug.LogWarning("InGameUIManager: Players が無いため Result を表示しません");
				return;
			}

			_resultUI.gameObject.SetActive(true);

			SetGameplayHudVisible(false);

			// PlayerId[]をint[]に変換
			var winnerIds = new int[e.WinnerPlayerIds.Count];
			for (int i = 0; i < e.WinnerPlayerIds.Count; i++)
			{
				winnerIds[i] = e.WinnerPlayerIds[i].Value;
			}

			_resultUI.DisplayResult(winnerIds, _gameContext.Players);
		}

		private void OnScanPhaseStarted(DomainEvents.ScanPhaseStartedEvent e)
		{
			Debug.Log("InGameUIManager: OnScanPhaseStarted");
			SetScanUiActive(true);
		}

		private void OnScanPhaseEnded(DomainEvents.ScanPhaseEndedEvent e)
		{
			Debug.Log("InGameUIManager: OnScanPhaseEnded");
			SetScanUiActive(false);
			SetActionPanelActive(true);
		}

		#endregion

		#region 初期表示・UIの有効化

		/// <summary>
		/// 初期化直後のHUD状態。ゲーム開始演出・プレイヤーパネルは表示し、
		/// Scan・アクション・カットイン・結果はイベントまで非表示にする。
		/// </summary>
		private void ApplyInitialHudVisibility()
		{
			SetScanUiActive(false);
			SetActionPanelActive(false);
			SetCutInActive(false);
			if (_resultUI != null)
			{
				_resultUI.gameObject.SetActive(false);
			}
		}

		private static void SetUiRootActive(MonoBehaviour component, bool active)
		{
			if (component != null)
			{
				component.gameObject.SetActive(active);
			}
		}

		private void SetScanUiActive(bool active) => SetUiRootActive(_ScanUIController, active);

		private void SetActionPanelActive(bool active) => SetUiRootActive(_actionPanelController, active);

		private void SetCutInActive(bool active) => SetUiRootActive(_TetrageSoloCutInAnimCtl, active);

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