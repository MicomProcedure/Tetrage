# Tetrageネットワークシステム強化設計書

**バージョン:** 1.0  
**最終更新:** 2025-11-13  
**ステータス:** 設計承認済み

---

## 1. 概要

### 1.1 目的
Tetrageのネットワーク層を拡張し、以下を実現する：

1. **PlayModeテストの実現** - Photon接続不要でE2Eに近い検証を高速実行
2. **ドメイン層の独立性向上** - ネットワーク実装詳細からの分離
3. **将来機能への拡張性確保** - チュートリアル、リプレイ、ボット対戦の基盤構築

### 1.2 スコープ

**今回実装:**
- アダプタ工場パターン導入（Photon/Virtual切替）
- INetworkContext（Photon依存の抽象化）
- IPlayerIdMapper（PlayerId/ActorNumber分離）
- DomainEvent + UniRx統合
- VirtualTransport（同一プロセス内仮想通信）
- NetworkMode管理機構
- PlayModeテストハーネス

**将来計画（設計のみ）:**
- リプレイ記録・再生
- ボット実装
- チュートリアルシステム

---

## 2. 現状の課題と解決策

### 2.1 課題分析

| 課題 | 影響 | 優先度 |
|------|------|--------|
| **Photon直接依存** | `PhotonNetwork.IsMasterClient`等の直接参照が散在 | 高 |
| **PlayerId/ActorNumber混在** | ドメイン層がネットワーク固有IDに依存 | 高 |
| **PlayModeテスト不可** | E2E検証にPhoton接続が必須 | 高 |
| **イベント型の制約** | NetworkDTOがプリミティブ型のみ（強い型不可） | 中 |
| **購読管理の煩雑さ** | 手動購読解除によるメモリリーク懸念 | 中 |

### 2.2 採用アプローチ

**アプローチ1: ドメインイベント分離**を採用（推奨度: 9/10）

- NetworkDTOは転送層として維持
- DomainEventを別途定義し、ドメイン層で使用
- Boundary層（NetworkEventApplier/Emitter）で双方向変換
- UniRxによる型安全なイベント購読

**不採用:**
- アプローチ2（EventEnvelope）: 根本的な問題が解決しない
- アプローチ3（CQRS + Event Sourcing）: オーバーエンジニアリング

**選択理由:**
- 実装コストと効果のバランスが最適（2-3週間）
- 段階的移行が可能
- PlayerId分離と自然に統合
- 将来的なリプレイ/CQRS移行も容易

---

## 3. アーキテクチャ設計

### 3.1 主要コンポーネント

#### 3.1.1 NetworkMode（動作モード）
ゲームの起動モードを定義する列挙型。

| モード | 説明 | 用途 |
|--------|------|------|
| `RealPhoton` | Photonネットワーク経由 | 本番プレイ |
| `VirtualTransport` | 同一プロセス内仮想通信 | PlayMode E2Eテスト |
| `LogicInjection` | DomainEvent直接注入 | 部分再現、チュートリアル |
| `LocalVsBot` | ローカル vs ボット | オフライン練習（計画） |

#### 3.1.2 INetworkContext
Photon固有の状態情報を抽象化。

```csharp
public interface INetworkContext {
    bool IsHost { get; }           // ホスト判定
    int LocalActorNumber { get; }  // 自ActorNumber
    bool IsReady { get; }          // 接続状態
    bool IsInRoom { get; }         // ルーム参加状態
}
```

**実装:** `PhotonNetworkContext`（本番）、`VirtualNetworkContext`（テスト）

#### 3.1.3 IPlayerIdMapper
PlayerId（ドメイン内部ID）とActorNumber（ネットワークID）の双方向変換を管理。

```csharp
public interface IPlayerIdMapper {
    bool TryGetPlayerId(int actorNumber, out PlayerId playerId);
    bool TryGetActorNumber(PlayerId playerId, out int actorNumber);
    void Register(PlayerId playerId, int actorNumber);
}
```

**設計意図:**
- ドメイン層はPlayerIdのみで動作（ActorNumber不要）
- PhotonではActorNumberをシーケンシャルなPlayerId（1,2,3...）へ変換
- VirtualTransport/リプレイではActorNumber完全不要

