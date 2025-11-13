using System.Collections.Generic;
using Tetrage.Core.Ids;

namespace Tetrage.Network.Contracts
{
    /// <summary>
    /// PlayerId（ドメイン内部ID）とActorNumber（ネットワークID）の双方向変換を管理するインターフェース。
    /// ドメイン層はPlayerIdのみで動作し、ActorNumberを一切知らない設計を実現する。
    /// </summary>
    public interface IPlayerIdMapper
    {
        /// <summary>
        /// ActorNumberからPlayerIdを取得する
        /// </summary>
        /// <param name="actorNumber">ネットワークID（ActorNumber）</param>
        /// <param name="playerId">取得したPlayerId</param>
        /// <returns>マッピングが存在する場合true</returns>
        bool TryGetPlayerId(int actorNumber, out PlayerId playerId);

        /// <summary>
        /// PlayerIdからActorNumberを取得する
        /// </summary>
        /// <param name="playerId">ドメイン内部ID（PlayerId）</param>
        /// <param name="actorNumber">取得したActorNumber</param>
        /// <returns>マッピングが存在する場合true</returns>
        bool TryGetActorNumber(PlayerId playerId, out int actorNumber);

        /// <summary>
        /// PlayerIdとActorNumberのマッピングを登録する
        /// </summary>
        /// <param name="playerId">ドメイン内部ID</param>
        /// <param name="actorNumber">ネットワークID</param>
        void Register(PlayerId playerId, int actorNumber);

        /// <summary>
        /// 登録されている全PlayerIdを取得する
        /// </summary>
        IReadOnlyList<PlayerId> GetAllPlayerIds();

        /// <summary>
        /// 登録されている全ActorNumberを取得する
        /// </summary>
        int[] GetAllActorNumbers();
    }
}

