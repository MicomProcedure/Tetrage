using Photon.Pun;
using UnityEngine;
using Tetrage.Title;

namespace Tetrage.Network
{
    public class ExitRoom : MonoBehaviourPunCallbacks
    {
        [SerializeField] public NetworkErrorUI networkErrorUI;
        
        /// <summary>
        /// 部屋退出ボタン押下時の処理
        /// </summary>
        public void OnExitRoom()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[ExitRoom] 部屋に参加していないため退出できません");
                if (networkErrorUI != null)
                {
                    networkErrorUI.ShowErrorMessagePanel("部屋に参加していません");
                }
                return;
            }

            Debug.Log("[ExitRoom] 部屋退出リクエストを送信します");
            PhotonNetwork.LeaveRoom();
        }

        /// <summary>
        /// 部屋から退出した際のコールバック
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("[ExitRoom] 部屋から退出しました");
            
            if (networkErrorUI != null)
            {
                networkErrorUI.ShowErrorMessagePanel("部屋から退出しました");
            }
            else
            {
                Debug.LogError("[ExitRoom] NetworkErrorUI の参照が未設定です", this);
            }
        }
    }
}

