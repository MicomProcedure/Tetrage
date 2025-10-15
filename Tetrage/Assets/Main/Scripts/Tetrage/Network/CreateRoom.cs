using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Tetrage.Network
{
        /// <summary>
        /// ルーム作成管理クラス
        /// </summary>
        public class CreateRoom : MonoBehaviourPunCallbacks
    {
        private string pendingRoomCode;
        private RoomOptions pendingRoomOptions;
    

        #region Public Methods
        /// <summary>
        /// ルーム作成ボタンクリック処理
        /// </summary>
        public void CreateNewRoom()
        {
            string roomCode = Random.Range(10000, 99999).ToString(); // 5桁のルームコードを生成
            RoomOptions options = new RoomOptions { MaxPlayers = 6, IsVisible = true, IsOpen = true };

            if (PhotonNetwork.InRoom)
            {
                Debug.LogError("既に部屋に参加しています: " + PhotonNetwork.CurrentRoom.Name);
                return;
            }

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
            {
                PhotonNetwork.CreateRoom(roomCode, options);
                Debug.Log("部屋作成リクエスト: " + roomCode);
                return;
            }

            // 接続状態に応じて保留
            pendingRoomCode = roomCode;
            pendingRoomOptions = options;

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
                Debug.Log("未接続のため接続開始。部屋作成を保留: " + roomCode);
            }
            else if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                Debug.Log("ロビー未参加のためJoinLobby。部屋作成を保留: " + roomCode);
            }
        }
        #endregion


        #region PhotonNetwork Callbacks
        public override void OnCreatedRoom()
        {
            Debug.Log("部屋作成成功: " + PhotonNetwork.CurrentRoom.Name);
            PlayerPrefs.SetString("RoomCode", PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError("部屋作成失敗: " + message);
        }

        public override void OnJoinedLobby()
        {
        
            // 保留中の部屋作成を実行
            if (!string.IsNullOrEmpty(pendingRoomCode) && pendingRoomOptions != null)
            {
                Debug.Log("保留中の部屋を作成: " + pendingRoomCode);
                PhotonNetwork.CreateRoom(pendingRoomCode, pendingRoomOptions);
                pendingRoomCode = null;
                pendingRoomOptions = null;
            }
        }
        #endregion
    }
}