#### 3.1.4 DomainEvent
ドメイン層専用のイベント型（NetworkDTOとは独立）。

**特徴:**
- 強い型使用可能（PlayerId、CardId、PileId等）
- 不変クラスベース
- ネットワーク制約から解放

#### 3.1.5 DomainEventConverter
NetworkDTOとDomainEventを相互変換（Boundary層でのみ使用）。

#### 3.1.6 UniRxEventBus
`IGameplayEventBus`をUniRxで実装。

**利点:**
- `AddTo()`による自動購読解除
- Rx演算子で宣言的処理
- 型安全なイベント購読

#### 3.1.7 INetworkAdapterFactory
`INetworkBroadcaster`と`INetworkReceiver`のペアを生成。NetworkModeに応じた実装を返す。

#### 3.1.8 VirtualTransport
同一プロセス内でネットワーク通信を模倣。

**コンポーネント:**
- `VirtualBroadcaster`/`VirtualReceiver`: 即時配送・順序保証
- `VirtualTransportHub`: ActorNumber割り当て、Peer間ルーティング
- `VirtualNetworkSettings`: 遅延/パケットロス注入（将来拡張）

### 3.2 システム構成図

```text
┌─────────────────────────────────────────────────────┐
│              ApplicationManager                      │
│  ・NetworkMode管理（起動時決定、ロック機構）         │
│  ・INetworkContext生成                               │
│  ・IPlayerIdMapper生成                               │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│                 GameManager                          │
│  ・INetworkAdapterFactory選択                       │
│  ・INetworkContext/IPlayerIdMapper注入              │
└───┬─────────────────┬───────────────────────────────┘
    │                 │
    ▼                 ▼
┌─────────┐   ┌──────────────────────────┐
│ Dealer  │   │ GameplayNetworkController│
│         │   │  ・INetworkBroadcaster   │
│         │   │  ・INetworkReceiver      │
└────┬────┘   │  ・NetworkEventApplier   │
     │        └────┬─────────────────────┘
     │ Emit       │ Receive & Apply
     ▼             ▼
┌──────────────────────────────────────────────────────┐
│          Network Boundary (変換層)                    │
│                                                       │
│  GameLifecycleEmitter   →  DomainEventConverter  ←  │
│         ↓                        ↕                   │
│    NetworkDTO          IPlayerIdMapper               │
│         ↓                        ↕                   │
│  INetworkBroadcaster      DomainEvent                │
│         ↓                        ↓                   │
│  (Photon/Virtual)        IGameplayEventBus (UniRx)   │
└──────────────────────────────┬───────────────────────┘
                               │
                   ┌───────────┼───────────┐
                   ▼           ▼           ▼
              ┌────────┐  ┌────────┐  ┌────────┐
              │ UI層   │  │ Bot層  │  │Test層  │
              │.AddTo()│  │購読    │  │購読    │
              └────────┘  └────────┘  └────────┘
```

### 3.3 データフロー

#### 3.3.1 実ネットワーク（RealPhoton）
```
ユーザー入力
  → NetworkActionBase.Execute()
  → PhotonBroadcaster.Raise(NetworkDTO)
  → Photon Server
  → PhotonReceiver.On<NetworkDTO>()
  → NetworkEventApplier.Apply(NetworkDTO)
     ├─ DomainEventConverter.ToDomain(DTO)
     ├─ ドメインロジック実行
     └─ IGameplayEventBus.Publish(DomainEvent)
  → UI層: IObservable<DomainEvent>.Subscribe().AddTo(this)
```

#### 3.3.2 仮想輸送（VirtualTransport）
```
ユーザー入力
  → NetworkActionBase.Execute()
  → VirtualBroadcaster.Raise(NetworkDTO)
  → VirtualTransportHub（同期配送）
  → VirtualReceiver.On<NetworkDTO>()
  → [以降は実ネットワークと同じ]
```

#### 3.3.3 ロジック注入（LogicInjection）
```
Test/TutorialScript
  → DomainEventを直接生成
  → IGameplayEventBus.Publish(DomainEvent)
  → UI層購読

※ NetworkDTO変換不要、ネットワーク層をバイパス
```

---

## 4. 実装計画

### 4.1 新規コンポーネント一覧

