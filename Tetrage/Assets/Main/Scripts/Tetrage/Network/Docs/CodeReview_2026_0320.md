# Tetrage ネットワーク層 コードレビュー報告書

**日付:** 2026-03-20  
**対象:** ネットワークシステム強化（NetworkSystemEnhancements.md に基づく実装）  
**ステータス:** レビュー完了

---

## 1. 実装状況サマリ

| フェーズ | 内容 | 状況 | 備考 |
|---------|------|------|------|
| Phase 1 | INetworkContext導入 | **完了** | Photon/Virtual両実装あり |
| Phase 2 | IPlayerIdMapper導入 | **完了** | TurnGateもPlayerId対応済み |
| Phase 2.5 Sub1 | DomainEvent定義（13種） | **完了** | `Tetrage.Core.Events` |
| Phase 2.5 Sub2 | DomainEventConverter | **完了** | ToDomain: 全13種、ToDto: 2種のみ |
| Phase 2.5 Sub3 | R3EventBus | **完了** | SimpleGameplayEventBusは残存 |
| Phase 2.5 Sub4-1 | NetworkEventApplier改修 | **完了** | 変換専用に縮小 |
| Phase 2.5 Sub4-2 | GameplayDomainEventHandler | **完了** | 一部ハンドラが空実装 |
| Phase 2.5 Sub4-3 | 初期化処理追加 | **完了** | GameplayNetworkControllerに統合 |
| Phase 2.5 Sub5 | UI層移行 | **完了** | Observable.Subscribe().AddTo() |
| Phase 3 | INetworkAdapterFactory導入 | **完了** | Photon/Virtual切替動作 |
| Phase 4 | NetworkMode管理 | **完了** | ロック機構あり |
| Phase 5 | VirtualTransport実装 | **完了** | Hub/Broadcaster/Receiver |
| Phase 6 | テストユーティリティ | **部分完了** | ハーネス・Feeder作成済、自動テスト未作成 |
| Phase 7 | 統合テスト | **未着手** | — |

---

## 2. 検出された欠陥

### ~~欠陥1: HostActionProcessorにPhoton直接依存が残存~~ ✅ 修正済み (2026-03-20)

- **深刻度:** 10/10（VirtualTransportモードのブロッカー）
- **ファイル:** `Network/Gameplay/HostActionProcessor.cs`
- **修正内容:**
  - `Photon.Pun.PhotonNetwork.PlayerList`の直接参照を除去
  - コンストラクタに`IPlayerIdMapper`を追加し、`GetAllActorNumbers()`経由で他プレイヤーを取得
  - TetrageSoloケースで`Broadcaster.Raise`が呼ばれていなかった問題も同時に修正
  - Drawロジックを簡潔化（デッドコード除去、メソッド分割）

---

### ~~欠陥2: Dealer.StartSingleTurnAsync()でTurnStartedが二重発行~~ ✅ 修正済み (2026-03-20)

- **深刻度:** 8/10（ゲームロジック破綻の可能性）
- **ファイル:** `Managers/Dealer.cs`
- **修正内容:**
  - `PublishTurnStart()`からネットワーク送信（`_messenger?.PublishTurnStarted()`）を除去
  - TurnStartedの発行責務を`FirstDeal()`（初回）と前ターン末尾（2回目以降）に一本化
  - `PublishTurnStart()`はターンカウント管理専用に限定

---

### 欠陥3: GameManager.BuildInitialPlayerOrder()がPlayerIdをActorNumberとして送信

- **深刻度:** 7/10（ID誤同定によるゲーム破綻）
- **ファイル:** `Managers/GameManager.cs` L462-472
- **内容:** `GameStartedEvent.playerActorNumbers`フィールドにActorNumberを格納すべきところ、`_playerRegistry`のキー（PlayerId）の`.Value`を使用している。
- **影響:** PlayerIdは1,2,3...のシーケンシャル、ActorNumberはPhotonが割り当てる任意の整数。途中退出・再接続時に両者が一致しなくなり、全プレイヤーの同定が破綻する。
- **修正方針:** `IPlayerIdMapper`を使用してPlayerId→ActorNumber変換を行う。

