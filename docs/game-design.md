# テトラージュ ゲーム設計書 (実装反映版)

## 列挙型 (Enums)

### GamePhase

```csharp
public enum GamePhase {
    Starting, // 初期準備フェーズ
    Playing,  // 実際のプレイフェーズ
    Ending    // 勝敗判定・結果表示フェーズ
}
```

### Suit

```csharp
public enum Suit {
    Spade,
    Heart,
    Diamond,
    Club
}
```

## クラス設計

### 1. GameManager（ゲームの進行・フェーズ管理）

- **メンバ**
  - `private GamePhase _currentPhase` （シリアライズ可）
- **プロパティ**
  ```csharp
  public GamePhase CurrentPhase {
      get => _currentPhase;
      set => _currentPhase = value;
  }
  ```
- **メソッド**
  - `void StartGame()`：ゲーム開始処理（未実装）
  - `void StartRound()`：ラウンド開始処理（未実装）
  - `void EndRound()`：ラウンド終了処理（未実装）
  - `void TransitionPhase()`：フェーズ切り替え処理（未実装）
  - `void DisplayResults()`：結果表示処理（未実装）
  - `void RestartOrQuit()`：再プレイ／終了選択処理（未実装）

### 2. Dealer（ターン進行管理・判定責任者）

- **メンバ**
  - `private CardPile _stack`：山札（内部操作は CardPile に委譲）
  - `private CardPile _trash`：捨て札（内部操作は CardPile に委譲）
  - `private Player _currentPlayer`：現在ターンのプレイヤー参照
- **メソッド**
  - `void DistributeCards()`：ターゲットカードの配布
  - `void DecideFirstPlayer()`：最初のプレイヤー決定
  - `void NextTurn()`：次のターンに移行
  - `void JudgeTetrage(ActionType actionType)`：テトラージュ判定委譲

### 3. Player（プレイヤー情報・アクション実行者）

- **メンバ**
  - `public string PlayerID`
  - `public CardPile Hands`：手札（最大枚数はコンストラクタ定義による）
  - `public CardPile Tmp`：一時保持カード（最大枚数はコンストラクタ定義による）
  - `public Card Target`：ターゲットカード（1枚）
- **メソッド**
  - `void PerformAction(Action action)`：アクション実行を要請

### 4. Card（カード情報）

- **メンバ**
  - `public Suit suit = Suit.Spade`：カードのスート（初期値 Spade）
  - `private int _number`：内部保持用番号
  - `public int Number {
        get => _number;
        set => _number = Mathf.Max(1, value);
    }`：番号プロパティ（設定時に1以上を保証）
  - `public bool isVisible`：表裏状態
- **メソッド**
  - `void Start()`：初期化（空実装）
  - `void Update()`：毎フレーム更新（空実装）

### 5. CardPile（カード束共通クラス）

- **目的・背景**
  - プレイヤーの手札、山札、捨て札など、複数のカード束で共通する操作（追加・削除・シャッフル・ドロー・転送・可視化）をまとめる
  - 単一責任原則に従い、カード束固有のロジックを `CardPile` に集約し、`Player` や `Stage` はゲーム進行に専念
  - `maxCount` による枚数上限管理でルール違反を防止し、実装の堅牢性を向上
- **メンバ**
  - `private readonly List<Card> _cards`：内部カードリスト
  - `private readonly int _maxCount`：最大枚数（コンストラクタで指定）
  - `public string Name`：束の名称
  - `public CardOwner Owner`Type：所有者プレイヤー（null なら共有束）
- **コンストラクタ**
  ```csharp
  public CardPile(string name, CardOwner ownerType = CardOwner.Null, int maxCount = int.MaxValue)
  ```
- **メソッド**
  - `bool Add(Card card)`：上限チェック付き追加（超過時は false）
  - `bool Remove(Card card)`：カード削除
  - `bool TransferTo(CardPile target, Card card)`：他束への移動
  - `void Shuffle()`：シャッフル
  - `List<Card> Draw(int count)`：先頭から count 枚ドロー
  - `IReadOnlyList<Card> Peek(int count)`：先頭から count 枚を閲覧
  - `int Count { get; }`：現在の枚数

### 6. Stage（カード配置管理）

- **メンバ**
  - `private CardPile _stack`：山札
  - `private CardPile _trash`：捨て札
- **メソッド**
  - `Card DrawFromStack()`：山札から1枚ドロー
  - `void Discard(Card card)`：捨て札に追加
  - `IReadOnlyList<Card> PeekTrash(int count)`：捨て札の先頭 count 枚を確認

## Actionクラス群（Commandパターンの応用）

> **Note:** `Action` クラス群は `MonoBehaviour` を継承せず、純粋なドメインロジックとして実装します。

### Action（抽象クラス）

- **メソッド**
  - `abstract bool Validate(GameState state)`
  - `abstract void Execute(GameState state)`

### DrawAction, OpenAction, CheckAction, TetrageAction

- 各アクションの処理概要は従来通り。

## クラス間の関係性

```text
GameManager
├─ composition → Dealer
├─ aggregation → List<Player>
└─ orchestrates game flow

Dealer
├─ composition → Stage
├─ reference → current Player
└─ uses → CardPile for stack/trash

Player
├─ aggregation → Cards via CardPile (Hands, Tmp)
├─ association → Target: Card
└─ sends → Action via PerformAction()

Stage
├─ aggregation → CardPile (_stack, _trash)
└─ provides → DrawFromStack(), Discard(), PeekTrash()

CardPile
└─ standalone domain model (カード束ロジック一括管理)

Card
└─ basic value object (suit, Number, isVisible)

Action ← DrawAction, OpenAction, CheckAction, TetrageAction
```

- **Composition**: GameManager→Dealer, Dealer→Stage
- **Aggregation**: GameManager→Player, Player→CardPile, Stage→CardPile
- **Reference**: Dealer→Player, Player/Dealer→Action
- **Inheritance**: Action ← 各具体クラス

---