| コンポーネント | 種類 | 責務 |
|--------------|------|------|
| `INetworkContext` | Interface | ネットワーク状態の抽象化 |
| `PhotonNetworkContext` | Class | Photon版NetworkContext |
| `VirtualNetworkContext` | Class | Virtual版NetworkContext |
| `IPlayerIdMapper` | Interface | PlayerId/ActorNumber変換 |
| `PlayerIdMapper` | Class | マッピング実装 |
| `DomainEvent`系 | Class | ドメイン層イベント（13種類） |
| `DomainEventConverter` | Class | DTO⇔DomainEvent変換 |
| `UniRxEventBus` | Class | UniRx版EventBus |
| `INetworkAdapterFactory` | Interface | Broadcaster/Receiver生成 |
| `PhotonNetworkAdapterFactory` | Class | Photon版Factory |
| `VirtualNetworkAdapterFactory` | Class | Virtual版Factory |
| `VirtualBroadcaster` | Class | 仮想送信 |
| `VirtualReceiver` | Class | 仮想受信 |
| `VirtualTransportHub` | Class | Peer管理・ルーティング |
| `VirtualNetworkSettings` | Class | 遅延/欠損設定（将来） |
| `IVirtualLogicFeeder` | Interface | ロジック注入 |
| `VirtualLogicFeeder` | Class | ロジック注入実装 |
| `VirtualNetworkTestHarness` | Class | PlayModeテストユーティリティ |
| `NetworkMode` | Enum | 動作モード定義 |

### 4.2 変更が必要なクラス

| クラス | 変更種別 | 主な変更内容 |
|--------|----------|-------------|
| `GameplayNetworkController` | 大幅 | INetworkAdapterFactory受取、Broadcaster/Receiver生成を工場に委譲 |
| `NetworkEventApplier` | 大幅 | Apply()内でDomainEventConverter呼び出し、IGameplayEventBus.Publish()追加 |
| `InGameUIManager` | 大幅 | イベント購読を+=からIObservable.Subscribe().AddTo()へ変更 |
| `GameManager` | 中 | InitializeNetworking()でINetworkContext/IPlayerIdMapper注入 |
| `ApplicationManager` | 中 | NetworkMode管理追加、PlayerInfo生成時にIPlayerIdMapper構築 |
| `GameLifecycleEmitter` | 中 | Emit()前にDomainEvent→DTO変換追加 |
| `DealerPlanEmitter` | 中 | Emit()前にDomainEvent→DTO変換追加 |
| `Dealer` | 小 | PhotonNetwork.IsMasterClient → INetworkContext.IsHost置換 |
| `TurnGate` | 小 | Release(actorNumber)をRelease(playerId)へ変更 |
| `IGameplayEventBus` | 置換 | UniRx版に置換（IObservable<T>返却） |
| `SimpleGameplayEventBus` | 削除 | UniRxEventBusに置換 |

### 4.3 実装手順（段階導入）

#### Phase 1: INetworkContext導入（1-2日）【完了済・未レビュー】
**目的:** Photon直接依存の抽象化

- `INetworkContext`インターフェース定義
- `PhotonNetworkContext` / `VirtualNetworkContext`実装
- `Dealer` / `GameManager` / `ApplicationManager`に注入
- `PhotonNetwork.IsMasterClient` → `_context.IsHost`置換

**影響範囲:** Dealer, GameManager, ApplicationManager  
**リスク:** 低

#### Phase 2: IPlayerIdMapper導入（2-3日）【完了済・未レビュー】
**目的:** PlayerId/ActorNumber分離

- `IPlayerIdMapper`インターフェース定義
- `PlayerIdMapper`実装
- `ApplicationManager`でPlayerInfo生成時にマッピング構築
- `NetworkEventApplier` / `GameLifecycleEmitter`に注入
- **重要:** `TurnGate.Release()`の変換処理追加

**影響範囲:** ApplicationManager, NetworkEventApplier, GameLifecycleEmitter, TurnGate  
**リスク:** 中（TurnGate変換漏れに注意）

#### Phase 2.5: DomainEvent + UniRxEventBus導入（5-7日）
**目的:** ドメイン層とネットワーク層の分離、UniRx統合