```csharp
// 現在（問題あり）
foreach (var kv in _playerRegistry.Entries)
{
    list.Add(kv.Key.Value); // PlayerId.Value を ActorNumber として送信
}

// 修正案
foreach (var kv in _playerRegistry.Entries)
{
    if (_playerIdMapper.TryGetActorNumber(kv.Key, out var actorNumber))
    {
        list.Add(actorNumber);
    }
}
```

---

### 欠陥4: ApplicationManagerがPhotonモード専用の初期化フロー

- **深刻度:** 7/10（VirtualTransportモードの本番パスが存在しない）
- **ファイル:** `Managers/ApplicationManager.cs` L197-258
- **内容:** `InitializeGameSceneAsync()`が`PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom`を直接チェックしており、`_networkMode`による分岐が実装されていない。`BuildPlayerInfosFromPhoton()`もPhoton専用。
- **影響:** ApplicationManager経由のVirtualTransport/LogicInjection初期化が永久にブロックされる。現状はPlayModeTestHarnessで迂回しているが、本番利用パス（将来のオフライン対戦など）が塞がれている。
- **修正方針:** `_networkMode`に応じた初期化パスを分岐する。

```csharp
// 修正案（概要）
private async UniTaskVoid InitializeGameSceneAsync(CancellationToken ct)
{
    switch (_networkMode)
    {
        case NetworkMode.RealPhoton:
            await WaitForPhotonConnection(ct);
            _networkContext = new PhotonNetworkContext();
            players = BuildPlayerInfosFromPhoton(out _playerIdMapper);
            break;
        case NetworkMode.VirtualTransport:
        case NetworkMode.LogicInjection:
            _networkContext = new VirtualNetworkContext(...);
            players = BuildVirtualPlayerInfos(out _playerIdMapper);
            break;
    }
    // 共通の初期化処理
    gameManager.Initialize(players, userInfo, _networkContext, _networkMode, _playerIdMapper);
}
```

---

### 欠陥5: DealerPlanEmitterとDealerNetworkMessengerの責務重複

- **深刻度:** 5/10（保守性低下・混乱の原因）
- **ファイル:**
  - `Network/Gameplay/DealerPlanEmitter.cs`
  - `Network/Gameplay/DealerNetworkMessenger.cs`
- **内容:** 両クラスが「DealerPlanをネットワークDTOに分解して送信する」という同一責務を持つ。`DealerNetworkMessenger`は`IPlayerIdMapper`を使用してPlayerId→ActorNumber変換を行うが、`DealerPlanEmitter`はこの変換を行わない。DealerはMessengerのみを使用しており、Emitterは事実上デッドコード化している。
- **影響:** どちらを使うべきか不明瞭。将来の修正時に片方だけ修正してもう片方が放置されるリスク。
- **修正方針:** `DealerPlanEmitter`を削除し、`DealerNetworkMessenger.PublishDealerPlan()`に統合する。

---

### 欠陥6: SimpleGameplayEventBusが残存

- **深刻度:** 3/10（デッドコード）
- **ファイル:** `Network/Gameplay/GameplayEventBus.cs` L181-220
- **内容:** `[Obsolete]`マーク付きで、全メソッドが`NotImplementedException`をthrowする状態で残存している。
- **影響:** AGENTS.mdのリファクタ方針「旧仕様は残さず新仕様に一新させる」に違反。誤って参照された場合にランタイム例外が発生する。
- **修正方針:** クラス定義ごと削除する。

---

### 欠陥7: DomainEventConverterがマッピング失敗時にPlayerId(-1)をフォールバック

- **深刻度:** 5/10（サイレントなデータ破損）
- **ファイル:** `Core/Events/DomainEventConverter.cs` 複数箇所
- **内容:** `TryGetPlayerId()`が失敗した場合、`PlayerId(-1)`をフォールバック値として生成し、WarningログのみでDomainEventの生成を続行する。
- **影響:** 不正な`PlayerId(-1)`がドメイン層に流入し、`GameplayDomainEventHandler`でプレイヤー検索が失敗する。エラーの根本原因が変換層ではなくハンドラ層で検出されるため、デバッグが困難になる。
- **修正方針:** マッピング失敗時は例外をスローするか、`Result<T>`パターンで呼び出し元に失敗を通知する。少なくとも`Debug.LogError`に昇格すべき。

