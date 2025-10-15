using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Tetrage.Title;

namespace Tetrage.Network
{
    /// <summary>
    /// マスターサーバー接続管理クラス
    /// </summary>
    public class EnterMasterServer : MonoBehaviourPunCallbacks
    {
       private bool isConnecting = false;
        
        #region Connection Management
        /// <summary>
        /// マスターサーバーへの接続を初期化
        /// </summary>
        private void InitializeConnection()
        {
            if (!PhotonNetwork.IsConnected && !isConnecting)
            {
                ConnectToMaster();
            }
        }

        /// <summary>
        /// マスターサーバーに接続
        /// </summary>
        public void ConnectToMaster()
        {
            if (isConnecting) return;

            isConnecting = true;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Photonへ接続開始");
        }
    
        #endregion



        #region PhotonNetwork Callbacks
        public override void OnConnectedToMaster()
        {
            Debug.Log("Masterに接続しました");
            isConnecting = false;
        
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                Debug.Log("ロビーに参加します");
            }
        }
        

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogError("切断されました: " + cause);
            isConnecting = false;
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("ロビー参加完了");
        }
        #endregion
    }
}
