using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Audio;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Managers;
using Tetrage.Core.Ids;
using Tetrage.Models;

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
        [SerializeField] private GameObject _playerProfileOnResultPrefab;

        [Header("Optional")]
        [SerializeField] private GameObject _resultPanel;

        #endregion

        #region Private Fields

        private readonly Dictionary<PlayerId, ResultPlayerEntry> _winnerItems = new Dictionary<PlayerId, ResultPlayerEntry>();
        private readonly Dictionary<PlayerId, ResultPlayerEntry> _loserItems = new Dictionary<PlayerId, ResultPlayerEntry>();

        #endregion

        #region Public Methods

        /// <summary>
        /// 結果を表示
        /// </summary>
        /// <param name="winnerPlayerIds">勝者の PlayerId 一覧</param>
        /// <param name="allPlayers">全プレイヤーのリスト</param>
        /// <param name="userPlayer">ローカルユーザー（勝敗ジングル判定用）</param>
        /// <param name="jingleAudio">勝敗ジングル再生 API（未設定時は再生しない）</param>
        public void DisplayResult(
            PlayerId[] winnerPlayerIds,
            IReadOnlyList<IPlayer> allPlayers,
            IPlayer userPlayer,
            IJingleAudioService jingleAudio)
        {
            if (winnerPlayerIds == null || allPlayers == null)
            {
                Debug.LogError("ResultUI: 引数がnullです");
                return;
            }

            ClearAllLists();

            foreach (var player in allPlayers)
            {
                if (winnerPlayerIds.Contains(player.Id))
                {
                    AddWinnerItem(player);
                }
                else
                {
                    AddLoserItem(player);
                }
            }

            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
            }

            // 同一 Canvas 上で Result が先頭子だと他 HUD より背面になるため、表示時は最前面へ
            transform.SetAsLastSibling();

            PlayResultJingle(winnerPlayerIds, userPlayer, jingleAudio);

            Debug.Log($"ResultUI: 勝者 {_winnerItems.Count}人, 敗者 {_loserItems.Count}人を表示");
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
            ClearList(_winnerItems, "Winner");
            ClearList(_loserItems, "Loser");
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

        private void PlayResultJingle(
            PlayerId[] winnerPlayerIds,
            IPlayer userPlayer,
            IJingleAudioService jingleAudio)
        {
            if (jingleAudio == null || userPlayer == null)
            {
                return;
            }

            // ローカルユーザーの勝敗に応じてジングルを再生する
            var isWinner = winnerPlayerIds.Contains(userPlayer.Id);
            jingleAudio.PlayJingle(isWinner ? JingleClipId.Win : JingleClipId.Lose);
        }

        private void AddWinnerItem(IPlayer player)
        {
            AddPlayerItem(player, _winnerContainer, _winnerItems, "Winner");
        }

        private void AddLoserItem(IPlayer player)
        {
            AddPlayerItem(player, _loserContainer, _loserItems, "Loser");
        }

        private void AddPlayerItem(
            IPlayer player,
            Transform container,
            Dictionary<PlayerId, ResultPlayerEntry> dict,
            string listName)
        {
            if (_playerProfileOnResultPrefab == null || container == null)
            {
                Debug.LogError($"ResultUI: PlayerItemPrefab または {listName}Container が未設定");
                return;
            }

            if (dict.ContainsKey(player.Id))
            {
                return;
            }

            var itemRoot = Instantiate(_playerProfileOnResultPrefab, container);
            var profileDisplayView = itemRoot.GetComponentInChildren<ProfileDisplayView>(true);
            var suitSpriteView = itemRoot.GetComponentInChildren<SuitSpriteView>(true);

            if (profileDisplayView == null || suitSpriteView == null)
            {
                Debug.LogError("ResultUI: Prefabに ProfileDisplayView または SuitSpriteView コンポーネントがありません");
                Destroy(itemRoot);
                return;
            }

            profileDisplayView.SetProfile(player.IconIndex, player.UserId);

            if (TryGetTargetSuit(player, out var targetSuit))
            {
                suitSpriteView.SetSuit(targetSuit);
            }
            else
            {
                Debug.LogWarning($"ResultUI: PlayerId={player.Id} の Target にカードがありません。スート表示をスキップします。");
            }

            dict[player.Id] = new ResultPlayerEntry(itemRoot, profileDisplayView, suitSpriteView);
        }

        private static bool TryGetTargetSuit(IPlayer player, out Suit suit)
        {
            suit = default;
            var target = player?.Target;
            if (target == null || target.Count == 0)
            {
                return false;
            }

            suit = target.Cards[0].Suit;
            return true;
        }

        private static void ClearList(Dictionary<PlayerId, ResultPlayerEntry> dict, string listName)
        {
            foreach (var entry in dict.Values)
            {
                if (entry.Root != null)
                {
                    Destroy(entry.Root);
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

        #region Nested Types

        private sealed class ResultPlayerEntry
        {
            public GameObject Root { get; }
            public ProfileDisplayView Profile { get; }
            public SuitSpriteView Suit { get; }

            public ResultPlayerEntry(GameObject root, ProfileDisplayView profile, SuitSpriteView suit)
            {
                Root = root;
                Profile = profile;
                Suit = suit;
            }
        }

        #endregion
    }
}
