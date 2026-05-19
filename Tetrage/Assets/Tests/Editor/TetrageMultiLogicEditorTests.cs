using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core.Actions;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// TetrageMulti の純粋ロジック（Judge / ApplySubmissionDefaults）を検証する。
    /// </summary>
    public class TetrageMultiLogicEditorTests
    {
        private static readonly PlayerId Parent  = new PlayerId(1);
        private static readonly PlayerId Child2  = new PlayerId(2);
        private static readonly PlayerId Child3  = new PlayerId(3);
        private static readonly PlayerId Child4  = new PlayerId(4);

        private static List<(PlayerId id, Suit suit)> AllFourPlayers => new()
        {
            (Parent,  Suit.Heart),
            (Child2,  Suit.Heart),
            (Child3,  Suit.Heart),
            (Child4,  Suit.Spade),
        };

        [Test]
        public void Judge_Condition1_AllSubmitSameSuitFullNomination_Success()
        {
            var nominated = new List<PlayerId> { Child2, Child3 };
            var submissions = new Dictionary<PlayerId, bool>
            {
                { Parent, true }, { Child2, true }, { Child3, true },
            };

            var result = TetrageMultiResultCalculator.Judge(
                Parent, Suit.Heart, nominated, submissions, AllFourPlayers);

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.Contains(result.Winners, Parent);
            CollectionAssert.Contains(result.Winners, Child2);
            CollectionAssert.Contains(result.Winners, Child3);
            CollectionAssert.DoesNotContain(result.Winners, Child4);
        }

        [Test]
        public void Judge_Condition1_NominationMiss_Fails()
        {
            // Child3 (Heart) を指名漏れ
            var nominated = new List<PlayerId> { Child2 };
            var submissions = new Dictionary<PlayerId, bool>
            {
                { Parent, true }, { Child2, true },
            };

            var result = TetrageMultiResultCalculator.Judge(
                Parent, Suit.Heart, nominated, submissions, AllFourPlayers);

            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Judge_Condition3_NoSubmissions_ParentTeamLoses()
        {
            var nominated = new List<PlayerId> { Child2, Child3 };
            var submissions = new Dictionary<PlayerId, bool>();

            var result = TetrageMultiResultCalculator.Judge(
                Parent, Suit.Heart, nominated, submissions, AllFourPlayers);

            Assert.IsFalse(result.IsSuccess);
            CollectionAssert.DoesNotContain(result.Winners, Parent);
            CollectionAssert.DoesNotContain(result.Winners, Child2);
            CollectionAssert.DoesNotContain(result.Winners, Child3);
            CollectionAssert.Contains(result.Winners, Child4);
        }

        [Test]
        public void Judge_Condition2_ParentOnlySubmit_SubmitterTeamLoses()
        {
            var nominated = new List<PlayerId> { Child2, Child3 };
            var submissions = new Dictionary<PlayerId, bool> { { Parent, true } };

            var result = TetrageMultiResultCalculator.Judge(
                Parent, Suit.Heart, nominated, submissions, AllFourPlayers);

            Assert.IsFalse(result.IsSuccess);
            CollectionAssert.DoesNotContain(result.Winners, Parent);
            CollectionAssert.DoesNotContain(result.Winners, Child2);
            CollectionAssert.DoesNotContain(result.Winners, Child3);
            CollectionAssert.Contains(result.Winners, Child4);
        }

        [Test]
        public void ApplySubmissionDefaults_ParentOpen_ChildClosed()
        {
            var nominated = new List<PlayerId> { Child2, Child3 };
            var responses = new Dictionary<PlayerId, bool> { { Child2, true } };

            var result = TetrageMultiResultCalculator.ApplySubmissionDefaults(
                Parent, nominated, responses);

            Assert.IsTrue(result[Parent]);
            Assert.IsTrue(result[Child2]);
            Assert.IsFalse(result[Child3]);
        }

        [Test]
        public void ApplySubmissionDefaults_TimeoutChild_DefaultsToDecline()
        {
            var nominated = new List<PlayerId> { Child2 };
            var result = TetrageMultiResultCalculator.ApplySubmissionDefaults(
                Parent, nominated, new Dictionary<PlayerId, bool>());

            Assert.IsTrue(result[Parent]);
            Assert.IsFalse(result[Child2]);
        }
    }
}
