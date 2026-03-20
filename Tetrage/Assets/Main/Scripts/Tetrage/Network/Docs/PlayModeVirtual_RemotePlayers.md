## PlayMode仮想リモート/ローカル/リプレイ対応計画

### 背景と方針
- **イベント設計: アプローチ1（ドメインイベント分離）を採用**
  - NetworkDTOは転送層として維持するが、**ドメインイベント（DomainEvent）を別途定義**
  - ドメイン層（ゲームロジック）はDomainEventのみを扱い、NetworkDTOを一切知らない
  - NetworkEventApplier/EmitterがネットワークBoundaryで変換を実行（DTO ⇔ DomainEvent）
  - 強い型（PlayerId, CardId等）をドメインイベントで使用可能
  - 採用理由: 実装コストと効果のバランスが最適、段階的移行が可能、PlayerId分離との相性が良い
  - 不採用: EventEnvelope（根本解決にならない）、CQRS（オーバーエンジニアリング）
- イベント購読には**UniRx**を使用
  - `IObservable<T>`でイベントストリームを表現
  - `AddTo()`による自動購読解除でメモリリーク防止
  - Where/Select/Merge等のRx演算子で宣言的なイベント処理
- 抽象の差し替え点を上位（アダプタ工場）に引き上げ、輸送層（Photon/Virtual）とロジック注入を切替可能にし、PlayModeでのネットワークテストを容易にする。

### 目的
- PlayModeテストの容易化: リモートプレイヤーの仮想化でE2Eに近い検証を高速に実行。
- ローカル対戦/ボット/チュートリアル/リプレイ（計画のみ）への拡張性を確保。

### スコープ
- やる: アダプタ工場導入、仮想輸送（即時/順序保証）、ロジック注入、PlayModeハーネス、モード切替。
- やらない: リプレイ/スナップショット/ボットの実装（今回は計画のみ）。

### アーキテクチャ構成
- 既存を活かす
  - `INetworkBroadcaster` / `INetworkReceiver` / `ISerializer`、`EventCode`、`NetworkDTO`、`NetworkEventApplier.Apply(TDto)`、`IGameplayEventBus`
- 新規コンポーネント
  - `INetworkAdapterFactory`: Broadcaster/Receiverを生成（Photon/Virtual切替）
  - `INetworkContext`: Photon固有の情報（IsHost/ActorNumber）を抽象化し、VirtualTransportでも使用可能に
  - `PhotonNetworkContext` / `VirtualNetworkContext`: 各モード用のNetworkContext実装
  - `IPlayerIdMapper`: PlayerId（ドメイン内部ID）とActorNumber（ネットワークID）のマッピング管理
    - ドメイン層はPlayerIdのみで動作し、ActorNumberを一切知らない設計に
    - PhotonではActorNumberからシーケンシャルなPlayerIdへ変換
    - VirtualTransport/リプレイではActorNumber不要でPlayerIdのみで動作
  - **DomainEvent**: ドメイン層で使用するイベント（強い型使用、クラスベース、不変オブジェクト）
    - `TurnStartedEvent`, `CardMovedEvent`, `GameEndedEvent`等
    - PlayerId/CardId等の強い型を直接使用可能
    - NetworkDTOとは独立して定義
  - `DomainEventConverter`: NetworkDTO ⇔ DomainEvent の変換を担当
    - `IPlayerIdMapper`を使用してActorNumber ⇔ PlayerId変換
    - ネットワークBoundaryでのみ使用（ドメイン層は変換を知らない）
  - **UniRxEventBus**: `IObservable<T>`ベースのイベント配信
    - `IGameplayEventBus`をUniRxで実装
    - 購読解除の自動化（`AddTo()`）、Rx演算子による宣言的処理
    - UI層はDomainEventを`IObservable<T>`として購読
  - `VirtualBroadcaster` / `VirtualReceiver`: 同一プロセス内で即時配送・順序保証
  - `VirtualNetworkSettings`: 将来の遅延/欠損/乱数注入フック
  - `IVirtualLogicFeeder`: DTOを直接`NetworkEventApplier`へ投入（部分再現/チュートリアル/リプレイ再生に使用）
  - `VirtualNetworkTestHarness`: PlayModeで仮想Peer生成・NetworkModeの一時切替ユーティリティ
  - `NetworkMode`（`RealPhoton`/`VirtualTransport`/`LogicInjection`/`LocalVsBot`）: `ApplicationManager`経由で起動切替

