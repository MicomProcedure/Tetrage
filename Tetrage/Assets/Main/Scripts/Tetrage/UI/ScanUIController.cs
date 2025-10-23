using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core.DTO;
using Tetrage.Core.Ids;
using Tetrage.Core.Contracts;
using Tetrage.Title;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Enums;
using Tetrage.Data;
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
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private ProfileDisplayUI _profileDisplayUI;

        [Header("Button")]
        [SerializeField] private Button NextButton;

        [Header("State Objects")]
        [SerializeField] private Image trumpBackImage;
        [SerializeField] private GameObject inactiveStateObject;
        [SerializeField] private CardImageMapper cardImageMapper;
        [SerializeField] private RectTransform targetCardAnchor; // 表示位置・サイズの参照

        [Header("Text")]
        [SerializeField] private List<string> _lineTexts = new List<string>(){
            "ターゲットカードの確認を行います。\n{_playerName}さんです。\n準備ができたら、「NEXT」を押してください。"
        };

        [Header("Target Check Text (Integrated)")]
        [SerializeField] private TextMeshProUGUI targetText; // 統合: 変更対象のTextMeshPro
        [SerializeField] private GameObject targetCheckButton; // 統合: ボタンGameObject
        [TextArea(2, 5)]
        [SerializeField] private string targetNewText = "新しいテキスト"; // 統合: インスペクタから変更可能なテキスト
		[SerializeField] private Color suitTextColor = new Color(0.9f, 0.1f, 0.1f); // スート表示色（RichText）
		[SerializeField] private Color redSuitColor = new Color(0.9f, 0.1f, 0.1f);
		[SerializeField] private Color blackSuitColor = new Color(0.1f, 0.1f, 0.1f);

        #endregion

        #region Private Fields

        private IGameContextProvider _gameContext;
        private PlayerId _playerId;
        private int _playerIconIndex;
        private string _playerName;
        private bool _isScanned = false;
        private GameObject _spawnedTargetCardImage;

        #endregion


        #region Public Methods

        /// <summary>
        /// ゲーム開始時にPlayerInfoを設定して初期化
        /// </summary>
        /// <param name="playerInfo">プレイヤー情報</param>
        public void Initialize(IGameContextProvider gameContext)
        {
            _gameContext = gameContext;
            _playerId = _gameContext.UserPlayer.Id;
            
            // プレイヤー番号を取得（0始まりのIDを1始まりの表示用番号に変換）
            _playerName = _gameContext.UserPlayer.UserId;
            _playerIconIndex = _gameContext.UserPlayer.IconIndex;
            _profileDisplayUI.InitializeDisplay();
            
            // 初期状態を設定
            UpdatePanelState();
            
            Debug.Log($"ScanUI: {_playerName}のプレイヤー情報を設定しました（UserId: {_playerName}）");

            // ターゲットカードの画像を生成

        }

        /// <summary>
        /// パネルの状態をリセット
        /// </summary>
        public void ResetState()
        {
            _isScanned = false;
            UpdatePanelState();
        }

        #region Button Methods

        /// <summary>
        /// TargetCheckText(ChangeText) の統合。対象テキストを書き換える。
        /// </summary>
		public void ChangeText()
        {
            if (targetText != null)
            {
				var targetCard = _gameContext.UserPlayer.Target.FirstOrDefault();
				var suitEnum = targetCard != null ? (Suit?)targetCard.Suit : null;
				var suitText = suitEnum?.ToString() ?? "";
				// Suit の色は拡張メソッド IsRed / IsBlack を使用
				Color applyColor = suitTextColor;
				if (suitEnum.HasValue)
				{
					applyColor = suitEnum.Value.IsRed() ? redSuitColor : blackSuitColor;
				}
				var hex = ColorUtility.ToHtmlStringRGB(applyColor);
				var coloredSuit = $"<color=#{hex}>{suitText}</color>";
				targetText.text = targetNewText.Replace("{Suit}", coloredSuit);

            }
            else
            {
                Debug.LogWarning("ScanUIController: targetText が設定されていません！", this);
            }
        }
        

        /// <summary>
        /// TargetCheckText(Finish) の統合。ボタンを非表示にする。
        /// </summary>
        public void Finish()
        {
            // 優先: 指定のターゲット用ボタン
            if (targetCheckButton != null)
            {
                var btn = targetCheckButton.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
                targetCheckButton.SetActive(false);
                return;
            }

            // フォールバック: NextButton を無効化
            if (NextButton != null)
            {
                NextButton.interactable = false;
                NextButton.gameObject.SetActive(false);
                return;
            }

        }

        /// <summary>
        /// プレイヤーのターゲットカードImageを生成し、アンカーTransformに追従した位置・サイズで表示
        /// </summary>
        public void SpawnTargetCardImage()
        {
            if (cardImageMapper == null || targetCardAnchor == null)
            {
                Debug.LogWarning("ScanUIController: cardImageMapper または targetCardAnchor が設定されていません", this);
                return;
            }

            var targetCard = _gameContext.UserPlayer.Target.FirstOrDefault();
            if (targetCard == null)
            {
                Debug.LogWarning("ScanUIController: ユーザーのターゲットカードが見つかりません", this);
                return;
            }

            var sprite = cardImageMapper.GetCardSprite(targetCard.Suit, targetCard.Number);
            if (sprite == null)
            {
                Debug.LogWarning($"ScanUIController: マッパーにスプライトがありません Suit={targetCard.Suit} Number={targetCard.Number}", this);
                return;
            }

            var go = new GameObject("TargetCardImage");
            go.transform.SetParent(targetCardAnchor, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;

            // アンカーが Image を持っていれば、関連プロパティを継承
            var anchorImg = targetCardAnchor.GetComponent<Image>();
            if (anchorImg != null)
            {
                img.preserveAspect = anchorImg.preserveAspect;
                img.raycastTarget = anchorImg.raycastTarget;
                img.color = anchorImg.color;
                img.material = anchorImg.material;
                img.maskable = anchorImg.maskable;
                img.type = anchorImg.type;
                img.fillCenter = anchorImg.fillCenter;
                img.fillMethod = anchorImg.fillMethod;
                img.fillAmount = anchorImg.fillAmount;
                img.fillClockwise = anchorImg.fillClockwise;
                img.fillOrigin = anchorImg.fillOrigin;
            }

            var rt = img.rectTransform;
            rt.anchorMin = targetCardAnchor.anchorMin;
            rt.anchorMax = targetCardAnchor.anchorMax;
            rt.pivot = targetCardAnchor.pivot;
            rt.sizeDelta = targetCardAnchor.sizeDelta;
            rt.localRotation = targetCardAnchor.localRotation;
            Debug.Log($"ScanUIController: ターゲットカード画像を生成しました Suit={targetCard.Suit} Number={targetCard.Number}");

            // 参照を保持（既存があれば置き換え）
            if (_spawnedTargetCardImage != null && _spawnedTargetCardImage != go)
            {
                Destroy(_spawnedTargetCardImage);
            }
            _spawnedTargetCardImage = go;
        }

        /// <summary>
        /// 生成済みターゲットカード画像を削除（UIボタン用）
        /// </summary>
        public void DeleteTargetCardImage()
        {
            if (_spawnedTargetCardImage != null)
            {
                Destroy(_spawnedTargetCardImage);
                _spawnedTargetCardImage = null;
                Debug.Log("ScanUIController: ターゲットカード画像を削除しました");
            }
        }
        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// スキャンボタンが押された時の処理
        /// </summary>
        private void OnNextButtonClicked()
        {
            _isScanned = !_isScanned;
            UpdatePanelState();
            
            Debug.Log($"ScanUI: {_playerName} スキャン状態変更 → {(_isScanned ? "ON" : "OFF")}");
        }

        /// <summary>
        /// パネルの状態を更新
        /// </summary>
        private void UpdatePanelState()
        {
            // プレイヤー番号テキストの更新
            if (_profileDisplayUI != null)
            {
                _profileDisplayUI.SetManualInputData(_playerIconIndex, _playerName);
            }

            // ステータステキストの更新
            if (statusText != null)
            {
                statusText.text = _lineTexts[0].Replace("{_playerName}", _playerName);
            }

            // 状態オブジェクトの表示切り替え
            if (trumpBackImage != null)
            {
                trumpBackImage.gameObject.SetActive(_isScanned);
            }

            if (inactiveStateObject != null)
            {
                inactiveStateObject.SetActive(!_isScanned);
            }
        }



        #endregion
    }
}
