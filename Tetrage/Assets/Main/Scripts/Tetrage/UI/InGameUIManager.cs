using UnityEngine;
using Tetrage.Core;
using Tetrage.Core.Constants;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.UI;
using Tetrage.Core.DTO;
using System.Collections.Generic;
using Tetrage.Core.Actions;
using R3;
using Tetrage.Services;
using Tetrage.Core.Ids;
using Cysharp.Threading.Tasks;
using DomainEvents = Tetrage.Core.Events;
using NetworkDto = Tetrage.Network.Gameplay;

namespace Tetrage.Managers
{
	/// <summary>
	/// ゲーム中UIの集約管理。GameplayEventBus を購読し、Scan 系は <see cref="ScanPhaseUI"/> へ委譲する。
	/// 試合結果は専用シーンではなく <see cref="Tetrage.UI.ResultUI"/> オーバーレイ（FinishingGame 時に表示）。
	/// </summary>
	public class InGameUIManager : MonoBehaviour
	{
		#region Serialized Fields
		[Header("UI Controllers")]
		[SerializeField] private PlayerUIPanelManager _playerUIPanelManager;
		[SerializeField] private ActionPanelController _actionPanelController;
		[SerializeField] private ScanPhaseUI _ScanUIController;
		[SerializeField] private ResultUI _resultUI;
		[SerializeField] private InGameNavigation _inGameNavigation;
		[SerializeField] private InGameLoadingUI _loadingUI;
		[Header("Animations")]
		[SerializeField] private CutInAnimationController _TetrageSoloCutInAnimCtl;
		[SerializeField] private GameStartAnimation _gameStartAnimation;

		#endregion
		#region Private Fields
		private IGameContext _gameContext;
		private IGameplayEventBus _events;
		private CompositeDisposable _disposables = new();
		private CompositeDisposable _scanTargetCardClickDisposables = new();
		private IGameplayNetworkController _gameplayNetwork;
		private bool _scanOwnTargetConfirmed;
		private bool _scanOpponentSelected;
		private bool _scanOpponentSuitRevealed;
		private bool _scanSelectionSent;
		private PlayerId _scanSelectedTargetPlayerId;
		private Suit _scanSelectedTargetSuit;
		#endregion

		#region Public API
		/// <summary>
		/// GameContextを受け取り、イベント購読を開始する。
		/// </summary>
		/// <param name="gameplayNetwork">ScanPhase で ScanTargetSelected を送るために ScanUI に渡す（未設定なら送信不可）</param>
		public void Initialize(IGameContext context, IGameplayNetworkController gameplayNetwork = null)
		{
			if (!ValidateInitialize(context))	// 初期化に必要なContextとInspector参照が揃っているか検証する。
			{
				return;
			}

			_gameContext = context;
			_events = context.Events;
			_gameplayNetwork = gameplayNetwork;

			InitializeUIElements(gameplayNetwork);	// UI要素の初期化
			Subscribe();	// R3のObservableで購読
			ApplyInitialHudVisibility();	// 初期表示・UIの有効化
		}