### 変更点（主なクラス）
- `GameplayNetworkController`: `INetworkAdapterFactory`を受け取り、Broadcaster/Receiverを工場から生成
- `PhotonAdapter`系: 既存実装は維持。`PhotonNetworkAdapterFactory`から返す役割に整理
- `ApplicationManager`: `NetworkMode`の設定/起動切替を提供。`NetworkMode`に応じた`INetworkContext`を生成・提供。PlayerInfo生成時にPlayerIdを独立割り当て、`IPlayerIdMapper`を生成
- `GameManager`: `InitializeNetworking()`で、モードに応じた工場を注入。`INetworkContext`を受け取り、Dealer/NetworkControllerへ注入。`IPlayerIdMapper`をNetworkEventApplierへ注入
- `Dealer`: 送出はDTO＋`DealerPlanEmitter`/`GameLifecycleEmitter`、適用は`NetworkEventApplier`で統一。`INetworkContext`を受け取り、`IsHost()`の判定をPhotonNetworkから分離
- **`NetworkEventApplier`**: NetworkDTO→DomainEventに変換してから処理
  - `DomainEventConverter`を使用してDTO→DomainEvent変換
  - ドメインロジック実行後、DomainEventを`IGameplayEventBus`へPublish
  - `IPlayerIdMapper`経由でActorNumber→PlayerId変換
- **`GameLifecycleEmitter`/`DealerPlanEmitter`**: DomainEvent→NetworkDTOに変換して送信
  - `DomainEventConverter`を使用してDomainEvent→DTO変換
  - `IPlayerIdMapper`経由でPlayerId→ActorNumber変換
- **`IGameplayEventBus`**: UniRx版に置き換え
  - `IObservable<DomainEvent>`でイベント配信
  - UI層は`AddTo()`で自動購読解除、Where/Select等のRx演算子で処理

### クラス関係図（変更後）

```text
                 +-----------------------+
                 |     ApplicationMgr    | ・・・NetworkMode切替
                 +-----------+-----------+
                             |
                             v
+----------------------------+----------------------------+
|                  GameManager                              |
+-----------+----------------+------------------+----------+
            |                |                  |
            v                v                  v
     +------+-----+   +------+-----+     +------+-----+
     |  Dealer    |   |  Gameplay  |     |  InGame UI |
     |            |   |  NetworkCtl|     |  (views)   |
     +--+-----+---+   +--+-----+---+     +------------+
        |     |          |     |
        |     |          |     | uses INetworkAdapterFactory
        |     |          |     +--------------------+
        |     |          v                          v
        |     |   +------+-------+        +----------------------+
        |     |   |INetworkBroadc|        | INetworkReceiver     |
        |     |   +------+-------+        +----------+-----------+
        |     |          |                           |
        |     |          |  (Photon or Virtual)      |
        |     |   +------+-----+              +------+-----+
        |     |   |  Photon*   |              |  Photon*   |
        |     |   +------------+              +------------+
        |     |   |  Virtual*  |              |  Virtual*  |
        |     |   +------------+              +------------+
        |     |
        |     | emits DealerPlan/Turn events via Broadcaster
        |     v
   +----+-------------------------------+
   |  DealerPlanEmitter / LifecycleEmtr |
   +------------------------------------+

Receiver -> DTO -> NetworkEventApplier -> DomainEventConverter -> DomainEvent
                                             |
                                             v
                                    IGameplayEventBus (UniRx)
                                             |
                                    IObservable<DomainEvent>
                                             |
                                             v
                                    UI層 (.Subscribe().AddTo(this))
                                    Bot層 (IGameplayEventBus購読)

                      IVirtualLogicFeeder -> DomainEvent直接生成 -> IGameplayEventBus

ReplayRecorder(計画)は Broadcaster & Receiver を傍受、ReplayPlayer(計画)は
IVirtualLogicFeederでDTOを適用。
```

