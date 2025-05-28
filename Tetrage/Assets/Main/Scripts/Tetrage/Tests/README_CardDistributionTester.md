# CardDistributionTester 使用方法

## 概要
`CardDistributionTester`は、FieldSetupManagerTesterと連携してカード配布とプレイヤー間転送をテストするクラスです。Stackの52枚のカードを全プレイヤーに均等配布し、プレイヤー間でのカード移動をテストできます。

## 主な機能
- **均等配布**: Stackのカードを全プレイヤーの指定カードパイルに均等配布
- **プレイヤー間転送**: 指定プレイヤー間でカードを1枚転送
- **状況確認**: 各プレイヤーのカード保有状況を表示
- **カード回収**: 全カードをStackに戻す機能
- **Inspector操作**: Custom Editorによる直感的な操作

## セットアップ方法

### 1. コンポーネントの配置
```
GameObject
├── FieldSetupManagerTester (必須)
└── CardDistributionTester (新規追加)
```

### 2. 前提条件
- FieldSetupManagerTesterが適切に設定済み
- フィールドセットアップが完了済み（Stack に52枚のカードが存在）

### 3. 設定項目

#### カード配布設定
- **Cards Per Player**: 各プレイヤーに配布するカード枚数（デフォルト: 13枚）
- **Target Pile Index**: 配布先カードパイル
  - 0: Hands（手札）
  - 1: Tmp（一時保管）
  - 2: Target（ターゲット）

#### プレイヤー間転送設定
- **From Player Index**: 転送元プレイヤーのインデックス（0から開始）
- **To Player Index**: 転送先プレイヤーのインデックス（0から開始）
- **From Pile Type**: 転送元カードパイル（0: Hands, 1: Tmp, 2: Target）
- **To Pile Type**: 転送先カードパイル（0: Hands, 1: Tmp, 2: Target）

## 使用方法

### Inspector操作（推奨）

#### 1. カード配布テスト
1. **設定確認**: 「Cards Per Player」と「Target Pile Index」を設定
2. **配布実行**: 「全プレイヤーに均等配布」ボタンをクリック
3. **結果確認**: 「配布状況を表示」ボタンで配布結果を確認

#### 2. プレイヤー間転送テスト
1. **転送設定**: From/To Player Indexと各Pile Typeを設定
2. **設定確認**: 転送設定ボックスで現在の設定を確認
3. **転送実行**: 「プレイヤー間でカード転送」ボタンをクリック

#### 3. 便利機能
- **カード回収**: 「全カードをStackに戻す」ボタンで全カードを初期状態に戻す
- **クイック設定**: 「手札配布設定」「Tmp配布設定」「Target配布設定」で素早く配布先変更

### 右クリックメニュー操作
- **Stackから全プレイヤーに均等配布**: カード配布を実行
- **プレイヤー間でカード転送**: プレイヤー間転送を実行
- **配布状況を表示**: 現在の配布状況を表示
- **全カードをStackに戻す**: 全カードを回収

## 使用例

### 基本的なカード配布
```
1. FieldSetupManagerTesterでフィールドセットアップを実行
2. CardDistributionTesterで「Cards Per Player」を13に設定
3. 「Target Pile Index」を0（Hands）に設定
4. 「全プレイヤーに均等配布」ボタンをクリック
→ 各プレイヤーのHandsに13枚ずつカードが配布される
```

### プレイヤー間転送テスト
```
1. カード配布完了後
2. 「From Player Index」を0、「To Player Index」を1に設定
3. 「From Pile Type」と「To Pile Type」を0（Hands）に設定
4. 「プレイヤー間でカード転送」ボタンをクリック
→ プレイヤー1のHandsからプレイヤー2のHandsにカード1枚が移動
```

### 異なるカードパイル間転送
```
1. 「From Pile Type」を0（Hands）、「To Pile Type」を1（Tmp）に設定
2. 転送実行
→ プレイヤー1のHandsからプレイヤー2のTmpにカード移動
```

## 配布結果の表示

### ログ出力例
```
=== カード配布開始 ===
配布前 - Stack: 52枚
配布予定: 各プレイヤーに13枚 (総計26枚)
プレイヤー1 (LocalPlayer1): Handsに13枚配布
プレイヤー2 (RemotePlayer1): Handsに13枚配布
配布後 - Stack: 26枚
=== カード配布完了 ===

--- 配布結果詳細 ---
プレイヤー1 (LocalPlayer1) - Hands: 13枚
  - Spade 1
  - Heart 1
  - Diamond 1
  ... 他10枚
```

### Inspector表示
Custom Editorにより、以下の情報がリアルタイムで表示されます：
- プレイヤー数
- 各カードパイルの枚数
- プレイヤー別詳細情報
- 転送設定の可視化

## エラーハンドリング

### よくあるエラーと対処法

#### 「FieldSetupManagerがセットアップされていません」
**原因**: FieldSetupManagerTesterでフィールドセットアップが未実行
**対処**: 先にFieldSetupManagerTesterで「フィールドセットアップを実行」

#### 「プレイヤーインデックスが範囲外です」
**原因**: 設定したプレイヤーインデックスが存在しないプレイヤーを指している
**対処**: 実際のプレイヤー数に合わせてインデックスを調整

#### 「転送元にカードがありません」
**原因**: 指定したカードパイルが空
**対処**: 事前にカード配布を行うか、カードが存在するカードパイルを指定

#### 「配布先カードパイルが取得できませんでした」
**原因**: 無効なPile Indexが指定されている
**対処**: 0-2の範囲内でPile Indexを設定

## 技術的な詳細

### CardPile.TransferServiceの使用
全てのカード移動は`CardPile.TransferService.Transfer()`メソッドを使用して実行されます。これにより：
- データ整合性の保証
- 適切なイベント発火
- エラーハンドリング

### 配布アルゴリズム
```csharp
int totalCards = stack.Cards.Count;
int distributableCards = Mathf.Min(totalCards, cardsPerPlayer * players.Count);
int actualCardsPerPlayer = distributableCards / players.Count;
```

### カード移動の安全性
- コレクション変更中の例外を避けるため、`ToList()`でコピーを作成
- 各転送の成功/失敗を個別にチェック
- 詳細なログ出力によるデバッグ支援

## 注意事項
- このクラスはテスト・デバッグ専用です
- 本番環境では使用しないでください
- プレイモードでのみ動作します
- FieldSetupManagerTesterとの連携が必須です
- カード移動は全てTransferServiceを通じて実行されます 