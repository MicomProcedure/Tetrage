# FieldSetupComponent 使用方法

## 概要
`FieldSetupComponent`は、フィールドセットアップに必要な静的設定を一元管理し、検証済みの`FieldSetupSettings`を提供するMonoBehaviourコンポーネントです。

## 設計思想

### 責任範囲
- **静的設定の提供**: プレハブ、位置、レイアウト等の静的な設定データ
- **設定検証**: `FieldSetupConfigurationValidator`による妥当性チェック
- **設定構築**: `FieldSetupSettingsBuilder`による設定オブジェクト構築
- **キャッシュ管理**: 一度構築した設定の再利用

### 依存関係
```
FieldSetupComponent
├── FieldSetupConfigurationValidator (内部利用)
├── FieldSetupSettingsBuilder (内部利用)
└── FieldSetupSettings (出力)
```

## セットアップ手順

### 1. プレハブの配置
```
Assets/Main/Prefabs/Settings/FieldSetupComponent.prefab
```
このプレハブをシーンに配置してください。

### 2. 必須設定

#### Configuration
- **PrefabConfig**: `FieldSetupPrefabConfig`アセット
  - パス: `Assets/Main/ScriptableObjects/FieldSetupConfig.asset`
  - 各種プレハブの参照を含む設定アセット

#### Scene Dependencies
- **StageRoot**: ステージオブジェクトの親Transform
- **PlayerRoot**: プレイヤーオブジェクトの親Transform
- **StageSpawnPositionPrefab**: ステージ位置設定プレハブ
- **PlayerLocationsPrefab**: プレイヤー位置設定プレハブ

#### カードパイルレイアウト設定
- **HandsPileLayoutAsset**: 手札レイアウト設定
- **TmpPileLayoutAsset**: 一時保持レイアウト設定
- **TargetPileLayoutAsset**: ターゲットレイアウト設定
- **TrashPileLayoutAsset**: トラッシュレイアウト設定
- **StackPileLayoutAsset**: スタックレイアウト設定

### 3. 各アセットの設定

#### FieldSetupPrefabConfig
```
プレハブ参照:
├── LocalPlayerViewPrefab
├── RemotePlayerViewPrefab
├── BotPlayerViewPrefab
├── CardViewPrefab
├── StageViewPrefab
└── 各種CardPileViewPrefab
```

#### CardPileLayoutAsset
各カードパイルタイプごとに設定：
```
レイアウト設定:
├── PileWidth: カードパイルの幅
├── MinSpacing: カード間の最小間隔
├── MaxSpacing: カード間の最大間隔
└── PositionOffset: 位置オフセット
```

## 主要機能

### 1. 統合設定取得メソッド

#### GetValidatedFieldSetupSettings()
```csharp
public FieldSetupSettings GetValidatedFieldSetupSettings()
```
- **機能**: 設定検証済みのFieldSetupSettingsを取得
- **特徴**: 
  - 自動的に設定検証を実行
  - キャッシュによる高速化
  - エラー時は詳細なException

#### GetValidatedFieldSetupSettings(int participantCount)
```csharp
public FieldSetupSettings GetValidatedFieldSetupSettings(int participantCount)
```
- **機能**: 参加者数との互換性検証付きで設定を取得
- **特徴**:
  - 参加者数とプレイヤー位置数の整合性チェック
  - より安全な設定取得

### 2. キャッシュ管理

#### ResetSettings()
```csharp
public void ResetSettings()
```
- **機能**: 構築済み設定のキャッシュをクリア
- **使用場面**: Inspector設定変更後の再構築

### 3. Inspector テスト機能

#### 設定を検証
```
右クリック → "設定を検証"
```
- 現在の設定の妥当性をチェック
- 設定ミスの早期発見

#### FieldSetupSettingsを構築テスト
```
右クリック → "FieldSetupSettingsを構築テスト"
```
- 実際の設定構築をテスト実行
- オブジェクト生成は行わない軽量テスト