### フロー（代表シナリオ）
1) 実ネットワーク（Photon）
```text
Action(Exec) → NetworkActionBase → INetworkActionContext.Request
→ PhotonBroadcaster.Raise(DTO) → (Photon) → PhotonReceiver.On → Deserialize DTO
→ NetworkEventApplier.Apply(dto) 
    → DomainEventConverter.ToDomain(dto) → DomainEvent
    → ドメインロジック実行
    → IGameplayEventBus.Publish(DomainEvent) 
→ UI層: IObservable<DomainEvent>.Subscribe().AddTo(this)
```

2) 仮想輸送（VirtualTransport）
```text
Action(Exec) → NetworkActionBase → INetworkActionContext.Request
→ VirtualBroadcaster.Raise(DTO) → 同期配送(順序保証) → VirtualReceiver.On → Deserialize DTO
→ NetworkEventApplier.Apply(dto)
    → DomainEventConverter.ToDomain(dto) → DomainEvent
    → ドメインロジック実行
    → IGameplayEventBus.Publish(DomainEvent)
→ UI層: IObservable<DomainEvent>.Subscribe().AddTo(this)
```

3) ロジック注入（部分再現/チュートリアル）
```text
Test/TutorialScript → DomainEventを直接生成
→ IGameplayEventBus.Publish(DomainEvent) → UI層購読
（注: DTO変換不要、ドメインイベントを直接発行）
```

4) リプレイ（計画のみ）
```text
(記録) Broadcaster/Receiverを横取り → [sequence,time,code,DTO_payload] 保存
(再生) ReplayPlayer → payloadをDeserialize
    → DomainEventConverter.ToDomain(dto) → DomainEvent
    → IGameplayEventBus.Publish(DomainEvent)
```

### インタフェース抜粋（スケッチ）

```csharp
// Adapter Factory
public interface INetworkAdapterFactory {
    INetworkBroadcaster CreateBroadcaster(ISerializer serializer);
    INetworkReceiver CreateReceiver(ISerializer serializer);
}
public sealed class PhotonNetworkAdapterFactory : INetworkAdapterFactory { /* 既存Photon*を返す */ }
public sealed class VirtualNetworkAdapterFactory : INetworkAdapterFactory { /* Virtual*を返す */ }
```

```csharp
// Network Context (Photon依存の抽象化)
public interface INetworkContext {
    bool IsHost { get; }                // ホスト判定
    int LocalActorNumber { get; }       // 自ActorNumber
    bool IsReady { get; }               // 接続状態
    bool IsInRoom { get; }              // ルーム参加状態
}
public sealed class PhotonNetworkContext : INetworkContext { 
    public bool IsHost => PhotonNetwork.IsMasterClient;
    public int LocalActorNumber => PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
    // ...
}
public sealed class VirtualNetworkContext : INetworkContext { 
    public VirtualNetworkContext(int actorNumber, bool isHost) { ... }
    // Virtual環境用の固定値を返す
}
```

```csharp
// Logic Injection
public interface IVirtualLogicFeeder {
    void Emit<T>(T dto); // NetworkEventApplier.Apply(dto)へ委譲
}
```

```csharp
// PlayerId/ActorNumber マッピング（ドメイン層とネットワーク層の分離）
public interface IPlayerIdMapper {
    bool TryGetPlayerId(int actorNumber, out PlayerId playerId);      // ActorNumber → PlayerId
    bool TryGetActorNumber(PlayerId playerId, out int actorNumber);   // PlayerId → ActorNumber
    void Register(PlayerId playerId, int actorNumber);                // マッピング登録
    IReadOnlyList<PlayerId> GetAllPlayerIds();                        // 全PlayerIdを取得
    int[] GetAllActorNumbers();                                       // 全ActorNumberを取得
}

// Photonモード: ActorNumberからシーケンシャルなPlayerIdへ変換
// ApplicationManagerでの生成例:
//   var mapper = new PlayerIdMapper();
//   for (int i = 0; i < actors.Length; i++) {
//       var playerId = new PlayerId(i + 1);  // 1,2,3,4... とシーケンシャルに
//       mapper.Register(playerId, actors[i].ActorNumber);
//   }

// VirtualTransport/リプレイモード: ActorNumberは不要、PlayerIdのみで動作
```

