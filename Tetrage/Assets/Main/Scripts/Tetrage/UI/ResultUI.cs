using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;

namespace Tetrage.UI
{
    /// <summary>
    /// ゲーム結果画面を管理するUIコンポーネント
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Containers")]
        [SerializeField] private Transform winnerContainer;
        [SerializeField] private Transform loserContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject playerItemPrefab;

        [Header("Optional")]
        [SerializeField] private GameObject resultPanel;

        #endregion

        #region Private Fields

        private Dictionary<string, ResultPlayerUI> winnerItems = new Dictionary<string, ResultPlayerUI>();
        private Dictionary<string, ResultPlayerUI> loserItems = new Dictionary<string, ResultPlayerUI>();

        #endregion

        #region Public Methods

        /// <summary>
        /// 結果を表示
        /// </summary>
        /// <param name="winnerUserIds">勝者のUserIdリスト</param>
        /// <param name="allPlayers">全プレイヤーのリスト</param>
        /// <param name="playerIconMap">UserId -> IconIndex のマッピング</param>
        public void DisplayResult(
            string[] winnerUserIds, 
            IReadOnlyList<IPlayer> allPlayers,
            Dictionary<string, int> playerIconMap)
        {
            if (winnerUserIds == null || allPlayers == null)
            {
                Debug.LogError("ResultUI: 引数がnullです");
                return;
            }

            ClearAllLists();

            foreach (var player in allPlayers)
            {
                if (winnerUserIds.Contains(player.UserId))
                {
                    AddWinnerItem(player);
                }
                else
                {
                    AddLoserItem(player);
                }
                
                // アイコンを設定
                if (playerIconMap != null && playerIconMap.TryGetValue(player.UserId, out int iconIndex))
                {
                    var playerUI = winnerUserIds.Contains(player.UserId) 
                        ? winnerItems.GetValueOrDefault(player.UserId)
                        : loserItems.GetValueOrDefault(player.UserId);
                    
                    if (playerUI != null)
                    {
                        playerUI.SetIcon(iconIndex);
                    }
                }
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            Debug.Log($"ResultUI: 勝者 {winnerItems.Count}人, 敗者 {loserItems.Count}人を表示");
        }

        /// <summary>
        /// 結果画面を非表示
        /// </summary>
        public void HideResult()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 全リストをクリア
        /// </summary>
        public void ClearAllLists()
        {
            ClearList(winnerItems, "Winner");
            ClearList(loserItems, "Loser");
        }

        #endregion

        #region Private Methods

        private void AddWinnerItem(IPlayer player)
        {
            AddPlayerItem(player, winnerContainer, winnerItems, "Winner");
        }

        private void AddLoserItem(IPlayer player)
        {
            AddPlayerItem(player, loserContainer, loserItems, "Loser");
        }

        private void AddPlayerItem(IPlayer player, Transform container, Dictionary<string, ResultPlayerUI> dict, string listName)
        {
            if (playerItemPrefab == null || container == null)
            {
                Debug.LogError($"ResultUI: PlayerItemPrefab または {listName}Container が未設定");
                return;
            }

            if (dict.ContainsKey(player.UserId))
            {
                return;
            }

            GameObject itemObj = Instantiate(playerItemPrefab, container);
            ResultPlayerUI playerUI = itemObj.GetComponent<ResultPlayerUI>();

            if (playerUI == null)
            {
                Debug.LogError("ResultUI: Prefabに ResultPlayerUI コンポーネントがありません");
                Destroy(itemObj);
                return;
            }

            playerUI.SetPlayerData(player);
            dict[player.UserId] = playerUI;
        }

        private void ClearList(Dictionary<string, ResultPlayerUI> dict, string listName)
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