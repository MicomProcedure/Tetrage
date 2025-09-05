# Dealerクラス 動作フロー解説書

## 📋 概要

本ドキュメントは、Tetrageプロジェクトの`Dealer`クラスの動作フローについて、特に**プレイヤーアクション待機システム**と**タイムアウト処理**の仕組みを詳しく解説します。

## 🎯 対象読者

- Tetrageプロジェクトの開発メンバー
- 非同期処理（UniTask）に関する基本知識がある方
- Dealerクラスの実装や拡張を行う方

## 📚 目次

1. [全体的な実行フロー](#1-全体的な実行フロー)
2. [プレイヤーアクション待機システム](#2-プレイヤーアクション待機システム)
3. [タイムアウト処理の仕組み](#3-タイムアウト処理の仕組み)
4. [Producer-Consumerパターンの実装](#4-producer-consumerパターンの実装)
5. [具体的な動作例](#5-具体的な動作例)
6. [注意点と設計の利点](#6-注意点と設計の利点)

---

## 1. 全体的な実行フロー

### 1.1 ゲーム開始からラウンド実行まで

```
StartGame()
    ↓
ゲーム初期化（デッキシャッフル、プレイヤー設定等）
    ↓
StartRoundLoop()
    ↓
StartRound() ← ここからがメインループ
    ↓
WaitForPlayerActionAsync() ← プレイヤーアクション待機
    ↓
結果に応じた処理（成功/タイムアウト/エラー）
    ↓
NextRound() ← 次のプレイヤーへ
    ↓
StartRound() ← ループ継続
```

### 1.2 コード上の対応箇所

| フェーズ       | メソッド                     | 場所    |
| -------------- | ---------------------------- | ------- |
| ゲーム開始     | `StartGame()`                | 151行目 |
| ラウンド開始   | `StartRoundLoop()`           | 189行目 |
| メインループ   | `StartRound()`               | 198行目 |
| アクション待機 | `WaitForPlayerActionAsync()` | 333行目 |
| 次ターン移行   | `NextRound()`                | 232行目 |

---

## 2. プレイヤーアクション待機システム

### 2.1 基本的な仕組み

Dealerクラスでは、プレイヤーのアクション実行を**非同期で待機**する仕組みを採用しています。これは**Producer-Consumerパターン**による実装です。

#### 核心的なコンポーネント

```csharp
// プレイヤーアクション完了の「約束」を管理
private UniTaskCompletionSource<ActionResult> _actionCompletionSource;

// キャンセル処理用
private CancellationTokenSource _actionCancellationTokenSource;
```

### 2.2 待機開始（Consumer側）

**場所**: `WaitForPlayerActionAsync()` メソッド（333行目）

```csharp
// 1. 待機用のタスクを作成
_actionCompletionSource = new UniTaskCompletionSource<ActionResult>();
UniTask<ActionResult> waitTask = _actionCompletionSource.Task;

// 2. 実際の待機処理
if (timeoutSeconds > 0)
{
    return await WaitWithTimeoutAsync(waitTask, timeoutSeconds);  // タイムアウト付き
}
return await waitTask;  // 無制限待機
```

**重要**: この時点で`waitTask`は**未完了状態**で、プレイヤーのアクションを待っている状態になります。

### 2.3 待機解除（Producer側）

**場所**: `OnPlayerActionCompleted()` メソッド（465行目）

```csharp
private void OnPlayerActionCompleted(IAction action, IActionContext context, ActionResult result)
{
    if (_actionCompletionSource != null &&
        ReferenceEquals(context.RequesterPlayer, _currentPlayer))
    {
        // ここで待機を解除！
        _actionCompletionSource.TrySetResult(result);
    }
}
```

**実行タイミング**: プレイヤーがUIボタンをクリック → ActionManager → このメソッド → 待機解除

---

## 3. タイムアウト処理の仕組み

### 3.1 タイムアウトの2つのモード

Dealerクラスは、タイムアウト時間の指定により2つの異なる動作をします：

| モード               | 条件                 | 動作                                           |
| -------------------- | -------------------- | ---------------------------------------------- |
| **タイムアウト付き** | `timeoutSeconds > 0` | プレイヤーアクション vs タイムアウトの**競争** |
| **無制限待機**       | `timeoutSeconds ≤ 0` | プレイヤーアクションのみ待機                   |

### 3.2 タイムアウト付き待機の詳細

**場所**: `WaitWithTimeoutAsync()` メソッド（389行目）

```csharp
private async UniTask<ActionResult> WaitWithTimeoutAsync(UniTask<ActionResult> task, float timeoutSeconds)
{
    var timeoutCts = CreateTimeoutCancellationToken(timeoutSeconds);
    
    try
    {
        // ここが競争の核心！
        return await task.AttachExternalCancellation(timeoutCts.Token);
    }
    catch (OperationCanceledException) when (IsTimeoutCancellation(timeoutCts))
    {
        // タイムアウト発生時の処理
        return ActionResult.Failure("TIMEOUT", /* タイムアウト情報 */);
    }
}
```

### 3.3 競争メカニズムの詳細

`AttachExternalCancellation`により、以下の2つが**同時に監視**されます：

1. **プレイヤーアクション完了** (`task` = `_actionCompletionSource.Task`)
2. **タイムアウト発生** (`timeoutCts.Token`)

**先に完了した方**が結果を決定します。

#### 競争の結果パターン

| 勝者                 | 発生条件                           | 結果                              |
| -------------------- | ---------------------------------- | --------------------------------- |
| プレイヤーアクション | プレイヤーが時間内にアクション実行 | `ActionResult`（成功/失敗）       |
| タイムアウト         | 指定時間が経過                     | `ActionResult.Failure("TIMEOUT")` |

---

## 4. Producer-Consumerパターンの実装

### 4.1 パターンの概要

このシステムは、典型的な**Producer-Consumerパターン**で実装されています：

- **Consumer（消費者）**: `WaitForPlayerActionAsync()` - 結果を待つ
- **Producer（生産者）**: `OnPlayerActionCompleted()` - 結果を提供する
- **共有リソース**: `UniTaskCompletionSource<ActionResult>` - 結果の受け渡し

### 4.2 実行シーケンス

```
[Dealer Thread]
1. WaitForPlayerActionAsync() 呼び出し
2. UniTaskCompletionSource作成
3. await waitTask ← ここで待機状態

[UI Thread]
4. プレイヤーがボタンクリック
5. ActionManager.ExecuteAction()
6. OnActionCompleted イベント発火

[Dealer Thread]
7. OnPlayerActionCompleted() 実行
8. TrySetResult() ← 待機解除
9. WaitForPlayerActionAsync() 復帰
10. 結果処理へ続く
```

### 4.3 スレッドセーフティ

`UniTaskCompletionSource`と`TrySetResult()`により、マルチスレッド環境でも安全に動作します。

---

## 5. 具体的な動作例

### 5.1 ケース1: プレイヤーアクション成功（タイムアウトなし）

```csharp
// 呼び出し
var result = await WaitForPlayerActionAsync(0);  // 無制限待機

// 内部動作
UniTask<ActionResult> waitTask = _actionCompletionSource.Task;
return await waitTask;  // プレイヤーアクションのみ待機

// プレイヤーがアクション実行 → OnPlayerActionCompleted() → TrySetResult()
// → await解除 → ActionResult返却
```

### 5.2 ケース2: プレイヤーアクション成功（タイムアウト5秒）

```csharp
// 呼び出し
var result = await WaitForPlayerActionAsync(5.0f);

// 内部動作
return await WaitWithTimeoutAsync(waitTask, 5.0f);
  ↓
await task.AttachExternalCancellation(timeoutCts.Token);
// プレイヤーアクション vs 5秒タイムアウトの競争開始

// 3秒後にプレイヤーがアクション実行
// → プレイヤーアクション勝利 → 通常のActionResult返却
```

### 5.3 ケース3: タイムアウト発生

```csharp
// 呼び出し
var result = await WaitForPlayerActionAsync(5.0f);

// 内部動作（5秒後）
// タイムアウト勝利 → OperationCanceledException
// → IsTimeoutCancellation() == true
// → ActionResult.Failure("TIMEOUT") 返却

// 呼び出し元での処理
if (IsTimeoutResult(result))
{
    await HandleTimeoutAsync(result);  // タイムアウトハンドラーで処理
}
```

---

## 6. 注意点と設計の利点

### 6.1 よくある誤解

❌ **間違った理解**: 「タイムアウト処理が完了してからプレイヤーアクションを待機する」

✅ **正しい理解**: 「プレイヤーアクションとタイムアウトが同時に競争する」

### 6.2 コードの条件分岐について

```csharp
if (timeoutSeconds > 0)
{
    return await WaitWithTimeoutAsync(waitTask, timeoutSeconds);  // タイムアウト付き
}
return await waitTask;  // 無制限待機
```

この2つの`await`は**排他的実行**です（どちらか一方のみ実行）。同時実行ではありません。

### 6.3 設計の利点

#### ✅ 柔軟性
- 同じインターフェースでタイムアウトあり・なしを切り替え可能
- 呼び出し側はタイムアウトを意識する必要がない

#### ✅ 効率性
- 無制限待機時は余計なオーバーヘッドなし
- タイムアウト時も最小限のリソースで実現

#### ✅ 安全性
- CancellationTokenSourceの確実なリソース解放
- 重複するアクション完了の防止

#### ✅ 拡張性
- TimeoutHandlerによる多様なタイムアウト処理対応
- Producer-Consumerパターンによる疎結合設計

### 6.4 実装時の注意点

1. **リソース解放**: CancellationTokenSourceは必ずDispose()すること
2. **イベント購読**: ActionManagerのイベント購読を忘れずに設定
3. **プレイヤー判定**: OnPlayerActionCompleted()での現在プレイヤーチェック
4. **例外処理**: OperationCanceledException の適切な判定

---

## 📝 まとめ

Dealerクラスのプレイヤーアクション待機システムは、以下の技術により実現されています：

- **UniTaskCompletionSource**: 非同期結果の管理
- **Producer-Consumerパターン**: スレッドセーフな結果受け渡し
- **AttachExternalCancellation**: 複数待機の競争制御
- **Strategy Pattern**: 柔軟なタイムアウト処理

この設計により、**統一的なインターフェース**でありながら**効率的で柔軟な**プレイヤーアクション待機を実現しています。

---

## 🔗 関連コード箇所

| 機能             | ファイル  | 行数      |
| ---------------- | --------- | --------- |
| メイン待機ループ | Dealer.cs | 198-224行 |
| アクション待機   | Dealer.cs | 333-368行 |
| タイムアウト処理 | Dealer.cs | 389-415行 |
| 待機解除処理     | Dealer.cs | 465-474行 |
| リソース管理     | Dealer.cs | 515-526行 |

---

**作成日**: 2024年
**対象バージョン**: Tetrage v1.0  
**更新者**: Development Team 