using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Ids;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// PlayerIdとActorNumberの双方向マッピングを管理する実装。
    /// </summary>
    public sealed class PlayerIdMapper : Tetrage.Network.Contracts.IPlayerIdMapper
    {
        private readonly Dictionary<int, PlayerId> _actorToPlayerId = new Dictionary<int, PlayerId>();
        private readonly Dictionary<PlayerId, int> _playerIdToActor = new Dictionary<PlayerId, int>();

        /// <summary>
        /// ActorNumberからPlayerIdを取得する
        /// </summary>
        public bool TryGetPlayerId(int actorNumber, out PlayerId playerId)
        {
            return _actorToPlayerId.TryGetValue(actorNumber, out playerId);
        }

        /// <summary>
        /// PlayerIdからActorNumberを取得する
        /// </summary>
        public bool TryGetActorNumber(PlayerId playerId, out int actorNumber)
        {
            return _playerIdToActor.TryGetValue(playerId, out actorNumber);
        }

        /// <summary>
        /// PlayerIdとActorNumberのマッピングを登録する
        /// </summary>
        public void Register(PlayerId playerId, int actorNumber)
        {
            _actorToPlayerId[actorNumber] = playerId;
            _playerIdToActor[playerId] = actorNumber;
        }

        /// <summary>
        /// 登録されている全PlayerIdを取得する
        /// </summary>
        public IReadOnlyList<PlayerId> GetAllPlayerIds()
        {
            return _playerIdToActor.Keys.ToList();
        }

        /// <summary>
        /// 登録されている全ActorNumberを取得する
        /// </summary>
        public int[] GetAllActorNumbers()
        {
            return _actorToPlayerId.Keys.ToArray();
        }
    }
}

