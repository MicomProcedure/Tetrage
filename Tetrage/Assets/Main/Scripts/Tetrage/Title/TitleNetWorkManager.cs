using UnityEngine;
using Tetrage.Network;

namespace Tetrage.Title
{
    /// <summary>
    /// タイトルシーン管理の統合クラス
    /// 3つの機能クラスを統合して管理
    /// </summary>
    public class TitleSceneManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private EnterMasterServer enterMasterServer;
    [SerializeField] private CreateRoom createRoom;
    [SerializeField] private EnterRoom enterRoom;



    #region Public Interface Methods



    /// <summary>
    /// ルーム作成（CreateRoomに委譲）
    /// </summary>
    public void OnClickCreateRoom()
    {
        if (enterMasterServer != null)
        {
            enterMasterServer.ConnectToMaster();
        }

        if (createRoom != null)
        {
            createRoom.CreateNewRoom();
        }
    }

    public void OnClickEnterRoom()
    {
        if (enterMasterServer != null)
        {
            enterMasterServer.ConnectToMaster();
        }

        if (enterRoom != null)
        {
            enterRoom.JoinRoom();
        }
    }


    #endregion

  
    }
}