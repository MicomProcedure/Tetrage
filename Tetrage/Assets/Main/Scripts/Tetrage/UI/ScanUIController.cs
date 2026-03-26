using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using Tetrage.Core;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Core.Contracts;
using Tetrage.Network.Gameplay;
using Tetrage.Title;
using Tetrage.Data;
using NetworkDto = Tetrage.Network.Gameplay;

namespace Tetrage.UI
{
    /// <summary>
    /// ScanPhase 中に偵察対象プレイヤーを選び ScanTargetSelected を送信し、ScanResultReceived でスートを表示する。
    /// </summary>
    public class ScanUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private TextMeshProUGUI Text_1;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private ProfileDisplayUI _profileDisplayUI;

        [Header("Scan target selection")]
        [Tooltip("偵察する相手プレイヤーを選ぶ。未設定の場合は ScanPhase で送信できない。")]
        [SerializeField] private TMP_Dropdown _scanOpponentDropdown;

        [Header("Button")]
        [SerializeField] private Button NextButton;

        [Header("State Objects")]
        [SerializeField] private Image trumpBackImage;
        [SerializeField] private GameObject inactiveStateObject;
        [SerializeField] private CardImageMapper cardImageMapper;

        #endregion

        #region Private Fields

        private IGameContext _gameContext;
        private IGameplayNetworkController _networkController;
        private PlayerId _playerId;
        private int _playerIconIndex;
        private string _playerName;
        private bool _isScanned = false;
        private GameObject _spawnedTargetCardImage;

