using System.Collections.Generic;
using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Tetrage.Factories;
using Tetrage.Managers;
using Tetrage.Managers.TimeoutHandlers;
using Tetrage.Core.Actions;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Enums;

namespace Tetrage.Tests
{
    /// <summary>
    /// タイムアウトハンドラーシステムの使用例
    /// </summary>
    public class TimeoutHandlerUsageExample : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private float timeoutSeconds = 5f;

        private Dealer _dealer;

        [ContextMenu("1. 基本: 自動パスのみ")]
        public void TestAutoPassOnly()
        {
            var handler = TimeoutHandlerFactory.CreateAutoPass();
            CreateDealerWithTimeout(handler);
            Debug.Log("自動パスハンドラーでDealer作成完了");
        }

        [ContextMenu("2. UI通知 + 自動パス")]
        public void TestUINotificationWithAutoPass()
        {
            var handler = TimeoutHandlerFactory.CreateNotificationWithAutoPass(3f);
            CreateDealerWithTimeout(handler);
            Debug.Log("UI通知+自動パスハンドラーでDealer作成完了");
        }

        [ContextMenu("3. カスタム複合ハンドラー")]
        public void TestCustomCompositeHandler()
        {
            var handler = TimeoutHandlerFactory.CreateComposite()
                .AddHandler(new UINotificationTimeoutHandler(2f))
                .AddHandler(new AutoPassTimeoutHandler())
                .AddHandler(new DebugTimeoutHandler());

            CreateDealerWithTimeout(handler);
            Debug.Log("カスタム複合ハンドラーでDealer作成完了");
        }

        [ContextMenu("4. ゲームモード別ハンドラー")]
        public void TestGameModeHandlers()
        {
            // カジュアルモード
            var casualHandler = TimeoutHandlerFactory.CreateForGameMode(GameMode.Casual);
            Debug.Log($"カジュアルモード: {casualHandler.GetType().Name}");

            // 競技モード
            var competitiveHandler = TimeoutHandlerFactory.CreateForGameMode(GameMode.Competitive);
            Debug.Log($"競技モード: {competitiveHandler.GetType().Name}");

            // デバッグモード
            var debugHandler = TimeoutHandlerFactory.CreateForGameMode(GameMode.Debug);
            CreateDealerWithTimeout(debugHandler);
            Debug.Log("デバッグモードハンドラーでDealer作成完了");
        }

        [ContextMenu("5. ランタイムでハンドラー変更")]
        public void TestRuntimeHandlerChange()
        {
            if (_dealer == null)
            {
                CreateDealerWithTimeout(TimeoutHandlerFactory.CreateAutoPass());
            }

            // 実行時にハンドラーを変更
            var newHandler = TimeoutHandlerFactory.CreateNotificationWithAutoPass(1f);
            _dealer.SetTimeoutHandler(newHandler);

            Debug.Log("ランタイムでタイムアウトハンドラーを変更しました");
        }

        [ContextMenu("6. タイムアウトテスト実行")]
        public void TestTimeoutExecution()
        {
            if (_dealer == null)
            {
                TestUINotificationWithAutoPass();
            }

            // タイムアウトを発生させるテスト
            _dealer.WaitForPlayerActionAsync(timeoutSeconds).Forget();
            Debug.Log($"{timeoutSeconds}秒後にタイムアウトが発生します");
        }

        /// <summary>
        /// 指定されたタイムアウトハンドラーでDealerを作成
        /// </summary>
        private void CreateDealerWithTimeout(ITimeoutHandler timeoutHandler)
        {
            // サンプルプレイヤー情報
            var participantInfoList = new List<PlayerInfo>
            {
                new PlayerInfo { UserId = "1", PlayerType = PlayerType.Local },
                new PlayerInfo { UserId = "2", PlayerType = PlayerType.Remote }
            };

            // タイムアウトハンドラー付きでDealer作成
            _dealer = new Dealer(participantInfoList, timeoutHandler);

            Debug.Log($"Dealer作成: タイムアウトハンドラー = {timeoutHandler.GetType().Name}");
        }

        /// <summary>
        /// カスタムタイムアウトハンドラーの作成例
        /// </summary>
        [ContextMenu("7. カスタムハンドラー例")]
        public void TestCustomHandler()
        {
            var customHandler = new CustomGameEndTimeoutHandler(maxTimeouts: 3);
            CreateDealerWithTimeout(customHandler);
            Debug.Log("カスタムゲーム終了ハンドラーでDealer作成完了");
        }
    }

    /// <summary>
    /// カスタムタイムアウトハンドラーの実装例
    /// 指定回数のタイムアウトでゲーム終了
    /// </summary>
    public class CustomGameEndTimeoutHandler : ITimeoutHandler
    {
        private readonly int _maxTimeouts;
        private int _timeoutCount = 0;

        public CustomGameEndTimeoutHandler(int maxTimeouts = 3)
        {
            _maxTimeouts = maxTimeouts;
        }

        public bool CanHandle(TimeoutContext context)
        {
            return true;
        }

        public async Cysharp.Threading.Tasks.UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context)
        {
            _timeoutCount++;

            Debug.Log($"カスタムハンドラー: タイムアウト {_timeoutCount}/{_maxTimeouts} - プレイヤー {context.Player.PlayerId}");

            if (_timeoutCount >= _maxTimeouts)
            {
                return TimeoutHandleResult.EndGame($"タイムアウト{_maxTimeouts}回によりゲーム終了");
            }

            // 自動パス実行
            await context.Player.PassAsync();

            return TimeoutHandleResult.ContinueGame(
                advanceTurn: true,
                message: $"自動パス実行 (残り{_maxTimeouts - _timeoutCount}回のタイムアウトでゲーム終了)"
            );
        }
    }
}