		/// <summary>
		/// 終了時に購読解除する。
		/// </summary>
		public void Teardown()
		{
			if (_ScanUIController != null)
			{
				_ScanUIController.ResetScanPhaseUIState();
			}
			ResetScanSelectionState();
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

			if (_inGameNavigation != null)
			{
				_inGameNavigation.gameObject.SetActive(visible);
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

            _events.ListOrderDeclared
                .Select(e => e as DomainEvents.ListOrderDeclaredEvent<PlayerId>)
                .Subscribe(OnListOrderDeclared)
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

			_events.ScanResultReceived
				.Subscribe(OnScanResultReceived)
				.AddTo(_disposables);
		}

		private void Unsubscribe()
		{
			_disposables.Dispose();
			_disposables = new();
			_scanTargetCardClickDisposables.Dispose();
			_scanTargetCardClickDisposables = new();
		}
		#endregion

		#region Event Handlers
		private void OnGameStarted(DomainEvents.GameStartedEvent e)
		{
			Debug.Log($"InGameUIManager: OnGameStarted");
			if (_playerUIPanelManager == null)
			{
				Debug.LogError("InGameUIManager: PlayerUIPanelManager が未設定のため GameStarted 時の PlayerUI 更新をスキップします。");
				return;
			}

			if (_gameContext?.Players == null)
			{
				Debug.LogError("InGameUIManager: Players が未設定のため GameStarted 時の PlayerUI 更新をスキップします。");
				return;
			}


			if (_gameContext?.CurrentPlayer != null)
			{
				_playerUIPanelManager.SetCurrentPlayer(_gameContext.CurrentPlayer.PlayerId); // 現在のプレイヤーをハイライトする。
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
			SetScanUIActive(true);	// スキャンUIを表示
			SetLoadingUIActive(false);	// ローディングUIを非表示
			SetInGameNavigationActive(false);
			ResetScanSelectionState();
			_playerUIPanelManager.SetupPanels();	// PlayerUIパネルを初期化する。
			// Instantiate 直後・Photon の FixedUpdate 内では Canvas / PlayerUI.Start より先にここへ来るため、レイアウト確定後に同期する。
			_ScanUIController?.ApplyScanPhaseStarted(e);
		}

		private async void OnListOrderDeclared(DomainEvents.ListOrderDeclaredEvent e){
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			await UniTask.Delay(100);
			RefreshTargetPileViewsAfterUILayoutAsync().Forget();	// ターン順序決定時にTargetカード山ビューの表示位置を再同期する。
		}

		private void OnScanPhaseEnded(DomainEvents.ScanPhaseEndedEvent e)
		{
			Debug.Log("InGameUIManager: OnScanPhaseEnded");
			ResetScanSelectionState();
			_ScanUIController?.ApplyScanPhaseEnded();
			SetScanUIActive(false);
			SetInGameNavigationActive(false);
			SetActionPanelActive(true);

			_gameStartAnimation?.PlayGameStartAnimation();	// ゲーム開始演出を再生
		}

		private void OnScanResultReceived(DomainEvents.ScanResultReceivedEvent e)
		{
			_ScanUIController?.ApplyScanResultReceived(e);
		}

		#endregion

		#region 初期表示・UIの有効化

		/// <summary>
		/// 初期化直後のHUD状態。ゲーム開始演出・プレイヤーパネルは表示し、
		/// Scan・アクション・カットイン・結果はイベントまで非表示にする。
		/// </summary>
		private void ApplyInitialHudVisibility()
		{
			SetScanUIActive(false);
			SetActionPanelActive(false);
			SetCutInActive(false);
			SetResultUIActive(false);
			SetInGameNavigationActive(false);

			SetLoadingUIActive(true);	// ローディングUIを表示

			Debug.Log("InGameUIManager: ApplyInitialHudVisibility");
		}

		private static void SetUIRootActive(MonoBehaviour component, bool active)
		{
			if (component != null)
			{
				component.gameObject.SetActive(active);
			}
		}

		private void SetScanUIActive(bool active) => SetUIRootActive(_ScanUIController, active);

		private void SetActionPanelActive(bool active) => SetUIRootActive(_actionPanelController, active);

		private void SetCutInActive(bool active) => SetUIRootActive(_TetrageSoloCutInAnimCtl, active);

		private void SetInGameNavigationActive(bool active) => SetUIRootActive(_inGameNavigation, active);
		private void SetResultUIActive(bool active) => SetUIRootActive(_resultUI, active);
		private void SetLoadingUIActive(bool active) => SetUIRootActive(_loadingUI, active);

		/// <summary>
		/// PlayerUI側の配置確定後にTargetカード山ビューの表示位置を再同期する。
		/// </summary>
		/// <summary>
		/// RectTransform の最終座標が確定した後に Target 山とマーカーを同期する。
		/// </summary>
		private async UniTaskVoid RefreshTargetPileViewsAfterUILayoutAsync()
		{
			Canvas.ForceUpdateCanvases();
			// SetupPanels 直後は同一フレーム内で PlayerUI.Start やレイアウトより先に実行されることがあるため、少なくとも 1 フレーム待つ。
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: this.GetCancellationTokenOnDestroy());
			Canvas.ForceUpdateCanvases();
			RefreshTargetPileViews();
		}

		private void RefreshTargetPileViews()
		{
			if (_playerUIPanelManager == null)
			{
				Debug.LogWarning("InGameUIManager: _playerUIPanelManager is null");
				return;
			}

			var targetPileViews = FindObjectsByType<TargetSyncUIPileView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < targetPileViews.Length; i++)
			{
				// TargetSyncUIPileViewに保持したPlayerIdを使って、Manager側へ登録する。
				if (!targetPileViews[i].TryGetOwnerPlayerId(out PlayerId? playerId))
				{
					Debug.LogWarning("InGameUIManager: targetPileViews[i].TryGetOwnerPlayerId failed");
					continue;
				}

				if (playerId == null)
				{
					Debug.LogWarning("InGameUIManager: playerId is null");
					continue;
				}

				_playerUIPanelManager.RegisterTargetPileView((PlayerId)playerId, targetPileViews[i]);	// ここではPlayerIdはnullではないことが保証されているため、キャストは安全。
			}

			_playerUIPanelManager.RefreshAllTargetPileViewPositions();
		
		}

