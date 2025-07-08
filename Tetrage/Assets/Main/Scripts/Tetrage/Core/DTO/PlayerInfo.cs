using System;
using Tetrage.Core.Enums;

namespace Tetrage.Core.DTO
{
    /// <summary>
    /// プレイヤーの情報を管理するクラス
    /// </summary>
    [Serializable]
    public struct PlayerInfo
    {
        public string UserId;   // プレイヤーのユーザーID
        public PlayerType PlayerType; // プレイヤーの種類
        
    }
}
