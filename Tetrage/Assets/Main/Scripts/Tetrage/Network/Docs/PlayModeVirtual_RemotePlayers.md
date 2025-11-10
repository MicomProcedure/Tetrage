## PlayMode仮想リモート/ローカル/リプレイ対応計画

### 背景と方針
- NetworkDTOは続投（`Assets/Main/Scripts/Tetrage/Network/Gameplay/NetworkDTO.cs`）。NetworkEventのような低レベル封筒型は新設しない。
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
  - `VirtualBroadcaster` / `VirtualReceiver`: 同一プロセス内で即時配送・順序保証
  - `VirtualNetworkSettings`: 将来の遅延/欠損/乱数注入フック
  - `IVirtualLogicFeeder`: DTOを直接`NetworkEventApplier`へ投入（部分再現/チュートリアル/リプレイ再生に使用）
  - `VirtualNetworkTestHarness`: PlayModeで仮想Peer生成・NetworkModeの一時切替ユーティリティ
  - `NetworkMode`（`RealPhoton`/`VirtualTransport`/`LogicInjection`/`LocalVsBot`）: `ApplicationManager`経由で起動切替

### 変更点（主なクラス）
- `GameplayNetworkController`: `INetworkAdapterFactory`を受け取り、Broadcaster/Receiverを工場から生成
- `PhotonAdapter`系: 既存実装は維持。`PhotonNetworkAdapterFactory`から返す役割に整理
- `ApplicationManager`: `NetworkMode`の設定/起動切替を提供
- `GameManager`: `InitializeNetworking()`で、モードに応じた工場を注入
- `Dealer`: 送出はDTO＋`DealerPlanEmitter`/`GameLifecycleEmitter`、適用は`NetworkEventApplier`で統一

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

Receiver -> DTO -> NetworkEventApplier -> IGameplayEventBus -> UI/Bot/Systems
                                             ^                 ^
                                             |                 |
                                  IVirtualLogicFeeder      BotAgent (IBotPolicy,
                                                          INetworkActionContext)

ReplayRecorder(計画)は Broadcaster & Receiver を傍受、ReplayPlayer(計画)は
IVirtualLogicFeederでDTOを適用。
```

### フロー（代表シナリオ）
1) 実ネットワーク（Photon）
```text
Action(Exec) → NetworkActionBase → INetworkActionContext.Request
→ PhotonBroadcaster.Raise → (Photon) → PhotonReceiver.On → Deserialize DTO
→ NetworkEventApplier.Apply(dto) → IGameplayEventBus.Publish → UI/Bot
```

2) 仮想輸送（VirtualTransport）
```text
Action(Exec) → NetworkActionBase → INetworkActionContext.Request
→ VirtualBroadcaster.Raise → 同期配送(順序保証) → VirtualReceiver.On → Deserialize DTO
→ NetworkEventApplier.Apply(dto) → IGameplayEventBus.Publish → UI/Bot
```

3) ロジック注入（部分再現/チュートリアル）
```text
Test/TutorialScript → IVirtualLogicFeeder.Emit(dto)
→ NetworkEventApplier.Apply(dto) → IGameplayEventBus.Publish → UI
```

4) リプレイ（計画のみ）
```text
(記録) Broadcaster/Receiverを横取り → [sequence,time,code,payload] 保存
(再生) ReplayPlayer → payloadをDeserialize → IVirtualLogicFeeder.Emit(dto)
→ NetworkEventApplier.Apply(dto) → IGameplayEventBus.Publish
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
// Logic Injection
public interface IVirtualLogicFeeder {
    void Emit<T>(T dto); // NetworkEventApplier.Apply(dto)へ委譲
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

### リスク/推奨度
- 依存面の波及（工場注入）: 3/10（変更は`GameplayNetworkController`中心）
- 初期化順（`GameManager`/`ApplicationManager`）: 5/10（ハーネスで統制）
- 将来の遅延/欠損注入の複雑度: 4/10（`VirtualNetworkSettings`で段階的導入）
- 推奨度: 9/10（PlayModeでの回帰と速度の両立に有効）

### 実装手順（段階導入）
1. `INetworkAdapterFactory`を追加（Photon/Virtual切替点の確立）
2. `GameplayNetworkController`を工場利用に変更
3. `VirtualBroadcaster/VirtualReceiver`と`VirtualNetworkSettings`を追加（即時配送）
4. `VirtualNetworkTestHarness`を追加（PlayMode一時切替）
5. `IVirtualLogicFeeder`を追加（部分再現）
6. `ApplicationManager`に`NetworkMode`切替を導入
7. 代表テスト（`ResultUISimpleTest`等）を仮想輸送/ロジック注入で通す
8. （将来計画）リプレイ/スナップショット、ボット実装

### 保存場所
- `Assets/Main/Scripts/Tetrage/Network/Docs/PlayModeVirtual_RemotePlayers.md`


