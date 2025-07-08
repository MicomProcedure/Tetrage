---
title: Dealerクラス 動作フロー解説書
created: 2025-07-03
modified: 2025-07-07
tags:
  - ai/cursor
  - prj/tetrage
  - 2025/07/07
---

## 📋 概要
Tetrage プロジェクトにおける `Dealer` クラス（`Managers/Dealer.cs`）の最新仕様を整理します。特に **プレイヤーアクション待機システム**、**タイムアウト処理**、**戦略パターン** への対応、そして **ゲームループ全体のエラーハンドリング** を中心に解説します。

---

## 1. 主要変更点（2025-07-07）
* `SetTimeoutHandler()` で動的にタイムアウト戦略を差し替え可能になりました。
* `FirstDeal()` を新設し、初期化処理を分離。
* `OperationCanceledException` を上位ループで握りつぶし、ユーザー操作によるゲーム中断を「正常フロー」として扱います。
* `CancelCurrentPlayerAction()` で待機中のアクションを明示的にキャンセル。
* ログレベルの見直し（致命的エラー ↔ 通常キャンセル）。

---

## 2. クラス構成概要
| フィールド / プロパティ        | 説明 |
| ------------------------------ | ---- |
| `Stage _stage` / `Stage`       | 現在のゲームボード。 |
| `List<IPlayer> _players` / `Players` | 参加プレイヤー一覧。 |
| `IPlayer _currentPlayer` / `CurrentPlayer` | 現在手番のプレイヤー。 |
| `IDealerStrategy _dealerStrategy` / `DealerStrategy` | ディーラー戦略（ターン順・デッキ操作）。 |
| `int _roundCount` / `RoundCount` | ラウンド番号。 |
| `ActionManager _actionManager` | アクション実行のハブ。 |
| `ActionAwaiter _actionAwaiter` | プレイヤーアクション待機の実装。 |
| `ITimeoutHandler _timeoutHandler` | タイムアウト時のフォールバック戦略。 |

---

## 3. 実行フロー
### 3.1 ゲーム開始から終了まで
```
StartGameAsync() (123行)
  └─ FirstDeal()               (145行) 初期配布
  └─ StartRoundLoopAsync()     (171行)
       ├─ StartSingleRoundAsync() (205行)
       │    ├─ WaitForPlayerActionAsync() (318行)
       │    └─ CheckWinCondition()         (280行)
       └─ ループ or 終了判定
  └─ EndGame()                 (228行)
```
※ 行番号は 2025-07-07 時点の目安。

### 3.2 ラウンド単位の詳細
1. `OnRoundStart()` でイベント発火 → UI 更新などに利用。
2. `_actionAwaiter.WaitForPlayerActionAsync()` でプレイヤー入力を待機。
3. アクション結果を `ActionResult.Log()` で統一ログ出力。
4. 勝敗判定後、`_dealerStrategy.GetNextPlayer()` で手番を更新。
5. `OnRoundEnd()` でラウンド終了を通知。

---

## 4. プレイヤーアクション待機システム
`ActionAwaiter` が **Producer-Consumer** パターンを実装しています。
* **Consumer**: `Dealer.WaitForPlayerActionAsync()`
* **Producer**: `ActionManager` がアクション完了時に `TrySetResult()`
* **共有リソース**: `UniTaskCompletionSource<ActionResult>`

### 待機の開始
```csharp
// Dealer.cs 318行付近
return await _actionAwaiter.WaitForPlayerActionAsync(_currentPlayer, timeoutSeconds);
```

### 待機の解除
```csharp
// ActionAwaiter.cs
_actionCompletionSource.TrySetResult(result);
```

---

## 5. タイムアウト処理
| モード | 判定条件 | 動作 |
| ------ | -------- | ---- |
| 無制限待機 | `timeoutSeconds <= 0` | プレイヤー完了のみ待機。 |
| タイムアウト付き | `timeoutSeconds > 0` | `AttachExternalCancellation` で競争。 |

`SetTimeoutHandler()` により、`AutoPass`、`AutoDraw` など戦略切替が可能です。

---

## 6. エラーハンドリング⁄キャンセル
* `OperationCanceledException` が **ユーザー中断 or タイムアウト** を示す場合は上位で捕捉して `Debug.Log` のみ。
* 想定外の例外は `Debug.LogError` → `_gameFinished = true` でゲームを強制終了。
* `CancelCurrentPlayerAction()` は `_actionAwaiter.CancelWaiting()` をラップ。UI ボタンなどから即時呼び出し可能。

---

## 7. 公開 API 一覧
| 区分 | メソッド | 概要 |
| ---- | -------- | ---- |
| ゲーム制御 | `StartGameAsync()` | ゲーム開始（非同期）。 |
| ゲーム制御 | `EndGame()` | ゲーム終了と後始末。 |
| ラウンド制御 | `StartRoundLoopAsync()` | 勝敗決定までループ。 |
| ラウンド制御 | `StartSingleRoundAsync()` | 単一ラウンドのみ実行。 |
| 待機制御 | `CancelCurrentPlayerAction()` | 現在待機をキャンセル。 |
| 設定 | `SetTimeoutHandler(ITimeoutHandler)` | タイムアウト戦略を変更。 |

---

## 8. 設計上の利点と注意点
* **柔軟性**: タイムアウトや戦略を DI で差し替え可能。
* **安全性**: UniTask の `CancellationToken` でリソースリーク防止。
* **拡張性**: 新アクション追加時は `ActionRegistry` 登録のみ。
* **注意**: `TimeoutSeconds` を 0 より大きく設定した場合、キャンセル理由の判定に `IsTimeoutResult()` ヘルパーを利用してください。

---

## 9. よくある質問（FAQ）
**Q. タイムアウト後にプレイヤーがボタンを押したら？**
A. `UniTaskCompletionSource` は一度完了した後は無視するため二重処理にはなりません。

**Q. 「ラウンドループ中に致命的エラー」のログが出る原因は？**
A. タイムアウトや手動キャンセルを `LogError` していた旧実装の名残です。最新実装では `Debug.Log` に変更済み。

---

## 10. 参考リンク
* `Managers/Dealer.cs`
* `Core/Actions/ActionAwaiter.cs`
* `Services/TimeoutService.cs` (AttachExternalCancellation のラッパー)

---

© 2025 Tetrage Project
  