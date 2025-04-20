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
  - `private GamePhase currentPhase` （シリアライズ可）
- **プロパティ**
  - `public GamePhase CurrentPhase { get; set; }`  // 現在のフェーズを取得/設定
- **メソッド**
  - `void StartGame()`：ゲーム開始処理（未実装）
  - `void StartRound()`：ラウンド開始処理（未実装）
  - `void EndRound()`：ラウンド終了処理（未実装）
  - `void TransitionPhase()`：フェーズ切り替え処理（未実装）
  - `void DisplayResults()`：結果表示処理（未実装）
  - `void RestartOrQuit()`：再プレイ／終了選択処理（未実装）

### 2. Dealer（ターン進行管理・判定責任者）
- **メンバ**
  - `Stage stage`：カード配置管理インスタンス
  - `Player currentPlayer`：現在ターンのプレイヤー参照
- **メソッド**
  - `void DistributeCards()`：ターゲットカードの配布
  - `void DecideFirstPlayer()`：最初のプレイヤー決定
  - `void NextTurn()`：次のターンに移行
  - `void JudgeTetrage(ActionType actionType)`：テトラージュ判定委譲

### 3. Player（プレイヤー情報・アクション実行者）
- **メンバ**
  - `string PlayerID`
  - `Card target`：ターゲットカード（1枚）
  - `List<Card> hands`：手札（最大3枚）
  - `List<Card> tmp`：一時保持カード（最大2枚）
- **メソッド**
  - `void PerformAction(Action action)`：アクション実行を要請

### 4. Card（カード情報）
- **メンバ**
  - `public Suit suit = Suit.Spade`：カードのスート（初期値 Spade）
  - `private int _Number`：内部保持用番号
  - `public int Number { get; set; }`：番号プロパティ（設定時に1以上を保証）
  - `public bool isVisible`：表裏状態
- **メソッド**
  - `void Start()`：初期化（空実装）
  - `void Update()`：毎フレーム更新（空実装）

### 5. Stage（カード配置管理）
- **メンバ**
  - `List<Card> stack`：山札（順序付きリスト）
  - `List<Card> trash`：墓地（順序付きリスト）
- **メソッド**
  - `Card DrawFromStack()`：山札からカードを取り出す
  - `void Discard(Card card)`：墓地へカードを捨てる
  - `Card PeekTrash()`：墓地のカードを表面のみ確認

## Actionクラス群（Commandパターンの応用）

### Action（抽象クラス）
- **メソッド**
  - `abstract void execute()`
  - `abstract bool validate()`

### DrawAction
- **処理概要**
  - 山札から2枚ドローし、一時領域に保持
  - プレイヤーが採用と墓地送りのカードを選択（カードの表裏も選択）
  - ターン終了処理

### OpenAction
- **処理概要**
  - 対象プレイヤーの裏向きカードを1枚表向きに変更
  - ターン終了処理

### CheckAction
- **処理概要**
  - 自プレイヤーの手札が3枚同一スートか検証
  - 他プレイヤーのターゲットカードのスートを問い合わせ
  - 一致したらターゲットカード公開
  - ターン終了処理

### TetrageAction
- **処理概要**
  - ソロ：自手札3枚＋自ターゲットカードのスート一致確認後、Dealerへ判定委譲
  - マルチ：親が同一チームのプレイヤーを指名し、指名者がターゲットカード公開、全員が条件を満たせば成功、Dealerに判定委譲
- **実装注意**
  - 各アクションは `validate()` で前提をチェックし、失敗時はエラー処理またはリトライを促す

## クラス間の関係性

- **GameManager** が全体進行とフェーズ管理を担当し、実際のゲームフローは **Dealer** に委譲
- **Dealer** がターン進行および勝敗判定を管理
- **Player** は自身のアクションを **Action** クラスを通じて実行
- **Stage** はカードの保管・配置管理を担当し、**Player**/ **Dealer** から呼び出される
- **Card** はカード状態の保持と更新を担う

## 所有・集約・参照・継承

```text
GameManager
├─ composition → Dealer
├─ aggregation → List<Player>
└─ orchestrates game flow

Dealer
├─ composition → Stage
├─ reference → current Player
└─ delegates → JudgeTetrage()

Player
├─ aggregation → target: Card
├─ aggregation → hands/tmp: List<Card>
└─ sends → Action via PerformAction()

Stage
├─ aggregation → stack/trash: List<Card>
└─ provides → DrawFromStack(), Discard(), PeekTrash()

Card
└─ standalone value object (suit, Number, isVisible)

Action ← DrawAction, OpenAction, CheckAction, TetrageAction
```

- **Composition** (強い所有): GameManager→Dealer, Dealer→Stage
- **Aggregation**: GameManager→Player リスト, Player→Card リスト, Stage→Card リスト
- **Reference**: Dealer→Player, Player/Dealer→Stage, Player→Action
- **Inheritance**: Action ← 各具体クラス