**Sub-Phase 1:** DomainEvent定義（1-2日）
- `Tetrage.Core.Events`名前空間作成
- 13種類のDomainEvent定義（強い型使用）

**Sub-Phase 2:** DomainEventConverter実装（1-2日）
- 全13種類のイベント変換メソッド実装
- `IPlayerIdMapper`を使用してID変換

**Sub-Phase 3:** UniRxEventBus実装（1日）
- `IGameplayEventBus`をUniRx版に書き換え
- `Subject<T>`で実装

**Sub-Phase 4:** NetworkEventApplier改修（1-2日）
- `Apply()`メソッド内でDTO→DomainEvent変換
- DomainEventを`IGameplayEventBus.Publish()`

**Sub-Phase 5:** UI層移行（2-3日）
- 全UI系クラスを`IObservable.Subscribe().AddTo()`へ変更
- `CompositeDisposable`でライフサイクル管理

**影響範囲:** NetworkEventApplier, GameLifecycleEmitter, IGameplayEventBus, 全UI層  
**リスク:** 中高（UI層の全面改修、購読解除漏れに注意）

#### Phase 3: INetworkAdapterFactory導入（2-3日）
**目的:** Photon/Virtual切替機構

- `INetworkAdapterFactory`インターフェース定義
- `PhotonNetworkAdapterFactory` / `VirtualNetworkAdapterFactory`実装
- `GameplayNetworkController`をファクトリ利用に変更
- `GameManager.InitializeNetworking()`でファクトリ選択

**影響範囲:** GameplayNetworkController, GameManager  
**リスク:** 中（初期化順序に注意）

#### Phase 4: NetworkMode管理（1-2日）
**目的:** NetworkMode切替機構

- `NetworkMode` enum定義
- `ApplicationManager`に管理機能追加
- ロック機構実装（`GameManager.Initialize()`後は変更不可）

**影響範囲:** ApplicationManager  
**リスク:** 中（ロックタイミングの設計）

#### Phase 5: VirtualTransport実装（2-3日）
**目的:** 仮想輸送の完全実装

- `VirtualTransportHub`実装
- `VirtualBroadcaster` / `VirtualReceiver`実装
- `VirtualNetworkSettings`（空実装）
- `VirtualNetworkAdapterFactory`の完成

**影響範囲:** 新規追加のみ  
**リスク:** 低

#### Phase 6: テストユーティリティ実装（2-3日）
**目的:** PlayModeテスト支援

- `VirtualNetworkTestHarness`実装
- `IVirtualLogicFeeder`実装
- PlayModeテスト作成

**影響範囲:** Testsディレクトリ  
**リスク:** 低

#### Phase 7: 統合テスト（3-5日）
**目的:** 実際のテストで動作検証

- `ResultUISimpleTest`を仮想輸送で実行
- ロジック注入でのUIテスト
- バグ修正・最適化

**総工数見積:** 18-30日（約3.5-6週間）

---

## 5. 技術仕様

### 5.1 インターフェース定義

#### INetworkContext
```csharp
public interface INetworkContext {
    bool IsHost { get; }
    int LocalActorNumber { get; }
    bool IsReady { get; }
    bool IsInRoom { get; }
}
```

#### IPlayerIdMapper
```csharp
public interface IPlayerIdMapper {
    bool TryGetPlayerId(int actorNumber, out PlayerId playerId);
    bool TryGetActorNumber(PlayerId playerId, out int actorNumber);
    void Register(PlayerId playerId, int actorNumber);
    IReadOnlyList<PlayerId> GetAllPlayerIds();
    int[] GetAllActorNumbers();
}
```

#### INetworkAdapterFactory
```csharp
public interface INetworkAdapterFactory {
    INetworkBroadcaster CreateBroadcaster(ISerializer serializer);
    INetworkReceiver CreateReceiver(ISerializer serializer);
}
```

#### IGameplayEventBus（UniRx版）
```csharp
public interface IGameplayEventBus {
    IObservable<TurnStartedEvent> TurnStarted { get; }
    IObservable<CardMovedEvent> CardMoved { get; }
    IObservable<GameEndedEvent> GameEnded { get; }
    // ... 他のイベント
    
    void Publish(TurnStartedEvent e);
    void Publish(CardMovedEvent e);
    // ... 他のPublishメソッド
}
```

