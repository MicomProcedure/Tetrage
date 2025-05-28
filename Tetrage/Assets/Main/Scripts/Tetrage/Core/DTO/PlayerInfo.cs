using Tetrage.Core.Enums;

namespace Tetrage.Core.DTO
{
    /// <summary>
    /// プレイヤーの情報を管理するクラス
    /// </summary>
    public struct PlayerInfo
    {
        public string UserId;   // プレイヤーのユーザーID
        public PlayerType PlayerType; // プレイヤーの種類
        
    }
}