```csharp
// DomainEvent（ドメイン層のイベント、強い型使用）
namespace Tetrage.Core.Events
{
    // 不変クラスベース、強い型を使用
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
        public CardId CardId { get; }          // 強い型
        public PileId FromPileId { get; }      // 強い型
        public PileId ToPileId { get; }        // 強い型
        
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

```csharp
// DomainEventConverter（DTO ⇔ DomainEvent 変換）
namespace Tetrage.Network.Gameplay
{
    public sealed class DomainEventConverter
    {
        private readonly IPlayerIdMapper _mapper;
        
        public DomainEventConverter(IPlayerIdMapper mapper)
        {
            _mapper = mapper;
        }
        
        // DTO → DomainEvent（受信時）
        public Core.Events.TurnStartedEvent ToDomain(TurnStartedDTO dto)
        {
            if (!_mapper.TryGetPlayerId(dto.currentPlayerActorNumber, out var playerId))
                throw new InvalidOperationException($"ActorNumber {dto.currentPlayerActorNumber} が見つかりません");
            return new Core.Events.TurnStartedEvent(dto.sequence, playerId);
        }
        
        // DomainEvent → DTO（送信時）
        public TurnStartedDTO ToDTO(Core.Events.TurnStartedEvent domainEvent)
        {
            if (!_mapper.TryGetActorNumber(domainEvent.CurrentPlayerId, out var actorNumber))
                throw new InvalidOperationException($"PlayerId {domainEvent.CurrentPlayerId} が見つかりません");
            return new TurnStartedDTO { sequence = domainEvent.Sequence, currentPlayerActorNumber = actorNumber };
        }
        
        // CardMovedは変換不要（ActorNumber含まない）
        public Core.Events.CardMovedEvent ToDomain(CardMovedDTO dto) => 
            new Core.Events.CardMovedEvent(dto.sequence, new CardId(dto.cardId), new PileId(dto.fromPileId), new PileId(dto.toPileId));
        
        public CardMovedDTO ToDTO(Core.Events.CardMovedEvent e) => 
            new CardMovedDTO { sequence = e.Sequence, cardId = e.CardId.Value, fromPileId = e.FromPileId.Value, toPileId = e.ToPileId.Value };
    }
}
```

```csharp
// UniRx版 IGameplayEventBus
namespace Tetrage.Core.Events
{
    using UniRx;
    
    public interface IGameplayEventBus
    {
        // IObservableでイベントを配信
        IObservable<TurnStartedEvent> TurnStarted { get; }
        IObservable<CardMovedEvent> CardMoved { get; }
        IObservable<GameEndedEvent> GameEnded { get; }
        // ... 他のイベント
        
        // Publish（内部用）
        void Publish(TurnStartedEvent e);
        void Publish(CardMovedEvent e);
    }
    
    // 実装例
    public sealed class UniRxEventBus : IGameplayEventBus
    {
        private readonly Subject<TurnStartedEvent> _turnStarted = new();
        private readonly Subject<CardMovedEvent> _cardMoved = new();
        
        public IObservable<TurnStartedEvent> TurnStarted => _turnStarted;
        public IObservable<CardMovedEvent> CardMoved => _cardMoved;
        
        public void Publish(TurnStartedEvent e) => _turnStarted.OnNext(e);
        public void Publish(CardMovedEvent e) => _cardMoved.OnNext(e);
    }
}

// UI層での使用例
public class PlayerUIManager : MonoBehaviour
{
    [Inject] private IGameplayEventBus _eventBus;
    
