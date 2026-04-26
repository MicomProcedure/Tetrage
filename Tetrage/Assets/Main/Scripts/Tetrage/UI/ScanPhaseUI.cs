using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Core.Contracts;
using Tetrage.Network.Gameplay;
using Tetrage.Data;
using NetworkDto = Tetrage.Network.Gameplay;
using Tetrage.Title;
using Tetrage.Services;
using R3;

namespace Tetrage.UI
{
    /// <summary>NaviText 用テンプレートの識別子。各値の {0}{1}… は <see cref="ScanPhaseUI"/> の XML コメントと SetNaviText 呼び出しを参照。</summary>
    public enum ScanNaviTextId
    {
        /// <summary>{0}=プレイヤー名, {1}=席番号(int)</summary>
        InitializeBoot = 0,
        /// <summary>{0}=プレイヤー名, {1}=席番号(int)</summary>
        ScanPhaseAskOwnTarget = 1,
        /// <summary>{0}=スート表示（色付き rich text 断片）</summary>
        ScanPhaseOwnTargetRevealed = 2,
        /// <summary>{0}=プレイヤー名, {1}=席番号(int)</summary>
        ScanPhaseSelectOpponent = 3,
        /// <summary>引数なし</summary>
        ScanPhaseSentWaiting = 4,
        /// <summary>{0}=偵察対象の席番号(int), {1}=スート表示（色付き rich text 断片）</summary>
        ScanPhaseResult = 5,
        /// <summary>{0}=スート表示（色付き rich text 断片）</summary>
        IdleOwnTargetRevealed = 6,
    }

    /// <summary>Inspector で Navi 文言を差し替えるための 1 行。</summary>
    [Serializable]
    public sealed class ScanNaviTextEntry
    {
        public ScanNaviTextId Id;
        [TextArea(2, 8)]
        public string Template;
    }

    /// <summary>
    /// Scan フェーズ専用の見た目と入力のみ。イベント購読は <see cref="Tetrage.Managers.InGameUIManager"/> が行い、本クラスへ委譲する。
    /// </summary>
    public class ScanPhaseUI : MonoBehaviour
    {
        private readonly struct OpponentTargetCardClick
        {
            public CardView CardView { get; }
            public PlayerId TargetPlayerId { get; }

            public OpponentTargetCardClick(CardView cardView, PlayerId targetPlayerId)
            {
                CardView = cardView;
                TargetPlayerId = targetPlayerId;
            }
        }

        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject _targetPanel;
        [SerializeField] private TextMeshProUGUI _naviText;
        [SerializeField] private TextMeshProUGUI _playerLabelText;
        [SerializeField] private ProfileDisplayUI _profileDisplayUI;

        [Header("Navi text templates")]
        [Tooltip("未設定の Id はコード内のデフォルト文言を使う。同一 Id が複数ある場合は先頭を採用。")]
        [SerializeField] private List<ScanNaviTextEntry> _naviTextEntries;

        [Header("Button")]
        [SerializeField] private Button _nextButton;

        [Header("State Objects")]
        [SerializeField] private Image _trumpBackImage;
        [SerializeField] private GameObject _inactiveStateObject;
        [SerializeField] private CardImageMapper _cardImageMapper;

        #endregion

        #region Private Fields

        private IGameContext _gameContext;
        private IGameplayNetworkController _networkController;
        private PlayerId _playerId;
        private int _playerIconIndex;
        private string _playerName;
        private bool _isScanned;
        private GameObject _spawnedTargetCardImage;

        private readonly Dictionary<CardView, Action> _opponentCardClickHandlers = new();
        private readonly Subject<OpponentTargetCardClick> _opponentTargetCardClickSubject = new();
        private CompositeDisposable _opponentTargetCardClickDisposables = new();
        private bool _isOpponentTargetCardClickSubscribed;
        private bool _scanPhaseActive;
        private bool _scanTargetSent;
        private bool _ownTargetAcknowledgedForScan;
        private bool _hasSelectedOpponentTarget;
        private PlayerId _selectedTargetPlayerId;
        private CardView _selectedTargetCardView;