```csharp
// 現在（サイレント失敗）
if (!_playerIdMapper.TryGetPlayerId(dto.currentPlayerActorNumber, out var playerId))
{
    Debug.LogWarning(...);
    playerId = new PlayerId(-1); // 不正値が伝播
}

// 修正案
if (!_playerIdMapper.TryGetPlayerId(dto.currentPlayerActorNumber, out var playerId))
{
    throw new InvalidOperationException(
        $"ActorNumber {dto.currentPlayerActorNumber} のPlayerIdマッピングが見つかりません");
}
```

---

### 欠陥8: GameManagerにPhoton残存参照

- **深刻度:** 3/10（VirtualTransportモードでNullReferenceException）
- **ファイル:** `Managers/GameManager.cs`
- **内容:**
  - L13: `using Photon.Pun;`
  - L161: `PhotonNetwork.LocalPlayer.ActorNumber` をデバッグログで参照
- **影響:** VirtualTransportモードで`PhotonNetwork.LocalPlayer`がnullのためNRE発生。
- **修正方針:** `_networkContext.UserActorNumber`に置換する。

```csharp
// 現在
Debug.Log($"GameManager: GameContext created, userPlayerId: {userPlayer.PlayerId}, PhotonActorId: {PhotonNetwork.LocalPlayer.ActorNumber}");

// 修正案
Debug.Log($"GameManager: GameContext created, userPlayerId: {userPlayer.PlayerId}, ActorNumber: {_networkContext.UserActorNumber}");
```

---

### 欠陥9: R3EventBusの過剰ログ出力

- **深刻度:** 2/10（パフォーマンス影響）
- **ファイル:** `Network/Gameplay/GameplayEventBus.cs` L80-156
- **内容:** R3EventBusの全Publish()メソッドにDebug.Logが含まれる。本番ビルドでも実行される。
- **影響:** 毎フレーム複数回のイベント発行がある場合、GCアロケーション（string interpolation）がパフォーマンスに影響する。
- **修正方針:** 条件付きコンパイルでデバッグビルドのみに限定する。

```csharp
// 修正案
public void Publish(DomainEvents.TurnStartedEvent e)
{
    _turnStarted.OnNext(e);
#if TETRAGE_DEBUG_EVENTS
    Debug.Log($"R3EventBus: TurnStarted published, Sequence={e.Sequence}");
#endif
}
```

---

### 欠陥10: VirtualTransportHubのSingletonパターン

- **深刻度:** 4/10（テスト並列実行時の干渉）
- **ファイル:** `Network/Gameplay/Virtual/VirtualTransportHub.cs`
- **内容:** staticシングルトンで実装されているため、複数のテストシナリオを並列実行すると相互干渉する。
- **影響:** テストの独立性が保証されない。将来のCI/CDパイプラインでFlaky testの原因になる可能性。
- **修正方針:** インスタンスベースに移行し、`VirtualNetworkAdapterFactory`から注入する。`DestroyInstance()`による手動リセットは暫定措置として残せるが、根本的にはDIで解決すべき。

---

## 3. 未達成事項

### 3.1 機能未実装

| 項目 | 説明 | 関連フェーズ |
|------|------|-------------|
| DomainEvent→DTO変換の不完全 | `DomainEventConverter.ToDto()`がTurnStarted/TurnEndedの2種のみ実装。残り11種が未実装。 | Phase 2.5 |
| GameEnded/FinishingGameのドメインロジック | `GameplayDomainEventHandler`の`OnGameEnded()`/`OnFinishingGame()`が空実装 | Phase 2.5 Sub4-2 |
| ScanPhaseのドメインロジック | `OnScanPhaseStarted()`/`OnScanPhaseEnded()`が空実装 | Phase 2.5 Sub4-2 |
| TetrageSoloアクションのBroadcast | `HostActionProcessor`のTetrageSoloケースで`Broadcaster.Raise`が呼ばれていない（`break`のみ） | — |
| ApplicationManagerのマルチモード初期化 | VirtualTransport/LogicInjectionの初期化パスが未実装 | Phase 4 |
| PlayModeテストケース | 自動化された統合テストが未作成（ハーネスのみ存在） | Phase 6-7 |

