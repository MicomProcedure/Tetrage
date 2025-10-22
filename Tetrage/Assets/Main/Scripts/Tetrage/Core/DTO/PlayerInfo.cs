using System;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.DTO
{
    /// <summary>
    /// プレイヤーの情報を管理するクラス
    /// </summary>
    [Serializable]
    public struct PlayerInfo
    {
        public PlayerId Id;   // プレイヤーのID
        public string UserId;   // プレイヤーのユーザーID
        public PlayerType PlayerType; // プレイヤーの種類

        public int PlayerIconIndex; // プレイヤーのアイコンのインデックス
    }
}
