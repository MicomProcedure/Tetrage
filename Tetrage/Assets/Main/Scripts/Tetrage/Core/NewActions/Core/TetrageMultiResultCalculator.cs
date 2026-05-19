using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageMulti の純粋な応答・勝敗計算ロジック。
    /// </summary>
    public static class TetrageMultiResultCalculator
    {
        #region Result types

        /// <summary>TetrageMulti の勝敗判定結果。</summary>
        public readonly struct JudgeResult
        {
            public IReadOnlyList<PlayerId> Winners { get; }
            public bool IsSuccess { get; }

            public JudgeResult(IReadOnlyList<PlayerId> winners, bool isSuccess)
            {
                Winners    = winners ?? new List<PlayerId>();
                IsSuccess  = isSuccess;
            }
        }

        #endregion

        #region Judge

        /// <summary>
        /// WinCondition.md に基づき勝者と成功フラグを返す。
        /// submissions: 親・指名子の提出有無（Host がタイムアウト既定を適用済みであること）。
        /// </summary>
        public static JudgeResult Judge(
            PlayerId parentId,
            Suit parentSuit,
            IReadOnlyList<PlayerId> nominatedChildIds,
            IReadOnlyDictionary<PlayerId, bool> submissions,
            IReadOnlyList<(PlayerId id, Suit suit)> allPlayers)
        {
            var participants = BuildParticipants(parentId, nominatedChildIds);
            var submitters   = participants.Where(id => submissions.TryGetValue(id, out var s) && s).ToList();

            // 条件3: participants 全員不提出
            if (submitters.Count == 0)
            {
                var losers = CollectTeammates(parentId, parentSuit, allPlayers);
                return new JudgeResult(BuildWinnersExcluding(allPlayers, losers), isSuccess: false);
            }

            var allSubmitted = participants.All(id => submissions.TryGetValue(id, out var s) && s);
            var suitsAligned = allSubmitted && participants.All(id =>
            {
                var suit = FindSuit(id, allPlayers);
                return suit == parentSuit;
            });
            var noNominationMiss = !HasNominationMiss(parentId, parentSuit, nominatedChildIds, allPlayers);

            // 条件1: 全員提出 + スート一致 + 指名漏れなし
            if (allSubmitted && suitsAligned && noNominationMiss)
                return new JudgeResult(participants.ToList(), isSuccess: true);

            // 条件2: 提出者と同スートチームが敗北
            var loserSet = new HashSet<PlayerId>();
            foreach (var submitterId in submitters)
            {
                var submitterSuit = FindSuit(submitterId, allPlayers);
                foreach (var (id, suit) in allPlayers)
                {
                    if (id != submitterId && suit == submitterSuit)
                        loserSet.Add(id);
                }
                loserSet.Add(submitterId);
            }

            return new JudgeResult(BuildWinnersExcluding(allPlayers, loserSet), isSuccess: false);
        }

        #endregion

        #region Submission defaults

        /// <summary>
        /// 未応答の既定を適用する。親=提出、子=提出しない。
        /// </summary>
        public static Dictionary<PlayerId, bool> ApplySubmissionDefaults(
            PlayerId parentId,
            IReadOnlyList<PlayerId> nominatedChildIds,
            IReadOnlyDictionary<PlayerId, bool> responses)
        {
            var result = new Dictionary<PlayerId, bool>(responses);
            if (!result.ContainsKey(parentId))
                result[parentId] = true;

            foreach (var childId in nominatedChildIds)
            {
                if (!result.ContainsKey(childId))
                    result[childId] = false;
            }

            return result;
        }

        #endregion

        #region Private helpers

        private static List<PlayerId> BuildParticipants(PlayerId parentId, IReadOnlyList<PlayerId> nominatedChildIds)
        {
            var list = new List<PlayerId> { parentId };
            if (nominatedChildIds != null)
                list.AddRange(nominatedChildIds);
            return list.Distinct().ToList();
        }

        private static bool HasNominationMiss(
            PlayerId parentId,
            Suit parentSuit,
            IReadOnlyList<PlayerId> nominatedChildIds,
            IReadOnlyList<(PlayerId id, Suit suit)> allPlayers)
        {
            var nominated = new HashSet<PlayerId>(nominatedChildIds ?? new List<PlayerId>());
            foreach (var (id, suit) in allPlayers)
            {
                if (id == parentId) continue;
                if (suit == parentSuit && !nominated.Contains(id))
                    return true;
            }
            return false;
        }

        private static HashSet<PlayerId> CollectTeammates(
            PlayerId parentId,
            Suit parentSuit,
            IReadOnlyList<(PlayerId id, Suit suit)> allPlayers)
        {
            var set = new HashSet<PlayerId> { parentId };
            foreach (var (id, suit) in allPlayers)
            {
                if (suit == parentSuit)
                    set.Add(id);
            }
            return set;
        }

        private static List<PlayerId> BuildWinnersExcluding(
            IReadOnlyList<(PlayerId id, Suit suit)> allPlayers,
            HashSet<PlayerId> losers)
        {
            return allPlayers
                .Where(p => !losers.Contains(p.id))
                .Select(p => p.id)
                .Distinct()
                .ToList();
        }

        private static Suit FindSuit(PlayerId id, IReadOnlyList<(PlayerId id, Suit suit)> allPlayers)
        {
            foreach (var (playerId, suit) in allPlayers)
            {
                if (playerId == id)
                    return suit;
            }
            return default;
        }

        #endregion
    }
}