		#endregion

		#region ヘルパー


		#endregion

		#region Validation

		/// <summary>
		/// 初期化に必要なContextとInspector参照が揃っているか検証する。
		/// </summary>
		private bool ValidateInitialize(IGameContext context)
		{
			bool ok = true;

			if (context == null)
			{
				Debug.LogError("InGameUIManager.Initialize: context が null です。");
				ok = false;
			}
			else if (context.Events == null)
			{
				Debug.LogError("InGameUIManager.Initialize: context.Events が null です。");
				ok = false;
			}

			if (!ValidateUIReferences())
			{
				ok = false;
			}

			return ok;
		}

		/// <summary>
		/// Inspectorで定義するUI要素の参照漏れを初期化時に検出する。
		/// </summary>
		private bool ValidateUIReferences()
		{
			bool ok = true;

			if (_playerUIPanelManager == null)
			{
				Debug.LogError("InGameUIManager.Initialize: _playerUIPanelManager が未設定です。Inspector で割り当ててください。");
				ok = false;
			}

			if (_actionPanelController == null)
			{
				Debug.LogError("InGameUIManager.Initialize: _actionPanelController が未設定です。Inspector で割り当ててください。");
				ok = false;
			}

			if (_gameStartAnimation == null)
			{
				Debug.LogError("InGameUIManager.Initialize: _gameStartAnimation が未設定です。Inspector で割り当ててください。");
				ok = false;
			}

			if (_ScanUIController == null)
			{
				Debug.LogError("InGameUIManager.Initialize: _ScanUIController が未設定です。Inspector で割り当ててください。");
				ok = false;
			}

			if (_resultUI == null)
			{
				Debug.LogError("InGameUIManager.Initialize: _resultUI が未設定です。Inspector で割り当ててください。");
				ok = false;
			}

			if (_inGameNavigation == null)
			{
				Debug.LogError("InGameUIManager.Initialize: _inGameNavigation が未設定です。Inspector で割り当ててください。");
				ok = false;
			}

			if (_loadingUI == null)
			{
				Debug.LogWarning("InGameUIManager.Initialize: _loadingUI が未設定です。LoadingUI の初期表示制御をスキップします。");
			}

			if (_TetrageSoloCutInAnimCtl == null)
			{
				Debug.LogWarning("InGameUIManager.Initialize: _TetrageSoloCutInAnimCtl が未設定です。カットイン演出の制御をスキップします。");
			}

			return ok;
		}

		#endregion

		#region UI Initialization

		/// <summary>
		/// InGameで利用するUI要素の初期化をまとめて行う。
		/// </summary>
		private void InitializeUIElements(IGameplayNetworkController gameplayNetwork)
		{
			_ScanUIController.Initialize(_gameContext);
			_ScanUIController.NextClicked
				.Subscribe(_ => OnScanPhaseNextClicked())
				.AddTo(_disposables);
			_actionPanelController.Initialize(_gameContext);
			InitializePlayerUIPanels();
			_playerUIPanelManager.Initialize(_gameContext);
		}

				/// <summary>
		/// 現在のGameContextからプレイヤーHUDを構築する。
		/// </summary>
		private void InitializePlayerUIPanels()
		{
			if (_gameContext?.Players == null)
			{
				Debug.LogWarning("InGameUIManager.Initialize: Players が未設定のため PlayerUI パネル初期化をスキップします。");
				return;
			}

			// PlayerUI は GameStarted / ScanPhaseStarted で SetupPanels される。この時点ではパネルが無く Refresh は無意味なため行わない。
		}
		#endregion

		#region ScanPhase

		private void OnScanPhaseNextClicked()
		{
			if (!_scanOwnTargetConfirmed)
			{
				_scanOwnTargetConfirmed = true;
				EnterOpponentScanStep();
				return;
			}

			if (_scanSelectionSent)
			{
				return;
			}

			if (!_scanOpponentSelected)
			{
				return;
			}

			if (!_scanOpponentSuitRevealed)
			{
				RevealSelectedOpponentTargetSuit();
				return;
			}

			if (!TrySendScanTargetSelected())
			{
				return;
			}

			_scanSelectionSent = true;
			if (_inGameNavigation != null)
			{
				_inGameNavigation.SetNavigationText(InGameConsts.ScanPhaseNavigationText.WaitingOtherPlayers);
			}
			_ScanUIController?.SetNextButtonVisible(false);
		}

