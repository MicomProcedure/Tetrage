using Photon.Pun;
using UnityEngine;
using Tetrage.Title;

namespace Tetrage.Network
{
    public class ExitRoom : MonoBehaviourPunCallbacks
    {
        [SerializeField] public NetworkErrorUI networkErrorUI;
        public void OnExitRoom()
        {
            PhotonNetwork.LeaveRoom();
            Debug.Log("部屋から退出しました");
            networkErrorUI.ShowErrorMessagePanel("部屋から退出しました");
        }
    }
}

