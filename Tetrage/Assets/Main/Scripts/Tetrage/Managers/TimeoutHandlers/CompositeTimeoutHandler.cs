using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;

namespace Tetrage.Managers.TimeoutHandlers
{
    /// <summary>
    /// 複数のタイムアウトハンドラーを順番に実行するCompositeハンドラー
    /// </summary>
    public class CompositeTimeoutHandler : ITimeoutHandler
    {
        private readonly List<ITimeoutHandler> _handlers;
        private readonly bool _stopOnFirstSuccess;

        public CompositeTimeoutHandler(bool stopOnFirstSuccess = false)
        {
            _handlers = new List<ITimeoutHandler>();
            _stopOnFirstSuccess = stopOnFirstSuccess;
        }

        /// <summary>
        /// ハンドラーを追加
        /// </summary>
        public CompositeTimeoutHandler AddHandler(ITimeoutHandler handler)
        {
            if (handler != null)
            {
                _handlers.Add(handler);
            }
            return this;
        }

        /// <summary>
        /// 複数のハンドラーを一度に追加
        /// </summary>
        public CompositeTimeoutHandler AddHandlers(params ITimeoutHandler[] handlers)
        {
            foreach (var handler in handlers.Where(h => h != null))
            {
                _handlers.Add(handler);
            }
            return this;
        }

        public bool CanHandle(TimeoutContext context)
        {
            // いずれかのハンドラーが処理可能であれば処理可能
            return _handlers.Any(h => h.CanHandle(context));
        }

        public async UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context)
        {
            var results = new List<TimeoutHandleResult>();
            var messages = new List<string>();

            Debug.Log($"CompositeTimeoutHandler: {_handlers.Count}個のハンドラーでタイムアウト処理開始");

            foreach (var handler in _handlers)
            {
                if (!handler.CanHandle(context))
                {
                    Debug.Log($"CompositeTimeoutHandler: {handler.GetType().Name} はこのコンテキストを処理できません");
                    continue;
                }

                try
                {
                    Debug.Log($"CompositeTimeoutHandler: {handler.GetType().Name} でタイムアウト処理実行中...");

                    var result = await handler.HandleTimeoutAsync(context);
                    results.Add(result);

                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        messages.Add(result.Message);
                    }

                    // ゲーム終了が指示された場合は即座に終了
                    if (!result.ShouldContinueGame)
                    {
                        Debug.Log($"CompositeTimeoutHandler: {handler.GetType().Name} がゲーム終了を指示");
                        return result;
                    }

                    // 最初の成功で停止するオプション
                    if (_stopOnFirstSuccess)
                    {
                        Debug.Log($"CompositeTimeoutHandler: {handler.GetType().Name} で処理完了（stopOnFirstSuccess=true）");
                        break;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"CompositeTimeoutHandler: {handler.GetType().Name} でエラー発生 - {ex.Message}");
                    messages.Add($"{handler.GetType().Name}: エラー発生");
                }
            }

            // 総合的な結果を決定
            return CreateFinalResult(results, messages);
        }

        /// <summary>
        /// 全ハンドラーの結果から最終結果を作成
        /// </summary>
        private TimeoutHandleResult CreateFinalResult(List<TimeoutHandleResult> results, List<string> messages)
        {
            if (results.Count == 0)
            {
                return TimeoutHandleResult.ContinueGame(
                    advanceTurn: true,
                    message: "処理可能なタイムアウトハンドラーがありませんでした"
                );
            }

            // ゲーム継続の判定：すべてのハンドラーが継続を指示している場合のみ継続
            bool shouldContinueGame = results.All(r => r.ShouldContinueGame);

            // ターン進行の判定：いずれかのハンドラーが進行を指示していれば進行
            bool shouldAdvanceTurn = results.Any(r => r.ShouldAdvanceTurn);

            // メッセージの統合
            string finalMessage = messages.Count > 0 ? string.Join(" | ", messages) : null;

            Debug.Log($"CompositeTimeoutHandler: 処理完了 - 継続={shouldContinueGame}, ターン進行={shouldAdvanceTurn}");

            return new TimeoutHandleResult(shouldContinueGame, shouldAdvanceTurn, finalMessage);
        }
    }
}