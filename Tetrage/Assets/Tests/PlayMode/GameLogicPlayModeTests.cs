using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using R3;
using Tetrage.Network;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Ids;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Tests.PlayMode
{
    /// <summary>
    /// GameSceneのPlayModeテスト。
    /// VirtualTransportを使用してPhoton接続なしで高速にテスト実行する。
    /// </summary>
    public class GameLogicPlayModeTests
    {
        private PlayModeTestHarness _harness;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // GameSceneをロード
            yield return SceneManager.LoadSceneAsync("GameScene");
            
            // 1フレーム待機（シーン初期化完了を待つ）
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // クリーンアップ
            _harness?.Teardown();
            _harness = null;
            
            yield return null;
        }

        /// <summary>
        /// テスト1: GameSceneの初期化が正常に完了すること
        /// </summary>
        [UnityTest]
        public IEnumerator Test_GameScene_Initialize_Success()
        {
            // Arrange & Act
            _harness = new PlayModeTestHarness();
            yield return _harness.SetupGameScene(
                playerCount: 2,
                mode: NetworkMode.VirtualTransport
            ).ToCoroutine();

            // Assert
            Assert.IsNotNull(_harness.GameManager, "GameManagerが初期化されていません");
            Assert.IsNotNull(_harness.NetworkContext, "NetworkContextが初期化されていません");
            Assert.IsTrue(_harness.NetworkContext.IsHost, "ホストとして初期化されていません");
            
            Debug.Log("<color=green>Test_GameScene_Initialize_Success: PASSED</color>");
        }

        /// <summary>
        /// テスト2: EventBusへのアクセスが可能なこと
        /// </summary>
        [UnityTest]
        public IEnumerator Test_EventBus_Access()
        {
            // Arrange
            _harness = new PlayModeTestHarness();
            yield return _harness.SetupGameScene(2, NetworkMode.VirtualTransport).ToCoroutine();

            // Act
            var eventBus = _harness.GetEventBus();

            // Assert
            Assert.IsNotNull(eventBus, "EventBusが取得できません");
            
            Debug.Log("<color=green>Test_EventBus_Access: PASSED</color>");
        }

        /// <summary>
        /// テスト3: LogicInjectionでDomainEventを直接発行できること
        /// </summary>
        [UnityTest]
        public IEnumerator Test_LogicInjection_PublishDomainEvent()
        {
            // Arrange
            _harness = new PlayModeTestHarness();
            yield return _harness.SetupGameScene(2, NetworkMode.LogicInjection).ToCoroutine();

            var eventBus = _harness.GetEventBus();
            Assert.IsNotNull(eventBus, "EventBusが取得できません");

            // イベント受信フラグ
            bool turnStartedReceived = false;
            var disposable = new CompositeDisposable();

            // Act: イベントを購読
            eventBus.TurnStarted
                .Subscribe(e =>
                {
                    turnStartedReceived = true;
                    Assert.AreEqual(1, e.Sequence, "Sequenceが一致しません");
                    Assert.AreEqual(new PlayerId(1), e.CurrentPlayerId, "CurrentPlayerIdが一致しません");
                })
                .AddTo(disposable);

            // Act: DomainEventを直接発行
            var testEvent = new DomainEvents.TurnStartedEvent(
                sequence: 1,
                currentPlayerId: new PlayerId(1)
            );
            eventBus.Publish(testEvent);

            // 1フレーム待機（イベント処理の完了を待つ）
            yield return null;

            // Assert
            Assert.IsTrue(turnStartedReceived, "TurnStartedEventが受信されませんでした");
            
            // Cleanup
            disposable.Dispose();
            
            Debug.Log("<color=green>Test_LogicInjection_PublishDomainEvent: PASSED</color>");
        }

        /// <summary>
        /// テスト4: VirtualLogicFeederを使ってイベント注入できること
        /// </summary>
        [UnityTest]
        public IEnumerator Test_VirtualLogicFeeder_FeedEvent()
        {
            // Arrange
            _harness = new PlayModeTestHarness();
            yield return _harness.SetupGameScene(2, NetworkMode.LogicInjection).ToCoroutine();

            var eventBus = _harness.GetEventBus();
            var feeder = new VirtualLogicFeeder(eventBus);

            bool eventReceived = false;
            var disposable = new CompositeDisposable();

            // イベントを購読
            eventBus.TurnStarted
                .Subscribe(e =>
                {
                    eventReceived = true;
                    Debug.Log($"TurnStartedEvent受信: Seq={e.Sequence}, Player={e.CurrentPlayerId}");
                })
                .AddTo(disposable);

            // Act: VirtualLogicFeederでイベント注入
            feeder.Feed(new DomainEvents.TurnStartedEvent(
                sequence: 1,
                currentPlayerId: new PlayerId(2)
            ));

            yield return null;

            // Assert
            Assert.IsTrue(eventReceived, "イベントが受信されませんでした");
            
            // Cleanup
            disposable.Dispose();
            
            Debug.Log("<color=green>Test_VirtualLogicFeeder_FeedEvent: PASSED</color>");
        }

        /// <summary>
        /// テスト5: 乱数シード固定で再現性のあるテストが可能なこと
        /// </summary>
        [UnityTest]
        public IEnumerator Test_RandomSeed_Reproducibility()
        {
            // Arrange: 同じシードで2回初期化
            const int seed = 12345;

            // 1回目
            _harness = new PlayModeTestHarness();
            yield return _harness.SetupGameScene(2, NetworkMode.VirtualTransport, seed).ToCoroutine();
            var random1 = UnityEngine.Random.Range(0, 1000);
            _harness.Teardown();

            yield return null;

            // 2回目
            _harness = new PlayModeTestHarness();
            yield return _harness.SetupGameScene(2, NetworkMode.VirtualTransport, seed).ToCoroutine();
            var random2 = UnityEngine.Random.Range(0, 1000);

            // Assert: 同じ乱数値が得られること
            Assert.AreEqual(random1, random2, "乱数シードが固定されていません");
            
            Debug.Log($"<color=green>Test_RandomSeed_Reproducibility: PASSED (値: {random1})</color>");
        }
    }
}

