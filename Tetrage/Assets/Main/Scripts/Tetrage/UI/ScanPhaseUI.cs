using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Core.Contracts;
using Tetrage.Data;
using Tetrage.Title;
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
        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject _scanNaviObject;
        [SerializeField] private GameObject _targetPanel;
        [SerializeField] private TextMeshProUGUI _naviText;
        [SerializeField] private TextMeshProUGUI _playerLabelText;
        [SerializeField] private ProfileDisplayView _profileDisplayView;

        [Header("Navi text templates")]
        [Tooltip("未設定の Id はコード内のデフォルト文言を使う。同一 Id が複数ある場合は先頭を採用。")]
        [SerializeField] private List<ScanNaviTextEntry> _naviTextEntries;

        [Header("Button")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private GameObject _buttonFrame;

        [Header("State Objects")]
        [SerializeField] private Image _trumpBackImage;
        [SerializeField] private CardImageMapper _cardImageMapper;

        #endregion

        #region Private Fields

        private IGameContext _gameContext;
        private PlayerId _playerId;
        private int _playerIconIndex;
        private string _playerName;
        private bool _isScanned;
        private bool _scanPhaseActive;
        private readonly Subject<Unit> _nextClicked = new();

        #endregion

        #region Navi text (public hooks)

        /// <summary>
        /// Navi 表示直前に全文を加工する。ローカライズ・デバッグ文言差し替えなどに使う。null で未使用。
        /// </summary>
        public Func<string, ScanNaviTextId, string> NaviTextFormatter { get; set; }
        /// <summary>
        /// 自分ターゲット確認後に押された Next を通知する。
        /// </summary>
        public Observable<Unit> NextClicked => _nextClicked;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextButtonClicked);
            }
        }

        private void OnDestroy()
        {
            ResetScanPhaseUIState();
            _nextClicked.Dispose();
        }

        #endregion

        #region Initialization (called from InGameUIManager)

        /// <summary>
        /// 表示用の参照を保持する。イベント購読は行わない。
        /// </summary>
        public void Initialize(IGameContext gameContext)
        {
            if (!ValidateInitialize(gameContext))
            {
                return;
            }

            _isScanned = false;
            _gameContext = gameContext;

            var user = gameContext.UserPlayer;
            _playerId = user.Id;
            _playerIconIndex = user.IconIndex;
            _playerName = user.UserId ?? string.Empty;

            int seatNumber = user.Id.Value;
            string seatLabel = $"{seatNumber}P";

            _naviText.richText = true;
            SetNaviText(ScanNaviTextId.InitializeBoot, _playerName, seatNumber);

            _playerLabelText.text = seatLabel;
            _profileDisplayView.SetProfile(user.IconIndex, user.UserId);

            ResetScanPhaseUIState();
        }

        /// <summary>
        /// InGameUIManager の Teardown から呼ぶ。Scan 中の内部状態のみリセットする。
        /// </summary>
        public void ResetScanPhaseUIState()
        {
            _scanPhaseActive = false;
            _isScanned = false;
            SetTargetConfirmationPanelVisible(true);
            SetNextButtonVisible(true);
        }

        #endregion

        #region Scan phase UI (called from InGameUIManager on domain events)

        /// <summary>ScanPhaseStarted 受信時の見た目・選択リストの準備。</summary>
        public void ApplyScanPhaseStarted(ScanPhaseStartedEvent e)
        {
            _scanPhaseActive = true;
            _isScanned = false;
            SetTargetConfirmationPanelVisible(true);
            SetNextButtonVisible(true);

            SetNaviText(ScanNaviTextId.ScanPhaseAskOwnTarget, _playerName, _playerId.Value);
        }

        /// <summary>ScanPhaseEnded 受信時の見た目リセット。</summary>
        public void ApplyScanPhaseEnded()
        {
            _scanPhaseActive = false;
            SetTargetConfirmationPanelVisible(true);
            SetNextButtonVisible(true);
        }

        /// <summary>ScanResultReceived 受信時の結果表示。</summary>
        public void ApplyScanResultReceived(ScanResultReceivedEvent e)
        {
            if (!ValidateScanResultContext())
            {
                return;
            }

            string suitLabel = e.TargetSuit.GetKatakanaName();
            string suitColorHex = GetSuitTextColorHex(e.TargetSuit);
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

        #endregion

        #region Next Button

        /// <summary>Prefab の Button からの別名（既存シリアライズ互換）。</summary>

        public void OnNextButtonClicked()
        {
            if (_scanPhaseActive)
            {
                if (!_isScanned)
                {
                    if (!RevealOwnTargetCardForScanPhase())
                    {
                        return;
                    }

                    _isScanned = true;
                    return;
                }

                _nextClicked.OnNext(Unit.Default);
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
            string suitColorHex = GetSuitTextColorHex(suit);
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
            string suitColorHex = GetSuitTextColorHex(suit);
            SetNaviText(ScanNaviTextId.ScanPhaseOwnTargetRevealed, BuildColoredSuitRichText(suitLabel, suitColorHex));

            return true;
        }

        #endregion

        #region Navi text helpers

        private static string BuildColoredSuitRichText(string suitLabel, string colorHexNoHash)
        {
            return $"<color=#{colorHexNoHash}>{suitLabel}</color>";
        }

        private static string GetSuitTextColorHex(Suit suit)
        {
            var color = suit.IsRed() ? Color.red : Color.black;
            return ColorUtility.ToHtmlStringRGBA(color);
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
                    return "ターゲットカードの確認を行います。\n{0}さんは{1}Pです。\n準備ができたら「Next」を押してください。";
                case ScanNaviTextId.ScanPhaseAskOwnTarget:
                    return "ターゲットカードの確認を行います。\n{0}さんは{1}Pです。\n準備ができたら「Next」を押してください。";
                case ScanNaviTextId.ScanPhaseOwnTargetRevealed:
                    return "あなたのターゲットは、{0}です。\nターゲットを覚えて、そのまま「Next」を押してください。";
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

        private bool ValidateInitialize(IGameContext gameContext)
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

            if (_profileDisplayView == null)
            {
                Debug.LogError("ScanPhaseUI.Initialize: _profileDisplayUI が未設定です。Inspector で割り当ててください。");
                ok = false;
            }

            return ok;
        }

        private bool ValidateScanResultContext()
        {
            return _gameContext?.UserPlayer != null;
        }
        
        /// <summary>
        /// ターゲット確認パネルのみ表示/非表示を切り替える。
        /// </summary>
        public void SetTargetConfirmationPanelVisible(bool visible)
        {
            if (_scanNaviObject != null)
            {
                _scanNaviObject.SetActive(visible);
                return;
            }

            if (_targetPanel != null)
            {
                _targetPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// ScanPhaseの進行用Nextボタンの表示を切り替える。
        /// </summary>
        public void SetNextButtonVisible(bool visible)
        {
            if (_nextButton == null) { return; }

            _nextButton.gameObject.SetActive(visible);

            if (_buttonFrame != null)   // ボタンの枠線も表示/非表示を切り替える
            {
                _buttonFrame.SetActive(visible);
            }
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
