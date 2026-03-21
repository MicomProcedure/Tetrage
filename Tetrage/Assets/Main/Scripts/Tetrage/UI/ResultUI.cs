using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using Tetrage.Managers;

namespace Tetrage.UI
{
    /// <summary>
    /// ゲーム結果画面を管理するUIコンポーネント
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Containers")]
        [SerializeField] private Transform _winnerContainer;
        [SerializeField] private Transform _loserContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject _playerUIPrefab;

        [Header("Optional")]
        [SerializeField] private GameObject _resultPanel;

        #endregion

        #region Private Fields
        private readonly Dictionary<int, ResultPlayerUI> winnerItems = new Dictionary<int, ResultPlayerUI>();
        private readonly Dictionary<int, ResultPlayerUI> loserItems = new Dictionary<int, ResultPlayerUI>();
        #endregion

        #region Public Methods

        /// <summary>
        /// 結果を表示
        /// </summary>
        /// <param name="winnerActorNumbers">勝者の PlayerId.Value 一覧</param>
        /// <param name="allPlayers">全プレイヤーのリスト</param>
        public void DisplayResult(
            int[] winnerActorNumbers,
            IReadOnlyList<IPlayer> allPlayers)
        {
            if (winnerActorNumbers == null || allPlayers == null)
            {
                Debug.LogError("ResultUI: 引数がnullです");
                return;
            }

            ClearAllLists();

            foreach (var player in allPlayers)
            {
                if (winnerActorNumbers.Contains(player.Id.Value))
                {
                    AddWinnerItem(player);
                }
                else
                {
                    AddLoserItem(player);
                }

                // アイコンを設定
                {
                    if (winnerActorNumbers.Contains(player.Id.Value))
                    {
                        if (winnerItems.TryGetValue(player.Id.Value, out var ui)) ui.SetIcon(player.IconIndex);
                    }
                    else
                    {
                        if (loserItems.TryGetValue(player.Id.Value, out var ui)) ui.SetIcon(player.IconIndex);
                    }
                }
            }

            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
            }

            Debug.Log($"ResultUI: 勝者 {winnerItems.Count}人, 敗者 {loserItems.Count}人を表示");
        }

        /// <summary>
        /// 結果画面を非表示
        /// </summary>
        public void HideResult()
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }

            // 結果だけ閉じる場合にHUDを戻す（タイトル遷移でシーン破棄される場合は冗長だが無害）
            var inGameUi = FindFirstObjectByType<InGameUIManager>(FindObjectsInactive.Exclude);
            inGameUi?.SetGameplayHudVisible(true);
        }

        /// <summary>
        /// 全リストをクリア
        /// </summary>
        public void ClearAllLists()
        {
            ClearList(winnerItems, "Winner");
            ClearList(loserItems, "Loser");
        }

        #region Button Events
        /// <summary>
        /// タイトルシーンへ戻る（ボタン用）
        /// </summary>
        public void OnClickReturnToTitle()
        {
            var app = ApplicationManager.Instance;
            if (app == null)
            {
                app = FindFirstObjectByType<ApplicationManager>(FindObjectsInactive.Exclude);
            }
            if (app == null)
            {
                Debug.LogError("ResultUI: ApplicationManager が見つかりません");
                return;
            }
            app.GoToTitle();
        }
        #endregion

        #endregion

        #region Private Methods

        private void AddWinnerItem(IPlayer player)
        {
            AddPlayerItem(player, _winnerContainer, winnerItems, "Winner");
        }

        private void AddLoserItem(IPlayer player)
        {
            AddPlayerItem(player, _loserContainer, loserItems, "Loser");
        }

        private void AddPlayerItem(IPlayer player, Transform container, Dictionary<int, ResultPlayerUI> dict, string listName)
        {
            if (_playerUIPrefab == null || container == null)
            {
                Debug.LogError($"ResultUI: PlayerItemPrefab または {listName}Container が未設定");
                return;
            }

            if (dict.ContainsKey(player.Id.Value))
            {
                return;
            }

            GameObject itemObj = Instantiate(_playerUIPrefab, container);
            ResultPlayerUI playerUI = itemObj.GetComponent<ResultPlayerUI>();

            if (playerUI == null)
            {
                Debug.LogError("ResultUI: Prefabに ResultPlayerUI コンポーネントがありません");
                Destroy(itemObj);
                return;
            }

            playerUI.SetPlayerData(player);
            dict[player.Id.Value] = playerUI;
        }

        private void ClearList(Dictionary<int, ResultPlayerUI> dict, string listName)
        {
            foreach (var item in dict.Values)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            dict.Clear();
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ClearAllLists();
        }

        #endregion
    }
}