    private void Start()
    {
        // 自動購読解除（GameObjectが破棄されたら自動でDispose）
        _eventBus.TurnStarted
            .Subscribe(e => OnTurnStarted(e))
            .AddTo(this);
        
        // 特定プレイヤーのターンだけ処理
        _eventBus.TurnStarted
            .Where(e => e.CurrentPlayerId == _myPlayerId)
            .Subscribe(_ => ShowMyTurnUI())
            .AddTo(this);
        
        // 複数イベントをマージ
        Observable.Merge(
            _eventBus.TurnStarted.AsUnitObservable(),
            _eventBus.CardMoved.AsUnitObservable()
        ).Subscribe(_ => UpdateUI()).AddTo(this);
    }
}
```

### ボット（方針のみ）
- 目的: ネット経路を維持したままBotを“遠隔プレイヤー”として扱い、テスト容易化
- 構成: `BotAgent`（各Bot1体）、`IBotPolicy`（観測→意思決定）、`IObservation`（DTO/Contextの読み取り専用ビュー）
- 入出力: `IGameplayEventBus`購読で観測、`INetworkActionContext`で`ActionRequestedEvent`送信（`TurnGate`で手番統制）
- 実行場所: Photon時はホスト/クライアント双方可、Virtual時は同一プロセスで複数体可
- 決定性: `SequenceService`と固定Seedで再現可能
- テスト利用: PlayModeで`VirtualNetworkTestHarness`からN体起動し、UI/E2Eを自動化

### テスト支援
- `VirtualNetworkTestHarness`: N人の仮想リモート生成、`NetworkMode`一時切替、シーン/`GameManager`初期化補助
- 例: `ResultUISimpleTest`を仮想輸送/ロジック注入で実行し、UIがDTO適用結果を反映するか検証

### 設計判断: イベントアーキテクチャの選択

#### 検討したアプローチ
**アプローチ1: ドメインイベント分離（採用）**
- NetworkDTOとDomainEventを分離、Boundary層で変換
- 実装コスト: 中（2-3週間）
- PlayerId/ActorNumber分離との統合: 容易
- 段階的移行: 可能

**アプローチ2: EventEnvelope（不採用）**
- DTOを薄い封筒で包むだけ
- 根本的な問題（ActorNumber依存）が解決しない
- メタデータ追加のみで設計改善効果が低い

**アプローチ3: CQRS + Event Sourcing（不採用）**
- Command/Query/Eventの完全分離
- 実装コスト: 大（1-2ヶ月）
- 本プロジェクトの規模・要件に対してオーバーエンジニアリング
- リプレイ/undo機能が必須要件ではない

#### アプローチ1を選択した理由
1. **実装コストと効果のバランス**: イベント数10-20程度の規模に最適
2. **PlayerId分離との相性**: 変換層で自然にActorNumber⇔PlayerId変換が可能
3. **段階的移行**: 既存のNetworkDTOを維持したまま変換層を追加できる
4. **将来の拡張性**: リプレイ実装時も変換層を活用できる
5. **チームの学習コスト**: CQRS/Event Sourcingより理解しやすい

#### 将来的な再検討条件
以下の条件を満たす場合、CQRS導入を再検討する価値がある：
- イベント数が50以上に増加
- リプレイ機能がビジネス要件（配信・大会記録等）になる
- undo/redo機能が必須になる
- チーム全員がCQRSパターンに精通する

### リスク/推奨度
- 依存面の波及（工場注入）: 3/10（変更は`GameplayNetworkController`中心）
- 初期化順（`GameManager`/`ApplicationManager`）: 5/10（ハーネスで統制）
- 将来の遅延/欠損注入の複雑度: 4/10（`VirtualNetworkSettings`で段階的導入）
- PlayerId/ActorNumber分離の波及: 6/10（`NetworkEventApplier`/`Emitter`中心、段階的移行可能）
  - メリット: ドメイン層の独立性向上、VirtualTransport/リプレイ/ローカル対戦でActorNumber不要に
  - 後方互換性: マッピング層追加は既存動作を維持したまま可能
  - 将来の拡張性: 10/10（ネットワーク実装に依存しないドメイン設計）
- **DomainEvent + UniRx導入の波及**: 7/10（イベント購読箇所の全面改修が必要）
  - メリット: 強い型使用、メモリリーク防止、宣言的なイベント処理
  - 実装コスト: 中（2-3週間、イベント定義+変換層+UI層移行）
  - 学習コスト: 中（UniRxの基本理解が必要）
  - 後方互換性: 既存NetworkDTOを維持、変換層追加で段階的移行可能
  - 将来の拡張性: 10/10（DTO→DomainEvent変換でリプレイ/CQRS移行も容易）
- 推奨度: 9/10（PlayModeでの回帰と速度の両立に有効、将来の拡張性も確保）

### 実装手順（段階導入）
1. `INetworkContext`を追加（Photon依存の抽象化層）
   - `PhotonNetworkContext` / `VirtualNetworkContext` 実装
   - `Dealer` / `GameManager` / `ApplicationManager` に注入
2. `IPlayerIdMapper`を追加（PlayerId/ActorNumber分離）
   - `PlayerIdMapper`実装
   - `ApplicationManager`でPlayerInfo生成時にマッピング生成
   - `NetworkEventApplier`/`GameLifecycleEmitter`に注入（変換層確立）
   - 段階的移行: 初期はマッピング追加のみ、既存のActorNumber直接使用は併用
2.5. **DomainEvent + UniRxEventBus導入**（イベント再設計）
   - **Phase 1**: DomainEvent定義（`Tetrage.Core.Events`名前空間）
     - `TurnStartedEvent`, `CardMovedEvent`等のクラス定義
     - 強い型（PlayerId, CardId等）を使用
   - **Phase 2**: `DomainEventConverter`実装
     - `IPlayerIdMapper`を使用してDTO ⇔ DomainEvent変換
   - **Phase 3**: `UniRxEventBus`実装
     - `IGameplayEventBus`をUniRx版に置き換え
     - `IObservable<T>`でイベント配信
   - **Phase 4**: `NetworkEventApplier`改修
     - DTOをDomainEventに変換してから処理
     - DomainEventを`IGameplayEventBus`へPublish
   - **Phase 5**: UI層の移行
     - `AddTo()`で自動購読解除、Where/Select等のRx演算子活用
   - 段階的移行: 既存のNetworkDTOは転送層として維持、変換層を追加
3. `INetworkAdapterFactory`を追加（Photon/Virtual切替点の確立）
4. `GameplayNetworkController`を工場利用に変更
5. `VirtualBroadcaster/VirtualReceiver`と`VirtualNetworkSettings`を追加（即時配送）
6. `VirtualNetworkTestHarness`を追加（PlayMode一時切替）
7. `IVirtualLogicFeeder`を追加（部分再現）
8. `ApplicationManager`に`NetworkMode`切替を導入
9. 代表テスト（`ResultUISimpleTest`等）を仮想輸送/ロジック注入で通す
10. （将来計画）リプレイ/スナップショット、ボット実装

### 追加検討事項（実装時の注意点）

#### 1. **TurnGateのPlayerId対応**
- **現状**: `TurnGate.Release(int actorNumber)`でActorNumberを使用
- **対応**: `IPlayerIdMapper`経由でPlayerId→ActorNumber変換を実装
- **影響箇所**: `NetworkEventApplier.Apply(TurnStartedEvent)`
- **リスク**: 低（変換層追加のみ）

#### 2. **乱数シード固定機構**
- **現状**: `RealDealerPlanner`で`UnityEngine.Random.Range()`を直接使用
- **対応**: テストモード時に固定シードを設定する`IRandomProvider`導入を推奨
  - `VirtualNetworkTestHarness`で`Random.InitState(fixedSeed)`を実行
  - ボット/リプレイの決定性保証に必須
- **優先度**: 中（ボット実装時に必須）

#### 3. **入力制御とUIロック**
- **現状**: UI層は個別にイベント購読しているが、入力ロック機構が明示的でない
- **対応**: `IInputController`インターフェース追加を推奨
  - テストモード/リプレイ再生時の入力無効化
  - チュートリアルでの段階的な入力許可
- **実装例**: `InputController.SetInputEnabled(bool)`
- **優先度**: 中（チュートリアル実装時に必須）

#### 4. **リプレイデータの永続化フォーマット**
- **計画のみ**: 具体的なデータ構造が未定義
- **推奨設計**:
  ```csharp
  public sealed class ReplayData
  {
      public int FormatVersion;  // 互換性管理
      public DateTime RecordedAt;
      public PlayerInfo[] Players;  // PlayerId含む
      public ReplayEventEntry[] Events;
  }
  public struct ReplayEventEntry
  {
      public float Timestamp;  // 相対時間
      public EventCode Code;
      public byte[] Payload;  // DTO serialized
  }
  ```
- **保存形式**: JSON（可読性重視）またはMessagePack（サイズ重視）
- **優先度**: 低（リプレイ実装時に具体化）

#### 5. **Photon RoomプロパティとVirtualTransportの互換性**
- **現状分析**: Roomプロパティへの重度依存は確認されず（プレイヤー名設定程度）
- **注意点**: 今後Room/Playerプロパティを使用する場合、`INetworkContext`へ抽象化を追加
- **リスク**: 低（現時点では問題なし）

#### 6. **ボットのObservation設計（詳細化）**
- **方針のみ**: `IObservation`が抽象的
- **推奨I/F**:
  ```csharp
  public interface IGameObservation
  {
      PlayerId CurrentPlayerId { get; }
      IReadOnlyList<ObservableCard> MyHand { get; }
      IReadOnlyList<ObservableCard> Field { get; }
      int OpponentHandCount(PlayerId opponentId);
      // ... 他の観測可能情報
  }
  ```
- **観測制限**: プレイヤー視点で見える情報のみ提供（完全情報を与えない）
- **優先度**: 中（ボット実装時に具体化）

#### 7. **NetworkMode切り替えのロック機構（詳細化）**
- **不安点**: 「切り替えの安全性」について懸念あり
- **推奨設計**:
  - **起動時決定**: `ApplicationManager`起動引数でモード固定
  - **シーン遷移時のみ変更可**: `GameScene`ロード前なら許可
  - **テスト専用切り替え**: `VirtualNetworkTestHarness.TemporarySwitch(mode, restoreOnDispose: true)`
  - **ロック機構**: `GameManager.Initialize()`後は変更不可
- **実装例**:
  ```csharp
  public sealed class NetworkModeController
  {
      private NetworkMode _currentMode;
      private bool _locked;
      public void LockMode() => _locked = true;
      public bool TrySetMode(NetworkMode mode) => !_locked && SetModeInternal(mode);
  }
  ```
- **優先度**: 高（Phase 3で実装）

#### 8. **UniRx購読のライフサイクル管理**
- **注意点**: MonoBehaviour破棄時の自動解除が正しく動作するか検証必要
- **推奨パターン**:
  ```csharp
  private CompositeDisposable _disposables = new();
  void Start() {
      _eventBus.TurnStarted.Subscribe(OnTurnStarted).AddTo(_disposables);
  }
  void OnDestroy() {
      _disposables?.Dispose();
  }
  ```
- **優先度**: 高（UniRx導入時に必須）

#### 9. **チュートリアルの段階的イベント注入**
- **現状**: `IVirtualLogicFeeder`でDTO注入のみ計画
- **推奨拡張**: `ITutorialScenario`インターフェース
  ```csharp
  public interface ITutorialScenario
  {
      UniTask ExecuteAsync(ITutorialContext context);
  }
  public interface ITutorialContext
  {
      UniTask WaitForUserAction(ActionType allowedType);
      void InjectEvent(DomainEvent e);
      void ShowGuidance(string message);
  }
  ```
- **優先度**: 低（チュートリアル実装時に具体化）

#### 10. **VirtualTransportのActorNumber管理**
- **計画あり**: `VirtualTransportHub`の概念はあるが詳細不明
- **推奨設計**:
  ```csharp
  public sealed class VirtualTransportHub
  {
      private int _nextActorNumber = 1;
      private Dictionary<int, VirtualPeer> _peers = new();
      public int RegisterPeer() => _nextActorNumber++;
      public void RouteMessage(int fromActor, int toActor, byte[] payload);
  }
  ```
- **ActorNumber割り当て**: VirtualTransport時は1,2,3...と順番に割り当て
- **優先度**: 中（Phase 5で実装）

### 優先度まとめ

| 項目 | 優先度 | フェーズ |
|------|--------|----------|
| TurnGateのPlayerId対応 | 高 | Phase 2 |
| NetworkMode切り替えロック | 高 | Phase 3 |
| UniRx購読ライフサイクル | 高 | Phase 2.5 |
| 入力制御/UIロック | 中 | Phase 6 |
| 乱数シード固定 | 中 | Phase 6 |
| VirtualTransportHub | 中 | Phase 5 |
| ボットObservation | 中 | Phase 10 |
| リプレイデータ構造 | 低 | Phase 10 |
| チュートリアルScenario | 低 | 将来計画 |
| Photon互換性 | 低 | 現状問題なし |

### 保存場所
- `Assets/Main/Scripts/Tetrage/Network/Docs/PlayModeVirtual_RemotePlayers.md`


