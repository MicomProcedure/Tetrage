using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace Tetrage.Title
{
    /// <summary>
    /// 待機部屋のUI管理クラス
    /// 部屋番号と参加プレイヤーの表示を担当
    /// </summary>
    public class WaitingRoomUI : MonoBehaviourPunCallbacks
    {
        #region UI References
        
        [Header("Room Info UI")]
        [SerializeField] private TextMeshProUGUI roomCodeText;          // 部屋番号表示
        
        [Header("Player List UI")]
        [SerializeField] private Transform playerListContainer;         // プレイヤーリストの親オブジェクト（Horizontal Layout Group推奨）
        [SerializeField] private GameObject playerItemPrefab;           // プレイヤー表示用のプレハブ（ProfileDisplayUIコンポーネント付き）
        
        [Header("Action Button UI")]
        [SerializeField] private Button actionButton;                   // Play/Readyボタン
        [SerializeField] private TextMeshProUGUI actionButtonText;      // ボタンのテキスト
        
        #endregion

        #region Private Fields
        
        private Dictionary<int, ProfileDisplayUI> playerItems = new Dictionary<int, ProfileDisplayUI>();
        private const string IS_READY_KEY = "isReady";  // カスタムプロパティのキー
        
        #endregion

        #region Unity Lifecycle
        
        void Start()
        {
            UpdateRoomCode();
            UpdatePlayerList();
            InitializeActionButton();
        }

        /// <summary>
        /// パネルがアクティブになった時にUIを更新
        /// 部屋に入り直した時にもUIが正しく表示されるようにする
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();
            
            // Start()が既に呼ばれている場合のみ更新（初回はStart()で処理）
            if (Time.frameCount > 0)
            {
                UpdateRoomCode();
                UpdatePlayerList();
                UpdateActionButton();
            }
        }
        
        #endregion

        #region Room Code Display
        
        /// <summary>
        /// 部屋番号を更新
        /// </summary>
        public void UpdateRoomCode()
        {
            if (roomCodeText == null)
            {
                Debug.LogWarning("RoomCodeTextが設定されていません");
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                roomCodeText.text = PhotonNetwork.CurrentRoom.Name;
            }
            else
            {
                roomCodeText.text = "-----";
                Debug.LogWarning("部屋に参加していません");
            }
        }
        
        #endregion

        #region Player List Management
        
        /// <summary>
        /// プレイヤーリストを更新
        /// </summary>
        public void UpdatePlayerList()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("部屋に参加していません");
                ClearPlayerList();
                return;
            }

            // 現在の部屋にいるプレイヤーリストを取得
            Player[] players = PhotonNetwork.PlayerList;
            
            // 既存のリストをクリア
            ClearPlayerList();
            
            // プレイヤーごとにUIアイテムを生成
            foreach (Player player in players)
            {
                AddPlayerItem(player);
            }
        }

        /// <summary>
        /// プレイヤーアイテムを追加
        /// </summary>
        private void AddPlayerItem(Player player)
        {
            if (playerItemPrefab == null || playerListContainer == null)
            {
                Debug.LogError("PlayerItemPrefab または PlayerListContainer が設定されていません");
                return;
            }

            // 既に存在する場合はスキップ
            if (playerItems.ContainsKey(player.ActorNumber))
            {
                return;
            }

            // プレハブからアイテムを生成
            GameObject itemObj = Instantiate(playerItemPrefab, playerListContainer);
            
            // ProfileDisplayUIコンポーネントを取得
            ProfileDisplayUI playerDisplayUI = itemObj.GetComponent<ProfileDisplayUI>();
            if (playerDisplayUI == null)
            {
                Debug.LogError("PlayerItemPrefabにProfileDisplayUIコンポーネントがありません");
                Destroy(itemObj);
                return;
            }
            
            // プレイヤー情報を設定
            playerDisplayUI.SetPlayerData(player);
            
            // 辞書に登録
            playerItems[player.ActorNumber] = playerDisplayUI;
        }

        /// <summary>
        /// プレイヤーアイテムを削除
        /// </summary>
        private void RemovePlayerItem(Player player)
        {
            if (playerItems.TryGetValue(player.ActorNumber, out ProfileDisplayUI item))
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
                playerItems.Remove(player.ActorNumber);
            }
        }

        /// <summary>
        /// プレイヤーリストをクリア
        /// </summary>
        private void ClearPlayerList()
        {
            foreach (var item in playerItems.Values)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            playerItems.Clear();
        }
        
        #endregion

        #region Photon Callbacks
        
        /// <summary>
        /// プレイヤーが部屋に参加したときのコールバック
        /// </summary>
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"プレイヤー参加: {newPlayer.NickName}");
            AddPlayerItem(newPlayer);
            // ホストの場合はボタン状態を更新
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateActionButton();
            }
        }

        /// <summary>
        /// プレイヤーが部屋から退出したときのコールバック
        /// </summary>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"プレイヤー退出: {otherPlayer.NickName}");
            RemovePlayerItem(otherPlayer);
            // ホストの場合はボタン状態を更新
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateActionButton();
            }
        }

        /// <summary>
        /// 部屋に参加したときのコールバック
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log("部屋に参加しました");
            UpdateRoomCode();
            UpdatePlayerList();
            UpdateActionButton();
        }

        /// <summary>
        /// 部屋から退出したときのコールバック
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("部屋から退出しました");
            ClearPlayerList();
            UpdateRoomCode();
        }

        /// <summary>
        /// プレイヤーのプロパティが更新されたときのコールバック
        /// </summary>
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // アイコンや名前が変更された場合に更新
            if (playerItems.TryGetValue(targetPlayer.ActorNumber, out ProfileDisplayUI item))
            {
                item.SetPlayerData(targetPlayer);
            }
            
            // isReadyプロパティが変更された場合はボタンの状態を更新
            if (changedProps.ContainsKey(IS_READY_KEY))
            {
                Debug.Log($"[OnPlayerPropertiesUpdate] プレイヤー {targetPlayer.NickName} のisReadyプロパティが変更されました");
                UpdateActionButton();
            }
        }
        
        #endregion

        #region Action Button Management

        /// <summary>
        /// アクションボタンを初期化
        /// </summary>
        private void InitializeActionButton()
        {
            if (actionButton == null)
            {
                Debug.LogWarning("ActionButtonが設定されていません");
                return;
            }

            // ボタンのテキストコンポーネントを取得（設定されていない場合）
            if (actionButtonText == null)
            {
                actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
                if (actionButtonText == null)
                {
                    Debug.LogError("ActionButtonにTextMeshProUGUIコンポーネントが見つかりません");
                    return;
                }
            }

            // ゲストの場合はisReadyを初期化（false）
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                Hashtable initialProps = new Hashtable() { { IS_READY_KEY, false } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
            }

            // ボタンのクリックイベントを設定
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);

            UpdateActionButton();
        }

        /// <summary>
        /// アクションボタンの表示と有効/無効状態を更新
        /// </summary>
        private void UpdateActionButton()
        {
            if (actionButton == null || actionButtonText == null)
            {
                return;
            }

            if (!PhotonNetwork.InRoom)
            {
                actionButton.interactable = false;
                actionButtonText.text = "-----";
                return;
            }

            // ホストの場合は「Play」ボタン
            if (PhotonNetwork.IsMasterClient)
            {
                actionButtonText.text = "Play";
                // 全ゲストが準備完了している場合のみ有効
                bool allReady = AreAllGuestsReady();
                actionButton.interactable = allReady;
                Debug.Log($"[UpdateActionButton] ホスト - Playボタンの有効状態: {allReady}");
            }
            // ゲストの場合は「Ready」ボタン
            else
            {
                // 現在の準備状態を取得
                bool isReady = GetLocalPlayerReadyState();
                actionButtonText.text = !isReady ? "Ready" : "Not Ready";
                actionButton.interactable = true;  // ゲストは常に切り替え可能
            }
        }

        /// <summary>
        /// アクションボタンがクリックされたときの処理
        /// </summary>
        private void OnActionButtonClicked()
        {
            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            // ホストの場合はゲームを開始
            if (PhotonNetwork.IsMasterClient)
            {
                if (AreAllGuestsReady())
                {
                    // TODO: ゲーム開始処理を実装
                    Debug.Log("ゲームを開始します");
                    // PhotonNetwork.LoadLevel("GameScene"); などの処理
                }
            }
            // ゲストの場合は準備状態を切り替え
            else
            {
                bool currentReadyState = GetLocalPlayerReadyState();
                bool newReadyState = !currentReadyState;

                Hashtable props = new Hashtable() { { IS_READY_KEY, newReadyState } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                Debug.Log($"準備状態を変更: {newReadyState}");
            }
        }

        /// <summary>
        /// ローカルプレイヤーの準備状態を取得
        /// </summary>
        private bool GetLocalPlayerReadyState()
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(IS_READY_KEY, out object isReady))
            {
                return (bool)isReady;
            }
            return false;
        }

        /// <summary>
        /// 全ゲストが準備完了しているかチェック
        /// ホストを除いた全員がisReadyがTrueになっていたらtrueを返す
        /// </summary>
        private bool AreAllGuestsReady()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.Log("[AreAllGuestsReady] 部屋に参加していません");
                return false;
            }

            Player[] players = PhotonNetwork.PlayerList;
            bool hasGuest = false;
            int readyGuestCount = 0;
            int totalGuestCount = 0;

            foreach (Player player in players)
            {
                // ホストは除外
                if (player.IsMasterClient)
                {
                    continue;
                }

                hasGuest = true;
                totalGuestCount++;

                // isReadyプロパティをチェック
                if (player.CustomProperties.TryGetValue(IS_READY_KEY, out object isReady))
                {
                    bool ready = (bool)isReady;
                    if (ready)
                    {
                        readyGuestCount++;
                    }
                    Debug.Log($"[AreAllGuestsReady] プレイヤー {player.NickName} (ActorNumber: {player.ActorNumber}): isReady = {ready}");
                }
                else
                {
                    // プロパティが設定されていない場合は準備未完了とみなす
                    Debug.Log($"[AreAllGuestsReady] プレイヤー {player.NickName} (ActorNumber: {player.ActorNumber}): isReadyプロパティが設定されていません");
                    return false;
                }
            }

            // ゲストが1人もいない場合は準備完了とみなす
            if (!hasGuest || totalGuestCount == 0)
            {
                Debug.Log("[AreAllGuestsReady] ゲストが存在しないため、準備完了とみなします");
                return true;
            }

            // 全ゲストが準備完了しているかチェック
            bool allReady = readyGuestCount == totalGuestCount;
            Debug.Log($"[AreAllGuestsReady] ゲスト数: {totalGuestCount}, 準備完了数: {readyGuestCount}, 結果: {allReady}");
            return allReady;
        }
        #endregion
    }
}