### 5.2 DomainEvent例

```csharp
namespace Tetrage.Core.Events
{
    // 不変クラスベース、強い型使用
    public sealed class TurnStartedEvent
    {
        public int Sequence { get; }
        public PlayerId CurrentPlayerId { get; }  // 強い型
        
        public TurnStartedEvent(int sequence, PlayerId currentPlayerId)
        {
            Sequence = sequence;
            CurrentPlayerId = currentPlayerId;
        }
    }
    
    public sealed class CardMovedEvent
    {
        public int Sequence { get; }
        public CardId CardId { get; }
        public PileId FromPileId { get; }
        public PileId ToPileId { get; }
        
        public CardMovedEvent(int sequence, CardId cardId, PileId fromPileId, PileId toPileId)
        {
            Sequence = sequence;
            CardId = cardId;
            FromPileId = fromPileId;
            ToPileId = toPileId;
        }
    }
}
```

### 5.3 UI層でのUniRx使用パターン

```csharp
public class InGameUIManager : MonoBehaviour
{
    private IGameplayEventBus _eventBus;
    private CompositeDisposable _disposables = new();
    
    public void Initialize(IGameContextProvider context)
    {
        _eventBus = context?.Events;
        
        // 基本的な購読
        _eventBus.TurnStarted
            .Subscribe(OnTurnStarted)
            .AddTo(_disposables);
        
        // フィルタリング: 特定プレイヤーのターンのみ
        _eventBus.TurnStarted
            .Where(e => e.CurrentPlayerId == _myPlayerId)
            .Subscribe(_ => ShowMyTurnUI())
            .AddTo(_disposables);
        
        // 複数イベントのマージ
        Observable.Merge(
            _eventBus.TurnStarted.AsUnitObservable(),
            _eventBus.CardMoved.AsUnitObservable()
        ).Subscribe(_ => UpdateUI()).AddTo(_disposables);
    }
    
    private void OnDestroy()
    {
        _disposables?.Dispose();
    }
}
```

---

## 6. リスクと注意点

### 6.1 実装時の重要注意事項

#### 🔴 高優先度（Phase 2-3で対応必須）

**1. TurnGateのPlayerId対応**
- **現状:** `TurnGate.Release(int actorNumber)`でActorNumberを使用
- **対応:** `IPlayerIdMapper`経由でPlayerId→ActorNumber変換を実装
- **影響箇所:** `NetworkEventApplier.Apply(TurnStartedEvent)`
- **リスク評価:** 7/10（変換漏れで手番制御が破綻）

**2. NetworkMode切り替えロック機構**
- **推奨設計:**
  - 起動時決定: `ApplicationManager`起動引数でモード固定
  - シーン遷移時のみ変更可: `GameScene`ロード前なら許可
  - テスト専用切り替え: `VirtualNetworkTestHarness.TemporarySwitch()`
  - ロック: `GameManager.Initialize()`後は変更不可
- **リスク評価:** 5/10（設計明確化で対応可能）

**3. UniRx購読のライフサイクル管理**
- **推奨パターン:** `CompositeDisposable`使用、`OnDestroy()`で確実にDispose
- **リスク評価:** 6/10（購読解除漏れに注意）

#### 🟡 中優先度（Phase 5-6で対応）

**4. 入力制御とUIロック**
- `IInputController`インターフェース追加を推奨
- テストモード/リプレイ再生時の入力無効化
- 優先度: 中（チュートリアル実装時に必須）

**5. 乱数シード固定機構**
- `VirtualNetworkTestHarness`で`Random.InitState(fixedSeed)`を実行
- ボット/リプレイの決定性保証に必須
- 優先度: 中（ボット実装時に必須）

**6. VirtualTransportのActorNumber管理**
- ActorNumber割り当て: 1,2,3...と順番に
- `VirtualTransportHub`でPeer管理
- 優先度: 中（Phase 5で実装）

#### 🟢 低優先度（将来実装時）

**7. リプレイデータの永続化フォーマット**
- JSON（可読性重視）またはMessagePack（サイズ重視）
- FormatVersionで互換性管理

**8. ボットのObservation設計**
- プレイヤー視点で見える情報のみ提供
- 完全情報を与えない設計