### 3.2 設計書記載の将来計画（未着手）

| 項目 | 備考 |
|------|------|
| リプレイ記録・再生 | データ構造・保存形式は方針のみ |
| ボット実装 | IBotPolicy/IObservation等のI/F未定義 |
| チュートリアルシステム | ITutorialScenario等のI/F未定義 |
| 乱数シード固定機構（IRandomProvider） | PlayModeTestHarnessでRandom.InitStateのみ |
| 入力制御・UIロック（IInputController） | 未実装 |

---

## 4. 推奨する着手順序

### 第1優先: VirtualTransportブロッカー修正 (推奨度: 10/10)

VirtualTransportモードの動作を阻害している欠陥を修正し、Phase 7に進める状態にする。

1. **欠陥1** HostActionProcessorのPhoton依存除去
2. **欠陥2** Dealer TurnStarted二重発行修正
3. **欠陥3** BuildInitialPlayerOrder()のID変換修正
4. **欠陥8** GameManagerのPhoton残存参照除去

**見積もり:** 1-2日

### 第2優先: コードクリーンアップ (推奨度: 8/10)

デッドコード・重複コードを整理し、保守性を向上させる。

5. **欠陥5** DealerPlanEmitter削除（DealerNetworkMessengerに統合）
6. **欠陥6** SimpleGameplayEventBus削除
7. **欠陥7** DomainEventConverterのエラーハンドリング強化

**見積もり:** 0.5-1日

### 第3優先: ApplicationManagerマルチモード対応 (推奨度: 9/10)

PlayModeTestHarness以外からVirtualTransportモードを使えるようにする。

8. **欠陥4** ApplicationManagerに`_networkMode`分岐を追加

**見積もり:** 1-2日

### 第4優先: Phase 7 統合テスト着手 (推奨度: 8/10)

- VirtualTransportモードでのゲームフロー（開始→ターン→終了）E2Eテスト
- LogicInjectionモードでのUI反映テスト
- PlayModeTestHarnessを活用した自動テスト

**見積もり:** 3-5日

### 第5優先: 品質改善 (推奨度: 6/10)

9. **欠陥9** ログ出力の条件付きコンパイル化
10. **欠陥10** VirtualTransportHubのインスタンスベース化

**見積もり:** 1日

---

## 5. アーキテクチャ所感

### 良い点

- **ドメインイベント分離が正しく実装されている**: NetworkDTO→DomainEvent→ドメインロジックの変換パイプラインが設計書通りに実現されている
- **R3統合が適切**: Observable/Subject による型安全なイベント購読と`AddTo()`による自動購読解除が全UI層に適用済み
- **INetworkAdapterFactory による切替機構**: Photon/VirtualTransportの切替が1箇所で完結する設計
- **PlayModeTestHarness**: Photon不要でGameManagerを初期化できるテストユーティリティが用意されている
- **TurnGateのPlayerId対応**: 設計書のリスク項目（7/10）だったTurnGate変換が正しく実装されている

### 改善すべき点

- **Photon直接依存の残存箇所が散在**: Phase 1で抽象化したにもかかわらず、HostActionProcessor・GameManager・ApplicationManagerにPhoton直接参照が残っている。全ソースコードを横断的にスキャンし、`Photon.Pun.PhotonNetwork`の直接使用を特定・除去すべき
- **デッドコードの蓄積**: SimpleGameplayEventBus、DealerPlanEmitter等、旧実装が残っており、コードベースの見通しが低下している
- **エラーハンドリングの一貫性不足**: DomainEventConverterはフォールバック、他は例外スローと、方針が統一されていない
