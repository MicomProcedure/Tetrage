using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

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
        
        #endregion

        #region Private Fields
        
        private Dictionary<int, ProfileDisplayUI> playerItems = new Dictionary<int, ProfileDisplayUI>();
        
        #endregion

        #region Unity Lifecycle
        
        void Start()
        {
            UpdateRoomCode();
            UpdatePlayerList();
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
        }

        /// <summary>
        /// プレイヤーが部屋から退出したときのコールバック
        /// </summary>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"プレイヤー退出: {otherPlayer.NickName}");
            RemovePlayerItem(otherPlayer);
        }

        /// <summary>
        /// 部屋に参加したときのコールバック
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log("部屋に参加しました");
            UpdateRoomCode();
            UpdatePlayerList();
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
        }
        
        #endregion
    }
}
