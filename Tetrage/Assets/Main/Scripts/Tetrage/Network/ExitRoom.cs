using Photon.Pun;
using UnityEngine;

public class ExitRoom : MonoBehaviourPunCallbacks
{
    public void OnExitRoom()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LeaveLobby();
        PhotonNetwork.Disconnect();
        Debug.Log("部屋から退出しました");
    }
}