		private void EnterOpponentScanStep()
		{
			_ScanUIController?.SetTargetConfirmationPanelVisible(false);
			SetInGameNavigationActive(true);
			if (_inGameNavigation != null)
			{
				_inGameNavigation.SetNavigationText(InGameConsts.ScanPhaseNavigationText.SelectOpponentTarget);
			}
			SubscribeOpponentTargetCardClicks();
		}

		private void SubscribeOpponentTargetCardClicks()
		{
			_scanTargetCardClickDisposables.Dispose();
			_scanTargetCardClickDisposables = new();

			if (_gameContext?.Players == null || _gameContext.UserPlayer == null)
			{
				return;
			}

			foreach (var player in _gameContext.Players)
			{
				if (player.Id == _gameContext.UserPlayer.Id)
				{
					continue;
				}

				if (player.Target?.Cards == null || player.Target.Cards.Count == 0)
				{
					continue;
				}

				var targetCard = player.Target.Cards[0];
				if (!CardViewRegistry.TryGetView(targetCard, out var targetCardView) || targetCardView == null)
				{
					continue;
				}

				var selectedPlayerId = player.Id;
				var selectedSuit = targetCard.Suit;
				targetCardView.Clicked
					.Subscribe(_ => OnOpponentTargetSelected(selectedPlayerId, selectedSuit))
					.AddTo(_scanTargetCardClickDisposables);
			}
		}

		private void OnOpponentTargetSelected(PlayerId playerId, Suit suit)
		{
			if (_scanOpponentSuitRevealed || _scanSelectionSent)
			{
				return;
			}

			_scanSelectedTargetPlayerId = playerId;
			_scanSelectedTargetSuit = suit;
			_scanOpponentSelected = true;

			if (_inGameNavigation != null)
			{
				_inGameNavigation.SetNavigationText(string.Format(
					InGameConsts.ScanPhaseNavigationText.ConfirmOpponentTargetSuitFormat,
					playerId.Value));
			}
		}

		/// <summary>
		/// 選択済みターゲットのスートをNext操作で初めて表示し、以降の再選択を受け付けない。
		/// </summary>
		private void RevealSelectedOpponentTargetSuit()
		{
			_scanOpponentSuitRevealed = true;
			_scanTargetCardClickDisposables.Dispose();
			_scanTargetCardClickDisposables = new();

			if (_inGameNavigation == null)
			{
				return;
			}

			_inGameNavigation.SetNavigationText(string.Format(
				InGameConsts.ScanPhaseNavigationText.OpponentTargetSuitRevealedFormat,
				_scanSelectedTargetPlayerId.Value,
				_scanSelectedTargetSuit.GetKatakanaName()));
		}

		private bool TrySendScanTargetSelected()
		{
			if (_gameplayNetwork == null || _gameContext?.UserPlayer == null)
			{
				Debug.LogWarning("InGameUIManager: ScanTargetSelected の送信に必要な参照が不足しています。");
				return false;
			}

			var mapper = _gameplayNetwork.PlayerIdMapper;
			if (!mapper.TryGetActorNumber(_gameContext.UserPlayer.Id, out var selfActor))
			{
				Debug.LogWarning("InGameUIManager: 自PlayerId の ActorNumber 変換に失敗しました。");
				return false;
			}

			if (!mapper.TryGetActorNumber(_scanSelectedTargetPlayerId, out var targetActor))
			{
				Debug.LogWarning("InGameUIManager: 対象PlayerId の ActorNumber 変換に失敗しました。");
				return false;
			}

			var payload = new NetworkDto.ScanTargetSelectedEvent
			{
				sequence = _gameplayNetwork.Sequence.NextSequence(),
				actorPlayerId = selfActor,
				selectedTargetActorNumber = targetActor
			};

			_gameplayNetwork.Broadcaster.Raise(EventCode.ScanTargetSelected, payload);
			return true;
		}

		private void ResetScanSelectionState()
		{
			_scanOwnTargetConfirmed = false;
			_scanOpponentSelected = false;
			_scanOpponentSuitRevealed = false;
			_scanSelectionSent = false;
			_scanSelectedTargetPlayerId = default;
			_scanSelectedTargetSuit = default;
			_scanTargetCardClickDisposables.Dispose();
			_scanTargetCardClickDisposables = new();
			_ScanUIController?.SetTargetConfirmationPanelVisible(true);
			_ScanUIController?.SetNextButtonVisible(true);
		}



		#endregion
	}

}