        private CompositeDisposable _scanDisposables = new();
        private readonly List<PlayerId> _opponentIdsOrdered = new();
        private bool _scanPhaseActive;
        private bool _scanTargetSent;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (NextButton != null)
            {
                NextButton.onClick.AddListener(OnNextButtonClicked1);
            }
        }

        private void OnDisable()
        {
            if (NextButton != null)
            {
                NextButton.onClick.RemoveListener(OnNextButtonClicked1);
            }
        }

        private void OnDestroy()
        {
            TeardownScan();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// GameContext のみ（後方互換）。ネットワーク送信はできない。
        /// </summary>
        public void Initialize(IGameContext gameContext)
        {
            Initialize(gameContext, null);
        }

        /// <summary>
        /// GameContext とネットワークを渡し、ScanPhase イベントを購読する。
        /// </summary>
        public void Initialize(IGameContext gameContext, IGameplayNetworkController networkController)
        {
            _isScanned = false;
            _gameContext = gameContext;
            _networkController = networkController;
            if (!TryGetInitializeUser(gameContext, out var user))
            {
                return;
            }

            _playerId = user.Id;
            _playerIconIndex = user.IconIndex;
            _playerName = user.UserId ?? string.Empty;

            int seatNumber = user.Id.Value;
            string seatLabel = $"{seatNumber}P";

            if (Text_1 != null)
            {
                Text_1.richText = true;
                Text_1.text =
                    "ScanPhase で偵察する相手を選び、「確定」を押してください。\n" +
                    $"{_playerName}さんは{seatNumber}Pです。";
            }

            if (statusText != null)
            {
                statusText.text = seatLabel;
            }

            if (_profileDisplayUI != null)
            {
                _profileDisplayUI.SetManualInputData(user.IconIndex, user.UserId);
            }

            TeardownScan();
            if (_gameContext?.Events != null && _networkController != null)
            {
                _gameContext.Events.ScanPhaseStarted
                    .Subscribe(OnScanPhaseStarted)
                    .AddTo(_scanDisposables);

                _gameContext.Events.ScanPhaseEnded
                    .Subscribe(_ => OnScanPhaseEnded())
                    .AddTo(_scanDisposables);

                _gameContext.Events.ScanResultReceived
                    .Subscribe(OnScanResultReceived)
                    .AddTo(_scanDisposables);
            }
            else if (_networkController == null)
            {
                Debug.LogWarning("ScanUIController: IGameplayNetworkController が未設定のため ScanTargetSelected を送信できません。");
            }
        }

        /// <summary>
        /// InGameUIManager の Teardown から呼ぶ。
        /// </summary>
        public void TeardownScan()
        {
            _scanDisposables.Dispose();
            _scanDisposables = new CompositeDisposable();
            _scanPhaseActive = false;
            _scanTargetSent = false;
            _opponentIdsOrdered.Clear();
        }

        #endregion

        #region Scan phase (event-driven)

        private void OnScanPhaseStarted(ScanPhaseStartedEvent e)
        {
            _scanPhaseActive = true;
            _scanTargetSent = false;
            _isScanned = false;

            BuildOpponentDropdown();

            if (Text_1 != null)
            {
                Text_1.text =
                    "偵察する相手プレイヤーをドロップダウンで選び、「確定」を押してください。\n" +
                    $"（{_playerName} / {_playerId.Value}P）";
            }

            if (trumpBackImage != null && cardImageMapper != null)
            {
                // 結果表示まで裏面のままにできる
            }
        }

        private void OnScanPhaseEnded()
        {
            _scanPhaseActive = false;
            _scanTargetSent = false;
            _opponentIdsOrdered.Clear();
        }

        private void OnScanResultReceived(ScanResultReceivedEvent e)
        {
            if (!ValidateScanResultContext()) return;

            string suitLabel = e.TargetSuit.GetKatakanaName();
            string suitColorHex = ColorUtility.ToHtmlStringRGBA(Color.red);

            if (Text_1 != null)
            {
                Text_1.text =
                    $"偵察結果: プレイヤー {e.TargetPlayerId.Value}P のターゲットカードのスートは " +
                    $"<color=#{suitColorHex}>{suitLabel}</color> です。";
            }

            if (cardImageMapper != null && trumpBackImage != null)
            {
                // スートのみ分かるため、代表として A の画像を出す（番号は仕様に合わせて差し替え可）
                var sprite = cardImageMapper.GetCardSprite(e.TargetSuit, 1);
                if (sprite != null)
                {
                    trumpBackImage.sprite = sprite;
                }
            }

            _isScanned = true;
        }

        private void BuildOpponentDropdown()
        {
            _opponentIdsOrdered.Clear();
            if (!ValidateBuildOpponentDropdownContext())
            {
                return;
            }

            _scanOpponentDropdown.ClearOptions();
            var labels = new List<string>();
            foreach (var p in _gameContext.Players)
            {
                if (p.Id == _gameContext.UserPlayer.Id) continue;
                _opponentIdsOrdered.Add(p.Id);
                labels.Add(string.IsNullOrEmpty(p.UserId) ? $"{p.Id.Value}P" : $"{p.UserId} ({p.Id.Value}P)");
            }

            _scanOpponentDropdown.AddOptions(labels);
            if (labels.Count > 0)
            {
                _scanOpponentDropdown.value = 0;
            }
        }

        #endregion

        #region Next Button

        public void OnNextButtonClicked1()
        {
            if (_scanPhaseActive && !_scanTargetSent)
            {
                TrySendScanTargetSelected();
                return;
            }

            if (_isScanned)
            {
                return;
            }

            RevealOwnTargetCard();
        }

        private void RevealOwnTargetCard()
        {
            if (!TryGetOwnTargetCard(out var card))
            {
                return;
            }

            var suit = card.Suit;

            if (cardImageMapper == null)
            {
                Debug.LogWarning("ScanUIController: CardImageMapper が未設定です");
            }
            else if (trumpBackImage != null)
            {
                var face = cardImageMapper.GetCardSprite(suit, card.Number);
                if (face != null)
                {
                    trumpBackImage.sprite = face;
                }
                else
                {
                    Debug.LogWarning($"ScanUIController: カード画像が見つかりません ({suit} {card.Number})");
                }
            }

            if (Text_1 != null)
            {
                string suitLabel = suit.GetKatakanaName();
                string suitColorHex = ColorUtility.ToHtmlStringRGBA(Color.red);
                Text_1.text =
                    $"あなたのターゲットカードは、<color=#{suitColorHex}>{suitLabel}</color>です。\n" +
                    "ターゲットを覚えて、そのまま「Next」を押してください。";
            }

            _isScanned = true;
        }

        private void TrySendScanTargetSelected()
        {
            if (!ValidateScanTargetSelection(out var selfActor, out var targetActor))
            {
                return;
            }

            var payload = new NetworkDto.ScanTargetSelectedEvent
            {
                sequence = _networkController.Sequence.NextSequence(),
                actorPlayerId = selfActor,
                selectedTargetActorNumber = targetActor
            };
            _networkController.Broadcaster.Raise(EventCode.ScanTargetSelected, payload);
            _scanTargetSent = true;

            if (Text_1 != null)
            {
                Text_1.text = "偵察対象を送信しました。結果を待っています…";
            }
        }

        #endregion

        #region Validation
        private bool ValidateScanTargetSelection(out int selfActor, out int targetActor)
        {
            selfActor = default;
            targetActor = default;

            if (_networkController == null || _gameContext?.UserPlayer == null)
            {
                Debug.LogError("ScanUIController: ネットワークまたは UserPlayer が無効です");
                return false;
            }

            if (_scanOpponentDropdown == null || _opponentIdsOrdered.Count == 0)
            {
                Debug.LogError("ScanUIController: 偵察対象ドロップダウンが未設定か、候補がありません。Inspector で TMP_Dropdown を割り当ててください。");
                return false;
            }

            int idx = _scanOpponentDropdown.value;
            if (idx < 0 || idx >= _opponentIdsOrdered.Count)
            {
                Debug.LogWarning("ScanUIController: ドロップダウンの選択が無効です");
                return false;
            }

            var targetPlayerId = _opponentIdsOrdered[idx];
            var mapper = _networkController.PlayerIdMapper;

            if (!mapper.TryGetActorNumber(_gameContext.UserPlayer.Id, out selfActor))
            {
                Debug.LogError("ScanUIController: 自PlayerId の ActorNumber 変換に失敗しました");
                return false;
            }

            if (!mapper.TryGetActorNumber(targetPlayerId, out targetActor))
            {
                Debug.LogError("ScanUIController: 対象 PlayerId の ActorNumber 変換に失敗しました");
                return false;
            }

            return true;
        }

        private bool TryGetInitializeUser(IGameContext gameContext, out IPlayer user)
        {
            user = gameContext?.UserPlayer;
            if (user != null)
            {
                return true;
            }

            Debug.LogWarning("ScanUIController: UserPlayer が null のため初期化をスキップします");
            return false;
        }

        private bool ValidateScanResultContext()
        {
            return _gameContext?.UserPlayer != null;
        }

        private bool ValidateBuildOpponentDropdownContext()
        {
            return _scanOpponentDropdown != null && _gameContext?.Players != null && _gameContext.UserPlayer != null;
        }

        private bool TryGetOwnTargetCard(out Tetrage.Models.Card card)
        {
            card = null;
            var targetPile = _gameContext?.UserPlayer?.Target;
            if (targetPile == null || targetPile.Count == 0)
            {
                Debug.LogWarning("ScanUIController: ターゲットカードが存在しません");
                return false;
            }

            card = targetPile.Cards[0];
            return true;
        }

        #endregion
    }
}
