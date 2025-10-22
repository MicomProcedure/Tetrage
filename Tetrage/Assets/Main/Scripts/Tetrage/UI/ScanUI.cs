using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core.DTO;
using Tetrage.Core.Ids;

namespace Tetrage.UI
{
    /// <summary>
    /// スキャンに関連するパネルの状態を変更するUIコンポーネント
    /// ボタン押下でパネル内のGameObjectやTextを変更する
    /// </summary>
    public class ScanUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private TextMeshProUGUI playerNumberText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerNameText;

        [Header("Button")]
        [SerializeField] private Button NextButton;

        [Header("State Objects")]
        [SerializeField] private Image trumpBackImage;
        [SerializeField] private GameObject inactiveStateObject;

        #endregion

        #region Private Fields

        private PlayerInfo _playerInfo;
        private PlayerId _playerId;
        private int _playerNumber;
        private bool _isScanned = false;

        #endregion


        #region Public Methods

        /// <summary>
        /// ゲーム開始時にPlayerInfoを設定して初期化
        /// </summary>
        /// <param name="playerInfo">プレイヤー情報</param>
        public void Initialize(PlayerInfo playerInfo)
        {
            _playerInfo = playerInfo;
            _playerId = playerInfo.Id;
            
            // プレイヤー番号を取得（0始まりのIDを1始まりの表示用番号に変換）
            _playerNumber = _playerId.Value + 1;
            
            // 初期状態を設定
            UpdatePanelState();
            
            Debug.Log($"ScanUI: {_playerNumber}Pのプレイヤー情報を設定しました（UserId: {playerInfo.UserId}）");
        }

        /// <summary>
        /// パネルの状態をリセット
        /// </summary>
        public void ResetState()
        {
            _isScanned = false;
            UpdatePanelState();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// スキャンボタンが押された時の処理
        /// </summary>
        private void OnNextButtonClicked()
        {
            _isScanned = !_isScanned;
            UpdatePanelState();
            
            Debug.Log($"ScanUI: {_playerNumber}P スキャン状態変更 → {(_isScanned ? "ON" : "OFF")}");
        }

        /// <summary>
        /// パネルの状態を更新
        /// </summary>
        private void UpdatePanelState()
        {
            // プレイヤー番号テキストの更新
            if (playerNumberText != null)
            {
                playerNumberText.text = $"{_playerNumber}P";
            }

            // ステータステキストの更新
            if (statusText != null)
            {
                statusText.text = $"ターゲットカードの確認を行います。\n{_playerInfo.UserId}さんは{_playerNumber}Pです。\n準備ができたら、「NEXT」を押してください。";
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
