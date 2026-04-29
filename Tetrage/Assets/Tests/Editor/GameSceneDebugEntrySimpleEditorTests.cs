using NUnit.Framework;
using Tetrage.Tests;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// GameScene直起動デバッグのMPPMプレイヤー解決を検証する。
    /// </summary>
    public class GameSceneDebugEntrySimpleEditorTests
    {
        #region Tests

        [TestCase("Player1", 0)]
        [TestCase("Player2", 1)]
        [TestCase("Player3", 2)]
        [TestCase("Player4", 3)]
        public void TryResolveMppmPlayerIndex_ParsesPlayerName(string playerName, int expectedIndex)
        {
            var resolved = GameSceneDebugEntrySimple.TryResolveMppmPlayerIndex(playerName, out var index);

            Assert.IsTrue(resolved);
            Assert.AreEqual(expectedIndex, index);
        }

        [Test]
        public void ResolveVirtualDebugPlayerIndex_UsesMppmPlayerBeforeInspectorUserPlayer()
        {
            var index = GameSceneDebugEntrySimple.ResolveVirtualDebugPlayerIndex(
                inspectorLocalPlayerIndex: 0,
                playerCount: 4,
                mppmPlayerName: "Player3");

            Assert.AreEqual(2, index);
        }

        #endregion
    }
}
