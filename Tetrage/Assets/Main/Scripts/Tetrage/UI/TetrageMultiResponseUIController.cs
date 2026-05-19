using System;
using System.Linq;
using R3;
using Tetrage.Core.Constants;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Tetrage.UI
{
    /// <summary>
    /// TetrageMulti の UI を担うコントローラー。
    /// - 宣言者フェーズ: SelectionStarted を受けて「確定」ボタンを表示する。
    ///   確定ボタン押下で PublishSelectionConfirmed(empty) を呼び出す（Executor が内部選択を使用）。
    /// - 被選択者フェーズ: Host から ResponseRequested が届いた場合にのみ「出す / 出さない」ボタンを表示し、
    ///   選択後に ActionRequestedEventPacket（ResponseOpen / ResponseDecline）を送信する。
    /// </summary>
    public class TetrageMultiResponseUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("宣言者 — 選択確定")]
        [SerializeField] private Button _confirmButton;

        [Header("被選択者 — 応答")]
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _declineButton;

        #endregion

        #region Private Fields

        private IGameContext _gameContext;
        private IGameplayNetworkController _network;
        private PlayerId _localPlayerId;
        private CompositeDisposable _disposables = new();

        #endregion

        #region Public API

        /// <summary>
        /// 初期化。EventBus と Network への参照を受け取る。
        /// </summary>
        public void Initialize(IGameContext gameContext, IGameplayNetworkController network)
        {
            if (!ValidateComponents()) return;

            _gameContext  = gameContext;
            _network      = network;
            _localPlayerId = gameContext.UserPlayer?.Id ?? default;

            SetupButtonHandlers();
            SubscribeToEvents();
            // 初期状態: 親ごと非表示（イベント到着時に親を activate → 個別ボタン表示）
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 購読解除。
        /// </summary>
        public void Teardown()
        {
            _disposables.Dispose();
            _disposables = new();
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            Teardown();
        }

        #endregion

        #region Initialization

        private void SetupButtonHandlers()
        {
            // 宣言者: 確定ボタン（Executor 内の selectedCardIds をそのまま使わせるため空リストを渡す）
            _confirmButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                TetrageMultiDispatcher.PublishSelectionConfirmed(System.Array.Empty<PlayerId>());
                Debug.Log("TetrageMultiResponseUI: 選択確定");
            });

            // 被選択者: 出す / 出さない
            _openButton.onClick.AddListener(() => SendResponse(isOpen: true));
            _declineButton.onClick.AddListener(() => SendResponse(isOpen: false));
        }

        private void SubscribeToEvents()
        {
            // 宣言者: 選択フェーズ開始時に確定ボタンを表示
            // gameObject が inactive でも R3 の Subscribe は発火するため、ここで親を activate する
            TetrageMultiDispatcher.SelectionStarted
                .Subscribe(_ =>
                {
                    gameObject.SetActive(true);
                    _confirmButton.gameObject.SetActive(true);
                    _openButton.gameObject.SetActive(false);
                    _declineButton.gameObject.SetActive(false);
                    Debug.Log("TetrageMultiResponseUI: 選択フェーズ開始 — 確定ボタン表示");
                })
                .AddTo(_disposables);

            // 被選択者: ResponseRequested で出す/出さないボタンを表示
            _gameContext.Events.ActionResult
                .Where(ev => ev.ActionType     == ActionType.TetrageMulti
                          && ev.ActionStatusInt == InGameConsts.TetrageMultiStatus.ResponseRequested
                          && IsLocalParticipant(ev))
                .Subscribe(_ =>
                {
                    gameObject.SetActive(true);
                    _confirmButton.gameObject.SetActive(false);
                    _openButton.gameObject.SetActive(true);
                    _declineButton.gameObject.SetActive(true);
                    Debug.Log("TetrageMultiResponseUI: ResponseRequested — 応答ボタン表示");
                })
                .AddTo(_disposables);
        }

        #endregion

        #region Handlers

        /// <summary>
        /// ローカルが提出フェーズの参加者（親または指名子）かどうか。
        /// </summary>
        private bool IsLocalParticipant(ActionResultEvent ev)
        {
            if (_network?.PlayerIdMapper?.TryGetActorNumber(_localPlayerId, out var localActor) != true)
                return false;

            return ev.ParticipantActorNumbers?.Contains(localActor) == true;
        }

        /// <summary>
        /// 応答を送信してパネルを非表示にする。
        /// </summary>
        private void SendResponse(bool isOpen)
        {
            gameObject.SetActive(false);

            if (_network?.PlayerIdMapper?.TryGetActorNumber(_localPlayerId, out var actorNumber) != true)
            {
                Debug.LogError("TetrageMultiResponseUI: ActorNumber の解決に失敗しました");
                return;
            }

            var statusInt = isOpen
                ? InGameConsts.TetrageMultiStatus.ResponseOpen
                : InGameConsts.TetrageMultiStatus.ResponseDecline;

            var packet = new ActionRequestedEventPacket
            {
                clientSequence  = _network.Sequence.NextSequence(),
                actorPlayerId   = actorNumber,
                actionType      = ActionType.TetrageMulti,
                actionStatusInt = statusInt,
                targetIds       = System.Array.Empty<int>()
            };

            _network.Broadcaster.Raise(EventCode.ActionRequested, packet);
            Debug.Log($"TetrageMultiResponseUI: 応答送信 open={isOpen}");
        }

        #endregion

        #region Validation

        private bool ValidateComponents()
        {
            if (_confirmButton == null)
            {
                Debug.LogError("TetrageMultiResponseUIController: _confirmButton が未設定です");
                return false;
            }
            if (_openButton == null)
            {
                Debug.LogError("TetrageMultiResponseUIController: _openButton が未設定です");
                return false;
            }
            if (_declineButton == null)
            {
                Debug.LogError("TetrageMultiResponseUIController: _declineButton が未設定です");
                return false;
            }
            return true;
        }

        #endregion
    }
}
