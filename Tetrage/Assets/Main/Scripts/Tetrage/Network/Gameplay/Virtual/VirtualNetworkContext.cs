using System.Collections.Generic;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// VirtualTransport実装のNetworkContext。
    /// テスト・デバッグ用に固定値を返す。
    /// </summary>
    public sealed class VirtualNetworkContext : INetworkContext
    {
        private readonly bool _isHost;
        private readonly int _localActorNumber;
        private readonly bool _isReady;
        private readonly bool _isInRoom;
        private readonly int _playerCount;

        /// <summary>
        /// VirtualNetworkContextのコンストラクタ
        /// </summary>
        /// <param name="actorNumber">ActorNumber（通常は1,2,3...と順番に割り当て）</param>
        /// <param name="isHost">ホストかどうか</param>
        /// <param name="playerCount">プレイヤー数（デフォルト: 4）</param>
        /// <param name="isReady">接続状態（デフォルト: true）</param>
        /// <param name="isInRoom">ルーム参加状態（デフォルト: true）</param>
        public VirtualNetworkContext(int actorNumber, bool isHost, int playerCount = 4, bool isReady = true, bool isInRoom = true)
        {
            _localActorNumber = actorNumber;
            _isHost = isHost;
            _playerCount = playerCount;
            _isReady = isReady;
            _isInRoom = isInRoom;
        }

        /// <summary>ホスト判定</summary>
        public bool IsHost => _isHost;

        /// <summary>自ActorNumber</summary>
        public int LocalActorNumber => _localActorNumber;

        /// <summary>接続状態</summary>
        public bool IsReady => _isReady;

        /// <summary>ルーム参加状態</summary>
        public bool IsInRoom => _isInRoom;

        /// <summary>ルーム内のプレイヤー数</summary>
        public int PlayerCount => _playerCount;

        /// <summary>
        /// ルーム内のActorNumber一覧を取得
        /// 1から連番でActorNumberを生成する
        /// </summary>
        public IReadOnlyList<int> GetActorNumbers()
        {
            var actorNumbers = new List<int>(_playerCount);
            for (int i = 1; i <= _playerCount; i++)
            {
                actorNumbers.Add(i);    // ActorNumberは1から順番に割り当てる
            }
            return actorNumbers;
        }
    }
}

