# FieldSetupManagerTester 使用方法

## 概要
`FieldSetupManagerTester`は、`FieldSetupManager`の動作をプレイモードでテストするためのMonoBehaviourクラスです。Inspectorから必要なプレハブや設定を行い、ボタン操作でフィールドセットアップを実行できます。

## セットアップ手順

### 1. シーンの準備
- 新しいシーンを作成するか、既存のシーンを使用
- 空のGameObjectを作成し、`FieldSetupManagerTester`スクリプトをアタッチ

### 2. 必要なプレハブの設定
以下のプレハブをInspectorで設定してください：

#### プレハブ設定
- **CardViewPrefab**: `Assets/Main/Prefabs/Card/CardViewPrefab.Prefab`
- **LocalPlayerViewPrefab**: `Assets/Main/Prefabs/Player/BasicPlayerViewPrefab.prefab`
- **RemotePlayerViewPrefab**: `Assets/Main/Prefabs/Player/BasicPlayerViewPrefab.prefab`
- **BotPlayerViewPrefab**: `Assets/Main/Prefabs/Player/BasicPlayerViewPrefab.prefab`

#### カードパイルビュープレハブ
- **BasicCardPileViewPrefab**: `Assets/Main/Prefabs/Card/BasicCardPileViewPrefab.prefab`
- **HandsCardPileViewPrefab**: `Assets/Main/Prefabs/Card/HandsCardPileVIewPrefab.prefab`
- **TmpCardPileViewPrefab**: `Assets/Main/Prefabs/Card/TmpCardPileViewPrefab.prefab`
- **StackCardPileViewPrefab**: `Assets/Main/Prefabs/Card/StackCardPileViewPrefab.prefab`
- **TrashCardPileViewPrefab**: `Assets/Main/Prefabs/Card/TrashCardPileViewPrefab.prefab`

### 3. シーン内の参照設定
- **StageRoot**: ステージオブジェクトの親となるTransform
- **PlayerRoot**: プレイヤーオブジェクトの親となるTransform
- **PositionMarker**: プレイヤー配置位置を管理する`PositionMarker`コンポーネント

### 4. プレイヤー設定
- `testPlayerList`でテスト用プレイヤーの情報を設定
- 各プレイヤーの`userId`と`playerType`を指定

### 5. レイアウト設定
各カードパイルのレイアウト設定を調整できます：
- **pileWidth**: カードパイルの幅
- **minSpacing/maxSpacing**: カード間の最小/最大間隔
- **positionOffset**: 位置オフセット

## 使用方法

### Inspector操作
1. プレイモードに入る
2. Inspector上の「フィールドセットアップを実行」ボタンをクリック
3. セットアップ結果がConsoleに出力されます
4. 「フィールドをクリア」ボタンで生成されたオブジェクトをクリア

### 右クリックメニュー操作
- コンポーネントを右クリック → 「フィールドセットアップを実行」
- コンポーネントを右クリック → 「フィールドをクリア」

### 自動実行
- `autoSetupOnStart`をtrueにすると、Start()時に自動でセットアップが実行されます

## トラブルシューティング

### エラー: 「必要なコンポーネントが不足しています」
- 全てのプレハブ参照が正しく設定されているか確認
- StageRoot、PlayerRoot、PositionMarkerが設定されているか確認

### エラー: 「位置数とプレイヤー数が一致しません」
- PositionMarkerの位置数とtestPlayerListのプレイヤー数を一致させる
- PositionMarkerコンポーネントで十分な数の位置が設定されているか確認

### セットアップが完了しない
- Console出力でエラーメッセージを確認
- 各プレハブが正しくアサインされているか確認
- プレイモードで実行されているか確認

## カスタマイズ

### プレイヤー数の変更
1. `testPlayerList`の要素数を変更
2. PositionMarkerで対応する数の位置を設定

### レイアウトの調整
各レイアウト設定の数値を調整して、カードパイルの表示を変更できます。

### プレイヤータイプの変更
各プレイヤーの`playerType`を変更することで、異なるタイプのプレイヤーをテストできます。

## 注意事項
- このクラスはテスト専用です。本番では使用しないでください
- プレイモードでのみ動作します
- セットアップ実行前に、必ず必要なプレハブが設定されているか確認してください 