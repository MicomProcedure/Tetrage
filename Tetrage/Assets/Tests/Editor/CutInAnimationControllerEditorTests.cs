using System.Reflection;
using NUnit.Framework;
using Tetrage.Core.Constants;

namespace Tetrage.Tests.Editor
{
    public class CutInAnimationControllerEditorTests
    {
        #region Tests

        [TestCase("_tetrageSoloCutIn")]
        [TestCase("_tetrageMultiCutIn")]
        [TestCase("_tetrageReachCutIn")]
        [TestCase("_gameStartAnimation")]
        public void CutInAnimationController_HasSerializedAnimationTargets(string fieldName)
        {
            var field = typeof(CutInAnimationController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field, $"{fieldName} field should exist.");
        }

        [TestCase("PlayTetrageSoloCutIn")]
        [TestCase("PlayTetrageMultiCutIn")]
        [TestCase("PlayTetrageReachCutIn")]
        [TestCase("PlayGameStartAnimation")]
        public void CutInAnimationController_HasAnimationTriggerMethods(string methodName)
        {
            var method = typeof(CutInAnimationController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(method, $"{methodName} method should exist.");
        }

        [Test]
        public void CutInAnimationDuration_TotalDurationsAreLongerThanTransitions()
        {
            var transitionDurationMs =
                (InGameConsts.CutInAnimationDuration.ENTRY_DURATION_SECONDS
                    + InGameConsts.CutInAnimationDuration.EXIT_DURATION_SECONDS)
                * InGameConsts.CutInAnimationDuration.MILLISECONDS_PER_SECOND;

            Assert.GreaterOrEqual(InGameConsts.CutInAnimationDuration.TETRAGE_SOLO_TOTAL_DURATION_MS, transitionDurationMs);
            Assert.GreaterOrEqual(InGameConsts.CutInAnimationDuration.TETRAGE_MULTI_TOTAL_DURATION_MS, transitionDurationMs);
            Assert.GreaterOrEqual(InGameConsts.CutInAnimationDuration.TETRAGE_REACH_TOTAL_DURATION_MS, transitionDurationMs);
        }

        #endregion
    }
}
