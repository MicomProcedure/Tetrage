using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core.Ids;
using Tetrage.Core.Contracts;
using Tetrage.Title;
using Tetrage.Data;
using Tetrage.Core.Enums;
namespace Tetrage.UI
{
    /// <summary>
    /// スキャンに関連するパネルの状態を変更するUIコンポーネント
    /// ボタン押下でパネル内のGameObjectやTextを変更する
    /// </summary>
    public class ScanUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private TextMeshProUGUI Text_1;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private ProfileDisplayUI _profileDisplayUI;

        [Header("Button")]
        [SerializeField] private Button NextButton;

        [Header("State Objects")]
        [SerializeField] private Image trumpBackImage;
        [SerializeField] private GameObject inactiveStateObject;
        [SerializeField] private CardImageMapper cardImageMapper;


        #endregion

        #region Private Fields

        private IGameContext _gameContext;
        private PlayerId _playerId;
        private int _playerIconIndex;
        private string _playerName;
        private bool _isScanned = false;
        private GameObject _spawnedTargetCardImage;

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

        #endregion

        #region Initialization

        /// <summary>
        /// GameContext からゲーム開始時のユーザー情報を取得し、スキャン案内テキストを設定する。
        /// </summary>
        public void Initialize(IGameContext gameContext)
        {
            _isScanned = false;
            _gameContext = gameContext;
            var user = gameContext?.UserPlayer;
            if (user == null)
            {
                Debug.LogWarning("ScanUIController: UserPlayer が null のため初期化をスキップします");
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
                    "ターゲットカードの確認を行います。\n" +
                    $"{_playerName}さんは{seatNumber}Pです。\n" +
                    "準備ができたら,「Next」を押してください。";
            }

            if (statusText != null)
            {
                statusText.text = seatLabel;
            }

            if (_profileDisplayUI != null)
            {
                _profileDisplayUI.SetManualInputData(user.IconIndex, user.UserId);
            }
        }

        #endregion

        #region Next Button

        public void OnNextButtonClicked1()
        {
            if (_isScanned)
            {
                return;
            }

            var user = _gameContext?.UserPlayer;
            var targetPile = user?.Target;
            if (targetPile == null || targetPile.Count == 0)
            {
                Debug.LogWarning("ScanUIController: ターゲットカードが存在しません");
                return;
            }

            var card = targetPile.Cards[0];
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

        #endregion

    }
}