**9. チュートリアルの段階的イベント注入**
- `ITutorialScenario`インターフェース
- ユーザーアクション待機、イベント注入、ガイダンス表示

### 6.2 リスク評価サマリー

| リスク項目 | 評価(10点満点) | 対策 |
|-----------|---------------|------|
| 依存面の波及 | 3/10 | 変更は`GameplayNetworkController`中心に限定 |
| 初期化順序の複雑化 | 5/10 | ハーネスで統制、ドキュメント化 |
| TurnGate変換漏れ | 7/10 | **Phase 2で確実に対応、単体テスト作成** |
| DomainEvent + UniRx導入の波及 | 7/10 | UI層の全面改修、段階的移行 |
| 購読解除漏れ | 6/10 | `CompositeDisposable`パターン徹底 |

**総合推奨度: 9/10**

PlayModeでの回帰テストと速度の両立に有効で、将来の拡張性も確保。

---

## 7. 付録

### 7.1 NetworkDTO ⇔ DomainEvent 対応表

| NetworkDTO名 | EventCode | DomainEvent名 | ActorNumber含有 | 変換の注意点 |
|-------------|-----------|--------------|----------------|-------------|
| GameStartedEvent | 1 | GameStartedEvent | ✓ | playerActorNumbers配列変換 |
| TurnStartedEvent | 2 | TurnStartedEvent | ✓ | currentPlayerActorNumber変換 |
| CardMovedEvent | 3 | CardMovedEvent | - | 変換不要（そのまま） |
| TurnEndedEvent | 4 | TurnEndedEvent | ✓ | previousPlayerActorNumber変換 |
| GameEndedEvent | 5 | GameEndedEvent | ✓ | winnerActorNumbers配列変換 |
| CardVisibilityChangedEvent | 7 | CardVisibilityChangedEvent | - | 変換不要 |
| StartScanPhaseEvent | 9 | ScanPhaseStartedEvent | ✓ | playerActorNumbers配列変換 |
| EndScanPhaseEvent | 10 | ScanPhaseEndedEvent | - | 変換不要 |
| FinishingGameEvent | 11 | FinishingGameEvent | ✓ | winnerActorNumbers配列変換 |
| ActionRequestedEvent | 20 | ActionRequestedEvent | ✓ | actorPlayerId変換 |
| ActionResultEvent | 21 | ActionResultEvent | ✓ | actorPlayerId変換 |
| PileShuffledWithSeedEvent | 22 | PileShuffledEvent | - | 変換不要 |
| ListOrderDeclaredEvent | 23 | ListOrderDeclaredEvent | - | 汎用リスト（種別に応じて変換） |

### 7.2 用語集

| 用語 | 説明 |
|------|------|
| **NetworkMode** | ゲームの動作モード（RealPhoton/VirtualTransport/LogicInjection/LocalVsBot） |
| **PlayerId** | ドメイン層で使用する内部プレイヤーID（1,2,3,4...とシーケンシャル） |
| **ActorNumber** | Photonが割り当てるネットワークID（任意の整数） |
| **NetworkDTO** | ネットワーク転送用のデータ構造（プリミティブ型のみ） |
| **DomainEvent** | ドメイン層で使用するイベント（強い型使用可能） |
| **Boundary** | ネットワーク層とドメイン層の境界（変換が行われる場所） |
| **VirtualTransport** | 同一プロセス内でネットワーク通信を模倣する機構 |
| **LogicInjection** | DomainEventを直接注入する機構 |
| **UniRx** | Reactive Extensions for Unity（リアクティブプログラミングライブラリ） |
| **CompositeDisposable** | 複数のDisposableを一括管理するUniRxのクラス |

### 7.3 参考資料

- **Photon Unity Networking (PUN2)**: https://doc.photonengine.com/pun/current/getting-started/pun-intro
- **UniRx**: https://github.com/neuecc/UniRx
- **UniTask**: https://github.com/Cysharp/UniTask
- **Adapter Factory Pattern**: Gang of Four デザインパターン
- **Domain Events**: Domain-Driven Design (Eric Evans)

---

**文書管理情報**
- 作成者: 高草木
- レビュー状態: 承認済み
- 関連ドキュメント: `PlayModeVirtual_RemotePlayers.md`（初期検討版）