#### 設定をリセット
```
右クリック → "設定をリセット"
```
- キャッシュされた設定をクリア
- 次回取得時に再構築

## 使用方法

### 基本的な使用パターン

#### 1. テスト環境での使用
```csharp
// FieldSetupManagerTesterなどで
var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);
var fieldSetupManager = new FieldSetupManager(settings, dependencies);
fieldSetupManager.SetupField(participantInfoList);
```

#### 2. 本番環境での使用
```csharp
// ApplicationManagerなどで
var fieldSetupComponent = FindObjectOfType<FieldSetupComponent>();
var participantCount = GetParticipantCount(); // 実際の参加者数取得
var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);
var fieldSetupManager = new FieldSetupManager(settings, dependencies);
fieldSetupManager.SetupField(participantInfoList);
```

#### 3. 設定検証のみ
```csharp
// 設定の妥当性チェックのみ実行
try
{
    var settings = fieldSetupComponent.GetValidatedFieldSetupSettings();
    Debug.Log("設定は正常です");
}
catch (Exception e)
{
    Debug.LogError($"設定エラー: {e.Message}");
}
```

## エラーハンドリング

### 主要なException

#### InvalidOperationException
```csharp
// 設定検証失敗
"FieldSetupComponent: 設定検証に失敗しました"

// 参加者数互換性エラー
"FieldSetupComponent: 参加者数(N)と設定が互換性がありません"
```

### エラー対処法

#### 設定検証エラー
1. Inspector上で「設定を検証」を実行
2. エラーメッセージを確認
3. 対象の設定を修正
4. 「設定をリセット」でキャッシュクリア

#### 参加者数互換性エラー
1. PlayerLocationsPrefabの位置数を確認
2. 参加者数以上の位置が設定されているか確認
3. 必要に応じて位置を追加

## パフォーマンス

### キャッシュ機能
- **初回構築**: 設定検証 + ビルド実行
- **2回目以降**: キャッシュから高速取得
- **リセット**: Inspector設定変更時に自動クリア

### 推奨使用パターン
```csharp
// ✅ 推奨: 一度取得してキャッシュ活用
var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);

// ❌ 非推奨: 毎回新しい取得（キャッシュの恩恵なし）
for (int i = 0; i < 10; i++)
{
    var settings = fieldSetupComponent.GetValidatedFieldSetupSettings();
}
```

## 開発Tips

### 設定変更時の注意点
1. Inspector設定変更後は「設定をリセット」を実行
2. プレハブ参照変更時はプロジェクト再ビルドを推奨
3. CardPileLayoutAsset変更時は対応するカードパイルタイプを確認

### デバッグ時の確認項目
1. **設定の妥当性**: 「設定を検証」で基本チェック
2. **構築テスト**: 「FieldSetupSettingsを構築テスト」で詳細チェック
3. **参加者数**: 実際の参加者数での互換性テスト

### 本番環境への移行
1. テスト環境で十分な検証を実施
2. 同じFieldSetupComponentプレハブを本番環境でも使用
3. ApplicationManagerなどの上位モジュールで統合

## 設計の利点

### 従来との比較
| 項目 | 従来 | 現在 |
|------|------|------|
| 設定方法 | 上位モジュールで個別設定 | 統合メソッド1回呼び出し |
| 検証タイミング | 実行時エラー | 事前検証 |
| 再利用性 | 各モジュールで重複実装 | 共通コンポーネント |
| 保守性 | 設定変更時の影響範囲が広い | 影響範囲が限定的 |

### アーキテクチャ上の利点
- **単一責任**: 設定管理に特化
- **依存関係の明確化**: 内部で必要なクラスを統合
- **テスタビリティ**: Inspector機能による容易なテスト
- **拡張性**: 新しい設定項目の追加が容易

## 注意事項
- このコンポーネントは静的設定のみを管理します
- 動的な参加者情報は別途`Dealer`などから取得してください
- 設定変更後は必ずキャッシュをリセットしてください
- Inspector設定変更時は`OnValidate()`が自動的にキャッシュをクリアします 