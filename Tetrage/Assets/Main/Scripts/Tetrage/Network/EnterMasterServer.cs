using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Tetrage.UI;

namespace Tetrage.Network
{
    /// <summary>
    /// マスターサーバー接続管理クラス
    /// </summary>
    public class EnterMasterServer : MonoBehaviourPunCallbacks
    {
        private bool isConnecting = false;
        [SerializeField] private NetworkErrorUI networkErrorUI;
        
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
            Debug.Log("切断されました: " + cause);
            isConnecting = false;
            
            // 切断原因に応じたエラーメッセージを表示
            string errorMessage = GetDisconnectErrorMessage(cause);
            if (!string.IsNullOrEmpty(errorMessage) && networkErrorUI != null)
            {
                networkErrorUI.ShowErrorMessagePanel(errorMessage);
            }
        }


        public override void OnJoinedLobby()
        {
            Debug.Log("ロビー参加完了");
        }
        #endregion

        #region Error Message Handling
        /// <summary>
        /// 切断原因に応じたエラーメッセージを取得
        /// </summary>
        /// <param name="cause">切断原因</param>
        /// <returns>エラーメッセージ</returns>
        private string GetDisconnectErrorMessage(DisconnectCause cause)
        {
            switch (cause)
            {
                case DisconnectCause.DisconnectByServerLogic:
                    return "サーバーによって切断されました。";
                case DisconnectCause.DisconnectByServerReasonUnknown:
                    return "サーバーから不明な理由で切断されました。";
                case DisconnectCause.InvalidAuthentication:
                    return "認証に失敗しました。アプリケーション設定を確認してください。";
                case DisconnectCause.CustomAuthenticationFailed:
                    return "カスタム認証に失敗しました。";
                case DisconnectCause.AuthenticationTicketExpired:
                    return "認証チケットの有効期限が切れました。";
                case DisconnectCause.DisconnectByClientLogic:
                    return "クライアントによって切断されました。";
                case DisconnectCause.DisconnectByOperationLimit:
                    return "操作制限に達しました。";
                case DisconnectCause.Exception:
                    return "ネットワークエラーが発生しました。インターネット接続を確認してください。";
                case DisconnectCause.ExceptionOnConnect:
                    return "サーバーへの接続中にエラーが発生しました。インターネット接続を確認してください。";
                case DisconnectCause.ServerTimeout:
                    return "サーバーへの接続がタイムアウトしました。インターネット接続を確認してください。";
                case DisconnectCause.ClientTimeout:
                    return "接続がタイムアウトしました。インターネット接続を確認してください。";
                case DisconnectCause.MaxCcuReached:
                    return "サーバーの同時接続数が上限に達しています。しばらく待ってから再試行してください。";
                case DisconnectCause.InvalidRegion:
                    return "無効なリージョンが指定されました。";
                default:
                    return "不明な理由で切断されました。ネットワーク接続を確認してください。";
            }
        }

        #endregion
    }
}
