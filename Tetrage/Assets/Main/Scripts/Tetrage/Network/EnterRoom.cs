using Photon.Pun;
using UnityEngine;
using Tetrage.Title;

/// <summary>
/// ルーム参加管理クラス
/// </summary>
namespace Tetrage.Network
{
    public class EnterRoom : MonoBehaviourPunCallbacks
    {
        [SerializeField] private NumberInputController numberInputController;

        private string pendingJoinRoomCode;

        #region Public Methods

        /// <summary>
        /// ルーム参加確認
        /// </summary>
        public void JoinRoom()
        {

            string code = numberInputController.DisplayText.text;
            if (!ValidateRoomCode(code))
            {
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.LogError("既に部屋に参加しています: " + PhotonNetwork.CurrentRoom.Name);
                return;
            }

            JoinRoom(code);
        }
        #endregion

        #region Room Validation
        /// <summary>
        /// ルームコードを検証
        /// </summary>
        /// <param name="code">検証するルームコード</param>
        /// <returns>有効かどうか</returns>
        private bool ValidateRoomCode(string code)
        {
            if (code.Length != 5 || !int.TryParse(code, out _))
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Room Joining
        /// <summary>
        /// ルームに参加
        /// </summary>
        /// <param name="roomCode">参加するルームコード</param>
        private void JoinRoom(string roomCode)
        {
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinRoom(roomCode);
                Debug.Log("部屋参加リクエスト: " + roomCode);
                return;
            }

            // 接続状態に応じて保留
            pendingJoinRoomCode = roomCode;
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
                Debug.Log("未接続のため接続開始。部屋参加を保留: " + roomCode);
            }
            else if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                Debug.Log("ロビー未参加のためJoinLobby。部屋参加を保留: " + roomCode);
            }
        }
        #endregion

        #region PhotonNetwork Callbacks
        public override void OnJoinedRoom()
        {
            Debug.Log("部屋参加成功: " + PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinedLobby()
        {
        
            // 保留中の部屋参加を実行
            if (!string.IsNullOrEmpty(pendingJoinRoomCode))
            {
                Debug.Log("保留中の部屋に参加: " + pendingJoinRoomCode);
                PhotonNetwork.JoinRoom(pendingJoinRoomCode);
                pendingJoinRoomCode = null;
            }
        }
        #endregion

    }
}