# テトラージュ ゲーム設計書

## クラス設計

### 1. GameManager（ゲームの進行・フェーズ管理）
- **メンバ**
  - 現在のゲームフェーズ（Starting, Playing, Ending）
  - Playerのリスト
  - Dealerのインスタンス
- **メソッド**
  - StartGame()：ゲーム開始（Startingフェーズ移行）
  - StartRound()：ラウンド開始
  - EndRound()：ラウンド終了処理（Endingフェーズ移行）
  - TransitionPhase()：フェーズ切り替え
  - DisplayResults()：ゲーム結果表示
  - RestartOrQuit()：再プレイ選択

### 2. Dealer（ターン進行管理・判定責任者）
- **メンバ**
  - Stageのインスタンス
  - 現在ターンを持つPlayerの参照
- **メソッド**
  - DistributeCards()：ターゲットカードの配布
  - DecideFirstPlayer()：最初のプレイヤー決定
  - NextTurn()：次のターンに移行
  - JudgeTetrage(actionType)：テトラージュアクションの判定

### 3. Player（プレイヤー情報・アクション実行者）
- **メンバ**
  - PlayerID
  - target（カード1枚）
  - hands（最大3枚）
  - tmp（一時保持カード、最大2枚）
- **メソッド（アクション実行要請）**
  - PerformAction(Action action)：アクション実行を要請

### 4. Card（カード情報）
- **メンバ**
  - suit（スート情報）
  - identifier（識別番号、同一スート内での区別用）
  - isVisible（表向きか裏向きか）
- **メソッド**
  - Flip()：カードをひっくり返す

### 5. Stage（カード配置管理）
- **メンバ**
  - stack（山札、順序付きリスト）
  - trash（墓地、順序付きリスト）
- **メソッド**
  - DrawFromStack()：山札からカードを取り出す
  - Discard(card)：墓地へカードを捨てる
  - PeekTrash()：墓地のカードを見る（表面のみ）

## Actionクラス群（Commandパターンの応用）

### Action（抽象クラス）
- **メソッド**
  - execute()
  - validate()

### DrawAction
- **処理概要**
  - 山札から2枚ドロー、一時領域に保持
  - プレイヤーが採用と墓地送りのカードを選択（カード表裏も選択）
  - ターン終了処理

### OpenAction
- **処理概要**
  - 対象プレイヤーの裏向きカードを1枚表向きに変更
  - ターン終了処理

### CheckAction
- **処理概要**
  - 自プレイヤーの手札が3枚同一スートであることを検証
  - 他プレイヤーのターゲットカードのスートを問い合わせ
  - 一致したらターゲットカード公開
  - ターン終了処理

### TetrageAction
- **処理概要**
  - Tetrage Solo
    - 自手札3枚＋自ターゲットカードのスート一致確認
    - 提出と同時にDealerへ判定を委譲
  - Tetrage Multi
    - 親が同一チームのプレイヤーを指名
    - 指名者がターゲットカード公開、全員が条件を満たせば成功
    - Dealerに勝敗判定を委譲
- **実装注意点**
  - 各アクションはvalidateを行い、条件不成立時はエラー処理またはリトライ促す

## クラス間の関係性
- **GameManager**がゲーム全体の進行とフェーズ管理を行い、実際のゲームプレイ進行は**Dealer**に委譲する。
- **Dealer**はターンの進行・勝敗判定を管理。
- 各**Player**は自身のアクションをActionクラスを介して実行。
- **Stage**はカードを保持し、PlayerやDealerからのアクセスによってカードの出し入れを行う。
- **Card**はカード状態管理を担当。

この構造により、明確で保守性が高く、拡張性にも優れた設計が実現されます。

## クラス間の関係性と階層

以下のような形でクラス間の関係性と所有・継承の階層を整理できます。

```text
GameManager
├─ owns → Dealer
├─ aggregates → List<Player>
└─ orchestrates game flow

Dealer
├─ aggregates → Stage
├─ references → current Player
├─ invokes → Player.PerformAction(Action)
└─ delegates → Dealer.JudgeTetrage()

Player
├─ aggregates → target: Card
├─ aggregates → hands: List<Card>
├─ aggregates → tmp: List<Card>
├─ sends → Action (via Player.PerformAction)
└─ calls → Stage / Card メソッド in execute()

Stage
├─ aggregates → stack: List<Card>
├─ aggregates → trash: List<Card>
└─ provides → DrawFromStack(), Discard(), PeekTrash()

Card
└─ standalone value object
   ├─ suit, identifier, isVisible
   └─ Flip()

Action (抽象)
├─ DrawAction
├─ OpenAction
├─ CheckAction
└─ TetrageAction
```

### 所有・集約・参照・継承

- **Composition（強い所有）**
  - GameManager → Dealer
  - Dealer → Stage

- **Aggregation（集約）**
  - GameManager → 複数のPlayerリスト
  - Player → target, hands, tmp のCardリスト
  - Stage → stack, trash のCardリスト

- **Association（参照）**
  - Dealer → 現在のPlayer
  - Player → Stage（引数として受け取り）
  - Player → Action（実行要求）

- **Inheritance（継承）**
  - Action ←─ DrawAction, OpenAction, CheckAction, TetrageAction

この構造により、
- 責務の明確化
- 変更影響範囲の最小化
- 拡張性の確保

が実現します。

