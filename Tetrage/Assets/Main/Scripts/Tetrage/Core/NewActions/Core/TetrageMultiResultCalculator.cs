using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageMulti の純粋な応答・勝敗計算ロジック。
    /// HostActionProcessor から分離し、テスト可能にする。
    /// </summary>
    public static class TetrageMultiResultCalculator
    {
        /// <summary>
        /// 応答状況から「出す」プレイヤーを決定する。
        /// - 明示的 ResponseOpen → 出す
        /// - 未応答（responses に含まれない）→ 出す（フォールバック）
        /// - 明示的 ResponseDecline → 出さない（除外）
        /// </summary>
        /// <param name="selectedPlayerIds">宣言者が選択したプレイヤー ID 一覧</param>
        /// <param name="responses">key=PlayerId, value=true(出す)/false(出さない)</param>
        /// <returns>出すプレイヤーの PlayerId 一覧</returns>
        public static List<PlayerId> DetermineOpenPlayers(
            IReadOnlyList<PlayerId> selectedPlayerIds,
            IReadOnlyDictionary<PlayerId, bool> responses)
        {
            var openPlayers = new List<PlayerId>();
            foreach (var id in selectedPlayerIds)
            {
                if (!responses.TryGetValue(id, out var isOpen) || isOpen)
                    openPlayers.Add(id); // 未応答 or 明示的に出す
            }
            return openPlayers;
        }

        /// <summary>
        /// 参加者のスート情報をもとに成功判定を行う。
        /// 宣言者と全 openPlayers のスートが一致すれば成功。
        /// </summary>
        /// <param name="requesterSuit">宣言者の Target スート</param>
        /// <param name="openPlayerSuits">出したプレイヤーの Target スート一覧</param>
        public static bool IsSuccess(Suit requesterSuit, IReadOnlyList<Suit> openPlayerSuits)
        {
            if (openPlayerSuits == null || openPlayerSuits.Count == 0) return false;
            return openPlayerSuits.All(s => s == requesterSuit);
        }

        /// <summary>
        /// 成功時の勝者 PlayerId 一覧を返す（宣言者 + 出したプレイヤー全員）。
        /// </summary>
        public static List<PlayerId> CalculateSuccessWinners(
            PlayerId requesterId, IReadOnlyList<PlayerId> openPlayerIds)
        {
            var winners = new List<PlayerId> { requesterId };
            winners.AddRange(openPlayerIds);
            return winners.Distinct().ToList();
        }

        /// <summary>
        /// 失敗時の勝者 PlayerId 一覧を返す。
        /// ① 出した参加者かつ宣言者とスート違い。
        /// ② 出さなかったプレイヤーかつ全 open 参加者のスートとも異なる。
        /// </summary>
        /// <param name="requesterId">宣言者の PlayerId</param>
        /// <param name="requesterSuit">宣言者の Target スート</param>
        /// <param name="allPlayers">全プレイヤー（PlayerId, Suit）</param>
        /// <param name="openPlayerIds">出したプレイヤーの PlayerId 集合</param>
        public static List<PlayerId> CalculateFailureWinners(
            PlayerId requesterId,
            Suit requesterSuit,
            IReadOnlyList<(PlayerId id, Suit suit)> allPlayers,
            IReadOnlyList<PlayerId> openPlayerIds)
        {
            var openSet   = new HashSet<PlayerId>(openPlayerIds) { requesterId };
            var openSuits = openPlayers_suits(requesterId, requesterSuit, allPlayers, openPlayerIds);
            var winners   = new List<PlayerId>();

            foreach (var (id, suit) in allPlayers)
            {
                if (id == requesterId) continue; // 宣言者は失敗時に勝者にならない

                if (openSet.Contains(id))
                {
                    // 出した: 宣言者とスートが違えば勝者
                    if (suit != requesterSuit)
                        winners.Add(id);
                }
                else
                {
                    // 出さなかった: 全 open スートとも違えば勝者
                    if (!openSuits.Contains(suit))
                        winners.Add(id);
                }
            }

            return winners.Distinct().ToList();
        }

        private static HashSet<Suit> openPlayers_suits(
            PlayerId requesterId, Suit requesterSuit,
            IReadOnlyList<(PlayerId id, Suit suit)> allPlayers,
            IReadOnlyList<PlayerId> openPlayerIds)
        {
            var openSet  = new HashSet<PlayerId>(openPlayerIds);
            var suitSet  = new HashSet<Suit> { requesterSuit };
            foreach (var (id, suit) in allPlayers)
            {
                if (openSet.Contains(id))
                    suitSet.Add(suit);
            }
            return suitSet;
        }
    }
}
