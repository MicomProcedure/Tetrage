# FieldSetupManagerTester 使用方法

## 概要
`FieldSetupManagerTester`は、`FieldSetupManager`の動作をプレイモードでテストするためのMonoBehaviourクラスです。シーン上の`FieldSetupComponent`から設定を取得し、フィールドセットアップの動作を確認できます。

## 新仕様の特徴
- **簡素化**: `FieldSetupComponent`の統合メソッドを使用
- **自動検証**: 設定の妥当性を自動でチェック
- **エラー安全**: 参加者数との互換性も自動検証

## セットアップ手順

### 1. シーンの準備
- 新しいシーンを作成するか、既存のシーンを使用
- `FieldSetupComponent`プレハブをシーンに配置
- 空のGameObjectを作成し、`FieldSetupManagerTester`スクリプトをアタッチ

### 2. FieldSetupComponentの設定
`FieldSetupComponent`で以下を設定してください：

#### 必須設定
- **PrefabConfig**: `FieldSetupPrefabConfig`アセット
- **StageRoot**: ステージオブジェクトの親となるTransform
- **PlayerRoot**: プレイヤーオブジェクトの親となるTransform
- **StageSpawnPositionPrefab**: ステージ位置設定プレハブ
- **PlayerLocationsPrefab**: プレイヤー位置設定プレハブ

#### カードパイルレイアウト設定
- **HandsPileLayoutAsset**: 手札レイアウト設定
- **TmpPileLayoutAsset**: 一時保持レイアウト設定
- **TargetPileLayoutAsset**: ターゲットレイアウト設定
- **TrashPileLayoutAsset**: トラッシュレイアウト設定
- **StackPileLayoutAsset**: スタックレイアウト設定

### 3. FieldSetupManagerTesterの設定
Inspector上で以下を設定：

#### 必須設定
- **FieldSetupComponent**: シーン上の`FieldSetupComponent`を参照

#### プレイヤー設定
- **TestPlayerList**: テスト用プレイヤー情報
  - `userId`: プレイヤーのユーザーID
  - `playerType`: プレイヤーの種類（Local/Remote/Bot）

#### テスト設定
- **AutoSetupOnStart**: Start時の自動セットアップ有無
- **PreventDuplicateSetup**: 重複セットアップの防止

## 使用方法

### 基本的な使用方法

#### 1. Inspector操作
1. プレイモードに入る
2. Inspector上の「フィールドセットアップを実行」ボタンをクリック
3. セットアップ結果がConsoleに出力されます
4. 「フィールドをクリア」ボタンで生成されたオブジェクトをクリア

#### 2. 右クリックメニュー操作
- コンポーネントを右クリック → 「フィールドセットアップを実行」
- コンポーネントを右クリック → 「フィールドをクリア」
- コンポーネントを右クリック → 「設定検証のみ実行」

#### 3. 自動実行
- `autoSetupOnStart`をtrueにすると、Start()時に自動でセットアップが実行されます

### 新機能

#### 設定検証のみ実行
```
右クリック → "設定検証のみ実行"
```
- 実際のオブジェクト生成は行わず、設定の妥当性のみをチェック
- 開発中の設定確認に便利

#### 自動的な参加者数チェック
- 参加者数とプレイヤー位置数の整合性を自動検証
- エラー時は詳細なメッセージを表示

## 出力例

### 成功時
```
FieldSetupManagerTester: フィールドセットアップが完了しました
=== FieldSetupManager セットアップ結果 ===
設定ソース: FieldSetupComponent（統合メソッド使用）
生成されたプレイヤー数: 2
プレイヤー1: UserID=LocalPlayer1, Hands=0枚, Tmp=0枚, Target=1枚
プレイヤー2: UserID=RemotePlayer1, Hands=0枚, Tmp=0枚, Target=1枚
ステージ - Stack: 52枚, Trash: 0枚
=== セットアップ完了 ===
```

### 設定検証のみの成功時
```
FieldSetupManagerTester: 参加者数(2)での設定検証を開始します
FieldSetupManagerTester: 設定検証が正常に完了しました
プレイヤー位置数: 4
ステージスポーン位置: (0.0, 0.0, 0.0)
```

## トラブルシューティング

### エラー: 「FieldSetupComponentが設定されていません」
- FieldSetupManagerTesterのFieldSetupComponent参照が設定されているか確認
- シーン上にFieldSetupComponentが存在するか確認

### エラー: 「設定検証に失敗しました」
- FieldSetupComponentの設定を確認
- 「FieldSetupSettingsを構築テスト」で詳細なエラーを確認

### エラー: 「参加者数と設定が互換性がありません」
- TestPlayerListの人数とPlayerLocationsの位置数を確認
- PlayerLocationsPrefabで十分な数の位置が設定されているか確認

### エラー: 「テストプレイヤーリストが設定されていません」
- TestPlayerListに少なくとも1人のプレイヤーを設定

## カスタマイズ

### プレイヤー数の変更
1. `testPlayerList`の要素数を変更
2. FieldSetupComponentのPlayerLocationsPrefabで対応する数の位置を設定

### プレイヤータイプの変更
各プレイヤーの`playerType`を変更することで、異なるタイプのプレイヤーをテストできます：
- `Local`: ローカルプレイヤー
- `Remote`: リモートプレイヤー
- `Bot`: ボットプレイヤー

### レイアウトの調整
FieldSetupComponentの各CardPileLayoutAssetを変更することで、カードパイルの表示をカスタマイズできます。

## 設計の利点

### 従来との比較
**従来**: 複数のプレハブを個別に設定する複雑な構成
**現在**: FieldSetupComponentから一括設定取得のシンプルな構成

### メリット
- **保守性**: 設定変更時の影響範囲が明確
- **安全性**: 事前検証により設定ミスを早期発見
- **簡単さ**: 最小限の設定で動作
- **再利用性**: 本番環境と同じFieldSetupComponentを使用

## 注意事項
- このクラスはテスト専用です。本番では使用しないでください
- プレイモードでのみ動作します
- FieldSetupComponentの設定が正しく行われていることを前提とします
- 設定に問題がある場合は、FieldSetupComponentの「FieldSetupSettingsを構築テスト」で詳細を確認してください 