        #endregion

        #region Navi text (public hooks)

        /// <summary>
        /// Navi 表示直前に全文を加工する。ローカライズ・デバッグ文言差し替えなどに使う。null で未使用。
        /// </summary>
        public Func<string, ScanNaviTextId, string> NaviTextFormatter { get; set; }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextButtonClicked1);
            }
        }

        private void OnDisable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextButtonClicked1);
            }
        }

        private void OnDestroy()
        {
            ResetScanPhaseUiState();
            _opponentTargetCardClickDisposables.Dispose();
            _opponentTargetCardClickSubject.Dispose();
        }

        #endregion

        #region Initialization (called from InGameUIManager)

        /// <summary>
        /// 表示用の参照を保持する。イベント購読は行わない。
        /// </summary>
        public void Initialize(IGameContext gameContext, IGameplayNetworkController networkController)
        {
            if (!ValidateInitialize(gameContext, networkController))
            {
                return;
            }

            _isScanned = false;
            _gameContext = gameContext;
            _networkController = networkController;

            var user = gameContext.UserPlayer;
            _playerId = user.Id;
            _playerIconIndex = user.IconIndex;
            _playerName = user.UserId ?? string.Empty;

            int seatNumber = user.Id.Value;
            string seatLabel = $"{seatNumber}P";

            _naviText.richText = true;
            SetNaviText(ScanNaviTextId.InitializeBoot, _playerName, seatNumber);

            _playerLabelText.text = seatLabel;
            _profileDisplayUI.SetManualInputData(user.IconIndex, user.UserId);
            SubscribeOpponentTargetCardClickStream();

            ResetScanPhaseUiState();
        }

        /// <summary>
        /// InGameUIManager の Teardown から呼ぶ。Scan 中の内部状態のみリセットする。
        /// </summary>
        public void ResetScanPhaseUiState()
        {
            _scanPhaseActive = false;
            _scanTargetSent = false;
            _ownTargetAcknowledgedForScan = false;
            ResetOpponentTargetSelection();
            ReleaseOpponentTargetCardSelection();
        }

        #endregion

        #region Scan phase UI (called from InGameUIManager on domain events)

        /// <summary>ScanPhaseStarted 受信時の見た目・選択リストの準備。</summary>
        public void ApplyScanPhaseStarted(ScanPhaseStartedEvent e)
        {
            _scanPhaseActive = true;
            _scanTargetSent = false;
            _isScanned = false;
            _ownTargetAcknowledgedForScan = false;
            ResetOpponentTargetSelection();

            BuildOpponentSelectionView();

            SetNaviText(ScanNaviTextId.ScanPhaseAskOwnTarget, _playerName, _playerId.Value);
        }

        /// <summary>ScanPhaseEnded 受信時の見た目リセット。</summary>
        public void ApplyScanPhaseEnded()
        {
            _scanPhaseActive = false;
            _scanTargetSent = false;
            _ownTargetAcknowledgedForScan = false;
            ResetOpponentTargetSelection();
            ReleaseOpponentTargetCardSelection();
        }

        /// <summary>ScanResultReceived 受信時の結果表示。</summary>
        public void ApplyScanResultReceived(ScanResultReceivedEvent e)
        {
            if (!ValidateScanResultContext())
            {
                return;
            }

            string suitLabel = e.TargetSuit.GetKatakanaName();
            string suitColorHex = ColorUtility.ToHtmlStringRGBA(Color.red);
            string suitColored = BuildColoredSuitRichText(suitLabel, suitColorHex);
            SetNaviText(ScanNaviTextId.ScanPhaseResult, e.TargetPlayerId.Value, suitColored);

            if (_cardImageMapper != null && _trumpBackImage != null)
            {
                var sprite = _cardImageMapper.GetCardSprite(e.TargetSuit, 1);
                if (sprite != null)
                {
                    _trumpBackImage.sprite = sprite;
                }
            }

            _isScanned = true;
        }

        /// <summary>相手カード選択リストの表示を更新する。</summary>
        private void BuildOpponentSelectionView()
        {
            ReleaseOpponentTargetCardSelection();   // 相手カード選択リストの初期化
            if (!ValidateBuildOpponentSelectionContext())   // 相手カード選択リストのバリデーション
            {
                return;
            }

            foreach (var p in _gameContext.Players)   // 全プレイヤーに対して相手カード選択リストの表示を更新
            {
                if (p.Id == _gameContext.UserPlayer.Id) continue;

                if (!TryGetTargetCardView(p, out var targetCardView))
                {
                    Debug.LogWarning($"ScanPhaseUI: PlayerId={p.Id.Value} のターゲットカードViewが見つからないため選択候補に含めません。");
                    continue;
                }

                var targetPlayerId = p.Id;  // 相手プレイヤーID
                Action onClicked = () => _opponentTargetCardClickSubject.OnNext(new OpponentTargetCardClick(targetCardView, targetPlayerId));
                _opponentCardClickHandlers[targetCardView] = onClicked;
                targetCardView.Clicked += onClicked;   // 相手カード選択リストの表示を更新
                targetCardView.Unhighlight();
            }
        }

        #endregion

        #region Next Button

        /// <summary>Prefab の Button からの別名（既存シリアライズ互換）。</summary>
        public void OnNextButtonClicked() => OnNextButtonClicked1();

        public void OnNextButtonClicked1()
        {
            if (_scanPhaseActive && !_scanTargetSent)
            {
                if (!_ownTargetAcknowledgedForScan)
                {
                    if (!RevealOwnTargetCardForScanPhase())
                    {
                        return;
                    }

                    _ownTargetAcknowledgedForScan = true;
                    SetNaviText(ScanNaviTextId.ScanPhaseSelectOpponent, _playerName, _playerId.Value);
                    return;
                }

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

            if (_cardImageMapper == null)
            {
                Debug.LogWarning("ScanPhaseUI: CardImageMapper が未設定です");
            }
            else if (_trumpBackImage != null)
            {
                var face = _cardImageMapper.GetCardSprite(suit, card.Number);
                if (face != null)
                {
                    _trumpBackImage.sprite = face;
                }
                else
                {
                    Debug.LogWarning($"ScanPhaseUI: カード画像が見つかりません ({suit} {card.Number})");
                }
            }

            string suitLabel = suit.GetKatakanaName();
            string suitColorHex = ColorUtility.ToHtmlStringRGBA(Color.red);
            SetNaviText(ScanNaviTextId.IdleOwnTargetRevealed, BuildColoredSuitRichText(suitLabel, suitColorHex));

            _isScanned = true;
        }

        private bool RevealOwnTargetCardForScanPhase()
        {
            if (!TryGetOwnTargetCard(out var card))
            {
                return false;
            }

            var suit = card.Suit;

            if (_cardImageMapper == null)
            {
                Debug.LogWarning("ScanPhaseUI: CardImageMapper が未設定です");
            }
            else if (_trumpBackImage != null)
            {
                var face = _cardImageMapper.GetCardSprite(suit, card.Number);
                if (face != null)
                {
                    _trumpBackImage.sprite = face;
                }
                else
                {
                    Debug.LogWarning($"ScanPhaseUI: カード画像が見つかりません ({suit} {card.Number})");
                }
            }

            string suitLabel = suit.GetKatakanaName();
            string suitColorHex = ColorUtility.ToHtmlStringRGBA(Color.red);
            SetNaviText(ScanNaviTextId.ScanPhaseOwnTargetRevealed, BuildColoredSuitRichText(suitLabel, suitColorHex));

            return true;
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

            SetNaviText(ScanNaviTextId.ScanPhaseSentWaiting);
        }

        #endregion

        #region Navi text helpers

        private static string BuildColoredSuitRichText(string suitLabel, string colorHexNoHash)
        {
            return $"<color=#{colorHexNoHash}>{suitLabel}</color>";
        }

        private string GetNaviTemplate(ScanNaviTextId id)
        {
            if (_naviTextEntries != null)
            {
                foreach (var entry in _naviTextEntries)
                {
                    if (entry != null && entry.Id == id && !string.IsNullOrEmpty(entry.Template))
                    {
                        return entry.Template;
                    }
                }
            }

            return GetBuiltinNaviTemplate(id);
        }

        private static string GetBuiltinNaviTemplate(ScanNaviTextId id)
        {
            switch (id)
            {
                case ScanNaviTextId.InitializeBoot:
                    return "ScanPhase で偵察する相手を選び、Next を押してください。\n{0}さんは{1}Pです。";
                case ScanNaviTextId.ScanPhaseAskOwnTarget:
                    return "まず Next で自分のターゲットカードを確認してください。\n（{0} / {1}P）";
                case ScanNaviTextId.ScanPhaseOwnTargetRevealed:
                    return "あなたのターゲットカードは、{0}です。\n確認できたら相手を選択して Next を押してください。";
                case ScanNaviTextId.ScanPhaseSelectOpponent:
                    return "偵察する相手のターゲットカードをフィールド上でクリックし、もう一度 Next を押してください。\n（{0} / {1}P）";
                case ScanNaviTextId.ScanPhaseSentWaiting:
                    return "偵察対象を送信しました。結果を待っています…";
                case ScanNaviTextId.ScanPhaseResult:
                    return "偵察結果: プレイヤー {0}P のターゲットカードのスートは {1} です。";
                case ScanNaviTextId.IdleOwnTargetRevealed:
                    return "あなたのターゲットカードは、{0}です。\nターゲットを覚えて、そのまま「Next」を押してください。";
                default:
                    return string.Empty;
            }
        }

        private void SetNaviText(ScanNaviTextId id, params object[] args)
        {
            if (_naviText == null)
            {
                return;
            }

            string tpl = GetNaviTemplate(id);
            string body;
            try
            {
                body = args != null && args.Length > 0 ? string.Format(tpl, args) : tpl;
            }
            catch (FormatException ex)
            {
                Debug.LogWarning($"ScanPhaseUI: Navi テンプレートの Format に失敗 Id={id} — {ex.Message}");
                body = tpl;
            }

            if (NaviTextFormatter != null)
            {
                try
                {
                    body = NaviTextFormatter(body, id) ?? body;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ScanPhaseUI: NaviTextFormatter で例外 — {ex.Message}");
                }
            }

            _naviText.text = body;
        }

        #endregion

        #region Validation

        private bool ValidateInitialize(IGameContext gameContext, IGameplayNetworkController networkController)
        {
            bool ok = true;

            if (gameContext == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: gameContext が null です。");
                ok = false;
            }

            if (gameContext != null && gameContext.UserPlayer == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: gameContext.UserPlayer が null です。");
                ok = false;
            }

            if (networkController == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: networkController (IGameplayNetworkController) が null です。");
                ok = false;
            }

            if (_naviText == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: _naviText が未設定です。Inspector で割り当ててください。");
                ok = false;
            }

            if (_playerLabelText == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: _playerLabelText が未設定です。Inspector で割り当ててください。");
                ok = false;
            }

            if (_profileDisplayUI == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: _profileDisplayUI が未設定です。Inspector で割り当ててください。");
                ok = false;
            }

            return ok;
        }

        private bool ValidateScanTargetSelection(out int selfActor, out int targetActor)
        {
            selfActor = default;
            targetActor = default;

            if (_networkController == null || _gameContext?.UserPlayer == null)
            {
                Debug.LogError("ScanPhaseUI: ネットワークまたは UserPlayer が無効です");
                return false;
            }

            if (!_hasSelectedOpponentTarget)
            {
                Debug.LogWarning("ScanPhaseUI: 偵察対象が未選択です。フィールド上の相手ターゲットカードをクリックしてください。");
                return false;
            }

            var targetPlayerId = _selectedTargetPlayerId;
            if (targetPlayerId == _gameContext.UserPlayer.Id)
            {
                Debug.LogWarning("ScanPhaseUI: 自分自身は偵察対象に選択できません。");
                return false;
            }
            var mapper = _networkController.PlayerIdMapper;

            if (!mapper.TryGetActorNumber(_gameContext.UserPlayer.Id, out selfActor))
            {
                Debug.LogError("ScanPhaseUI: 自PlayerId の ActorNumber 変換に失敗しました");
                return false;
            }

            if (!mapper.TryGetActorNumber(targetPlayerId, out targetActor))
            {
                Debug.LogError("ScanPhaseUI: 対象 PlayerId の ActorNumber 変換に失敗しました");
                return false;
            }

            return true;
        }

        private bool ValidateScanResultContext()
        {
            return _gameContext?.UserPlayer != null;
        }

        private bool ValidateBuildOpponentSelectionContext()
        {
            return _gameContext?.Players != null && _gameContext.UserPlayer != null;
        }

        private bool TryGetTargetCardView(IPlayer player, out CardView targetCardView)
        {
            targetCardView = null;
            if (player?.Target?.Cards == null || player.Target.Cards.Count == 0)
            {
                return false;
            }

            var targetCard = player.Target.Cards[0];
            return CardViewRegistry.TryGetView(targetCard, out targetCardView) && targetCardView != null;
        }

        private void SubscribeOpponentTargetCardClickStream()
        {
            if (_isOpponentTargetCardClickSubscribed)
            {
                return;
            }

            _opponentTargetCardClickSubject
                .Subscribe(onClicked => ApplyOpponentTargetCardSelection(onClicked.CardView, onClicked.TargetPlayerId))
                .AddTo(_opponentTargetCardClickDisposables);

            _isOpponentTargetCardClickSubscribed = true;
        }

        private void ApplyOpponentTargetCardSelection(CardView clickedCardView, PlayerId targetPlayerId)
        {
            if (!_scanPhaseActive || _scanTargetSent || !_ownTargetAcknowledgedForScan)
            {
                return;
            }

            if (_selectedTargetCardView != null && _selectedTargetCardView != clickedCardView)
            {
                _selectedTargetCardView.Unhighlight();
            }

            _selectedTargetCardView = clickedCardView;
            _selectedTargetCardView.Highlight();
            _selectedTargetPlayerId = targetPlayerId;
            _hasSelectedOpponentTarget = true;
        }

        private void ResetOpponentTargetSelection()
        {
            if (_selectedTargetCardView != null)
            {
                _selectedTargetCardView.Unhighlight();
            }

            _selectedTargetCardView = null;
            _selectedTargetPlayerId = default;
            _hasSelectedOpponentTarget = false;
        }

        private void ReleaseOpponentTargetCardSelection()
        {
            foreach (var pair in _opponentCardClickHandlers)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                pair.Key.Clicked -= pair.Value;
                pair.Key.Unhighlight();
            }

            _opponentCardClickHandlers.Clear();
        }

        private bool TryGetOwnTargetCard(out Tetrage.Models.Card card)
        {
            card = null;
            var targetPile = _gameContext?.UserPlayer?.Target;
            if (targetPile == null || targetPile.Count == 0)
            {
                Debug.LogWarning("ScanPhaseUI: ターゲットカードが存在しません");
                return false;
            }

            card = targetPile.Cards[0];
            return true;
        }

        #endregion
    }
}
