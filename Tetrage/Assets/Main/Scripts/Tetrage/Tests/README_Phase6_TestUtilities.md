# Phase 6: テストユーティリティ実装ガイド

## 概要
Phase 6では、PlayModeテストを支援するユーティリティを実装しました。Photon接続なしで高速にゲームロジックを検証できます。

---

## 実装したコンポーネント

### 1. PlayModeTestHarness
**ファイル**: `Assets/Main/Scripts/Tetrage/Tests/PlayModeTestHarness.cs`

**役割**: PlayModeテスト用のエントリポイント

**主な機能**:
- GameSceneの初期化（Photon不要）
- テスト用PlayerInfo生成
- NetworkMode切り替え
- 乱数シード固定（再現性のあるテスト）
- 自動クリーンアップ

**使用例**:
```csharp
[UnityTest]
public IEnumerator Test_GameLogic()
{
    // セットアップ
    var harness = new PlayModeTestHarness();
    yield return harness.SetupGameScene(
        playerCount: 2,
        mode: NetworkMode.VirtualTransport,
        randomSeed: 12345 // 再現性のあるテスト
    ).ToCoroutine();

    // ゲーム開始
    yield return harness.StartGame().ToCoroutine();

    // テスト実行...

    // クリーンアップ
    harness.Teardown();
}
```

---

### 2. IVirtualLogicFeeder / VirtualLogicFeeder
**ファイル**: 
- `Assets/Main/Scripts/Tetrage/Network/Gameplay/IVirtualLogicFeeder.cs`
- `Assets/Main/Scripts/Tetrage/Network/Gameplay/VirtualLogicFeeder.cs`

**役割**: DomainEventを直接EventBusに注入

**主な機能**:
- ネットワーク層をバイパス
- DomainEventを直接発行
- LogicInjectionモードで使用

**使用例**:
```csharp
[UnityTest]
public IEnumerator Test_UIUpdate_OnTurnStarted()
{
    // セットアップ
    var harness = new PlayModeTestHarness();
    yield return harness.SetupGameScene(2, NetworkMode.LogicInjection).ToCoroutine();

    // VirtualLogicFeederを作成
    var eventBus = harness.GetEventBus();
    var feeder = new VirtualLogicFeeder(eventBus);

    // イベントを購読
    bool eventReceived = false;
    var disposable = new CompositeDisposable();
    eventBus.TurnStarted
        .Subscribe(e => { eventReceived = true; })
        .AddTo(ref disposable);

    // DomainEventを直接注入
    feeder.Feed(new TurnStartedEvent(
        sequence: 1,
        currentPlayerId: new PlayerId(1)
    ));

    yield return null; // 1フレーム待機

    // 検証
    Assert.IsTrue(eventReceived);
    
    // クリーンアップ
    disposable.Dispose();
    harness.Teardown();
}
```

---

### 3. GameSceneDebugEntrySimple
**ファイル**: `Assets/Main/Scripts/Tetrage/Tests/GameSceneDebugEntrySimple.cs`

**役割**: GameSceneを直接起動した時のDebugエントリポイント

**主な機能**:
- #if UNITY_EDITORで保護（ビルドに影響なし）
- ApplicationManager検出時に自己無効化
- Inspector上で設定変更可能
- PlayModeTestHarness統合

**Inspector設定**:
- **Enable Debug Mode**: Debugモードを有効化
- **Player Count**: プレイヤー数（2-4）
- **Network Mode**: VirtualTransport / LogicInjection
- **Auto Start Game**: 自動ゲーム開始
- **Random Seed**: 乱数シード固定（-1で無効）

**使い方**:
1. GameSceneを開く
2. Hierarchyに「GameSceneDebugEntrySimple」を配置
3. Inspectorで設定
4. Playボタンを押す → 自動的に初期化・ゲーム開始

---

### 4. サンプルPlayModeテスト
**ファイル**: `Assets/Main/Tests/PlayMode/GameLogicPlayModeTests.cs`

**含まれるテスト**:
1. `Test_GameScene_Initialize_Success`: 初期化テスト
2. `Test_EventBus_Access`: EventBus取得テスト
3. `Test_LogicInjection_PublishDomainEvent`: DomainEvent発行テスト
4. `Test_VirtualLogicFeeder_FeedEvent`: VirtualLogicFeeder使用テスト
5. `Test_RandomSeed_Reproducibility`: 乱数シード固定テスト

**実行方法**:
```
1. Unity Test Runnerを開く（Window > General > Test Runner）
2. PlayModeタブを選択
3. Run All または個別テストを実行
```

---

## テストモード別の使い分け

### NetworkMode.VirtualTransport
**用途**: ネットワーク通信込みの統合テスト

