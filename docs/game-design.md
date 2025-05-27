# テトラージュ ゲーム設計書
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

### ActionType
```csharp
public enum ActionType {
    Draw,
    Open,
    Check,
    TetrageSolo,
    TetrageMulti
}
```

## クラス設計

### 1. GameManager（ゲームの進行・フェーズ管理）
- **メンバ**
  - `private GamePhase _currentPhase;`
- **プロパティ**
  ```csharp
  public GamePhase CurrentPhase {
      get => _currentPhase;
      set => _currentPhase = value;
  }
  ```
- **メソッド**
  - `void StartGame()`：ゲーム開始処理
  - `void StartRound()`：ラウンド開始処理
  - `void EndRound()`：ラウンド終了処理
  - `void TransitionPhase()`：フェーズ切替処理
  - `void DisplayResults()`：結果表示処理
  - `void RestartOrQuit()`：再プレイまたは終了

### 2. Dealer（ターン進行管理・判定責任者）
- **メンバ**
  - `private CardPile _stack;` // 山札
  - `private CardPile _trash;` // 捨て札
  - `private Player _currentPlayer;` // 現在のプレイヤー
- **プロパティ**
  ```csharp
  public CardPile Stack => _stack;
  public CardPile Trash => _trash;
  public Player CurrentPlayer => _currentPlayer;
  ```
- **メソッド**
  - `void DistributeCards()`：対象プレイヤーにカード配布
  - `void DecideFirstPlayer()`：親プレイヤー決定
  - `void NextTurn()`：次のターンへ
  - `void JudgeTetrage(ActionType actionType)`：テトラージュ判定

### 3. Player（プレイヤー情報・アクション実行者）
- **メンバ**
  - `public string PlayerID;`
  - `public CardPile Hands;` // 手札
  - `public CardPile Tmp;`   // 一時保持
  - `public Card Target;`    // ターゲットカード
- **メソッド**
  - `void PerformAction(Action action)`：アクション実行管理

### 4. Card（カード情報）
- **メンバ**
  - `public Suit suit = Suit.Spade;`
  - `private int _number;`
  - `public int Number {
      get => _number;
      set => _number = Mathf.Max(1, value);
  }
  `
  - `public bool isVisible;`
- **メソッド**
  - `void Flip()`：表裏反転
  - `void Start()`, `void Update()`: MonoBehaviour ライフサイクル

### 5. CardPile（カード束共通クラス）
- **メンバ**
  - `private readonly List<Card> _cards;`
  - `private readonly int _maxCount;`
  - `public string Name;`
  - `public CardOwner OwnerType;`
- **コンストラクタ**
  ```csharp
  public CardPile(string name, CardOwner ownerType = CardOwner.Null, int maxCount = int.MaxValue)
  ```
- **メソッド**
  - `bool Add(Card card)`
  - `bool Remove(Card card)`
  - `bool TransferTo(CardPile target, Card card)`
  - `void Shuffle()`
  - `List<Card> Draw(int count)`
  - `IReadOnlyList<Card> Peek(int count)`
  - `int Count { get; }`

### 6. Stage（カード配置管理）
- **メンバ**
  - `private CardPile _stack;`
  - `private CardPile _trash;`
  - `private Transform stackContainer, trashContainer;`
- **プロパティ**
  ```csharp
  public CardPile Stack => _stack;
  public CardPile Trash => _trash;
  ```
- **メソッド**
  - `Card DrawFromStack()`：
    ```csharp
    var card = _stack.Draw(1).FirstOrDefault();
    if (card != null) card.transform.SetParent(stackContainer);
    return card;
    ```
  - `void Discard(Card card)`：
    ```csharp
    _trash.Add(card);
    card.transform.SetParent(trashContainer);
    ```
  - `IReadOnlyList<Card> PeekTrash(int count) => _trash.Peek(count);`

## Actionクラス群（Commandパターンの応用）

### Action（抽象クラス）
- **仕様**
  - 純粋な C# クラス（`MonoBehaviour` を継承せず、ゲームロジックのみを保持）
  - Unity コンポーネントとしてアタッチしない
  - 実行主体は `Player`
  - `Action` は必ず `Player` をコンストラクタで受け取り、内部フィールド `_player` に保持

- **メソッド**
  ```csharp
  public abstract bool Validate();  // 実行可否判定
  public abstract void Execute();   // 実行処理
  ```

- **実行フロー**（`Player.PerformAction` 内で一元管理）
  1. `Validate()` を呼び、失敗時は共通ハンドリング（ログなど）
  2. 成功時に `Execute()` を呼び出す

- **UI 連動**
  - ボタンの `interactable = action.Validate()` で押下可能／不可を制御
  - ボタン押下時にも再チェックし、通れば `Execute()`（または `PerformAction`）を実行

- **コンテキスト管理**
  - 必要なゲーム状態（手札内容やターゲットカードの可視状態など）はすべて `Player` が保持
  - `Action` は `_player` 経由で参照するだけ

- **拡張性・テスト性**
  - 新しいアクションは `class NewAction : Action` を作り、`Validate()`／`Execute()` を実装するだけ
  - `Player` モックを渡すことでユニットテストが容易

### DrawAction
- **処理概要**：山札から2枚ドロー→一時保持→ターン終了

### OpenAction
- **処理概要**：他プレイヤーの裏向きカードを1枚オープン

### CheckAction
- **処理概要**：手札同一スート検証→ターゲットカード公開

### TetrageAction
- **処理概要**：ソロ／マルチ判定後、Dealerへ委譲

## クラス間の関係性
```text
GameManager
├─ composition → Dealer
├─ aggregation → List<Player>
└─ orchestrates game flow

Dealer
├─ composition → Stage
├─ reference → CurrentPlayer
└─ uses → CardPile

Player
├─ aggregation → Hands, Tmp
├─ association → Target
└─ method → PerformAction(Action)

Stage
├─ aggregation → Stack, Trash (CardPile)
└─ methods → DrawFromStack(), Discard(), PeekTrash()

CardPile
└─ standalone model for pile logic

Card
└─ value object

Action ← DrawAction, OpenAction, CheckAction, TetrageAction
```

- **Composition**: GameManager→Dealer, Dealer→Stage
- **Aggregation**: GameManager→Players, Player→CardPile, Stage→CardPile
- **Association**: Dealer→Player, Player→Action
- **Inheritance**: Action ← sub classes

## テトラージュの確率について
スプレッドシートのURL:https://docs.google.com/spreadsheets/d/1lhUMp0F4Xd1jotfpcBv1PmrUZzLDEmI0cZEIOKkUO3U/edit?usp=sharing
コラボラトリーのURL:https://colab.research.google.com/drive/1_4t_iobtMdNSSfsiINBte_dHl0AfvhFr?usp=sharing