**特徴**:
- ✅ Photon不要
- ✅ Broadcaster/Receiver経由でイベント送受信
- ✅ 実際のネットワークフローに近い
- ⚠️ DTO変換・シリアライズを含む

**例**:
```csharp
yield return harness.SetupGameScene(2, NetworkMode.VirtualTransport).ToCoroutine();
// ネットワークアクションを実行
// イベントが正常に配信されることを検証
```

---

### NetworkMode.LogicInjection
**用途**: UI層の単体テスト、イベントハンドラテスト

**特徴**:
- ✅ ネットワーク層を完全にバイパス
- ✅ DomainEventを直接発行
- ✅ 高速（DTO変換なし）
- ⚠️ ネットワーク層の動作は検証されない

**例**:
```csharp
yield return harness.SetupGameScene(2, NetworkMode.LogicInjection).ToCoroutine();
var feeder = new VirtualLogicFeeder(harness.GetEventBus());
feeder.Feed(new TurnStartedEvent(...)); // 即座にUIが更新される
```

---

## ベストプラクティス

### 1. テストの独立性を保つ
```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    // 各テストで新しいシーンをロード
    yield return SceneManager.LoadSceneAsync("GameScene");
    yield return null;
}

[UnityTearDown]
public IEnumerator TearDown()
{
    // 必ずクリーンアップ
    _harness?.Teardown();
    _harness = null;
    yield return null;
}
```

### 2. 乱数シードを固定する
```csharp
// 再現性のあるテスト
yield return harness.SetupGameScene(
    playerCount: 2,
    mode: NetworkMode.VirtualTransport,
    randomSeed: 12345 // ← 固定シード
).ToCoroutine();
```

### 3. R3のAddToでリソース管理
```csharp
var disposable = new CompositeDisposable();

eventBus.TurnStarted
    .Subscribe(e => { /* ... */ })
    .AddTo(ref disposable); // ← 自動購読解除

// テスト終了時
disposable.Dispose();
```

### 4. 非同期処理の待機
```csharp
// イベント発行
eventBus.Publish(new TurnStartedEvent(...));

// 1フレーム待機（イベント処理の完了を待つ）
yield return null;

// 検証
Assert.IsTrue(eventReceived);
```

---

## トラブルシューティング

### Q1: GameManagerが見つからない
**A**: GameSceneにGameManagerが配置されているか確認してください。

### Q2: EventBusがnull
**A**: `harness.SetupGameScene()`が完了した後にアクセスしてください。

### Q3: テストが失敗する
**A**: 
1. TearDownでクリーンアップしているか確認
2. 乱数シードを固定してみる
3. VirtualTransportHub.DestroyInstance()を呼んでいるか確認

### Q4: ApplicationManager検出エラー
**A**: GameSceneを単独で起動していることを確認してください。TitleSceneから起動している場合はApplicationManagerが存在するため、DebugEntryは自動的に無効化されます。

---

## 実装完了状況

| コンポーネント | 状態 | ファイル |
|--------------|------|---------|
| PlayModeTestHarness | ✅完了 | Tests/PlayModeTestHarness.cs |
| IVirtualLogicFeeder | ✅完了 | Network/Gameplay/IVirtualLogicFeeder.cs |
| VirtualLogicFeeder | ✅完了 | Network/Gameplay/VirtualLogicFeeder.cs |
| GameSceneDebugEntrySimple | ✅完了 | Tests/GameSceneDebugEntrySimple.cs |
| サンプルPlayModeテスト | ✅完了 | Tests/PlayMode/GameLogicPlayModeTests.cs |

---

## 次のステップ（Phase 7）

Phase 7「統合テスト」では、実際のゲームシナリオで以下を自動化します：
1. VirtualTransportで `StartGame -> GameEnded` までの完走検証
2. VirtualTransport通信経路（DTO受信 -> Converter -> Applier -> EventBus反映）の検証
3. LogicInjectionでのUI反映検証
4. バグ修正・最適化

---

## 検証資産（再定義）

- 維持: `PlayModeTestHarness`, `PlayModeTestHelper`, `GameSceneDebugEntrySimple`, `GameLogicPlayModeTests`
- 廃止: 旧手動デバッグ系スクリプト（旧Photonデバッグ入口、Factory/FieldSetup/ResultUI単体デバッガ群）

---

## 関連ドキュメント

- `NetworkSystemEnhancements.md`: Phase 3-6の設計書
- `README_DI_Usage.md`: 依存性注入ガイド（削除済み、本ドキュメントに統合）
- 既存の`GameSceneDebugEntry.cs`: Photon依存版（既存実装を維持）

