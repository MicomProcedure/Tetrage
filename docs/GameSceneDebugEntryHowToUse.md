# GameSceneDebugEntry 使い方ガイド

## 概要

`GameSceneDebugEntry`は、UnityのMultiPlayModeで4人プレイのテストを簡単に行うためのデバッグツールです。PUN2のルーム作成、プレイヤー管理、初期カード配置、ターン順設定などをInspector上で制御できます。

## セットアップ

### 1. GameObjectの作成

1. GameSceneに空のGameObjectを作成
2. 名前を「GameSceneDebugEntry」に変更
3. `GameSceneDebugEntry.cs`スクリプトをアタッチ
4. `PhotonView`コンポーネントを追加
   - Add Component → "PhotonView"で検索
   - View IDは自動割り当てでOK

### 2. Inspector設定

#### 必須設定
- **Game Manager**: GameManagerの参照を設定

#### 基本設定
```
[Debug Settings]
Debug Room Code: "DEBUG"  (デバッグ用ルーム名)
Max Players: 4  (最大プレイヤー数)
Auto Start Game: ✅  (自動でゲーム開始)
Wait For Players Timeout: 10  (プレイヤー待機タイムアウト)
Fixed Region: "jp"  (固定リージョン)
Connection Timeout: 15  (接続タイムアウト)
```

## 機能一覧

### 🎮 **機能1: MultiPlayMode対応**

#### 動作
- GameSceneを開くだけで自動的にPUN2ルームに接続
- 各インスタンスが同じ"DEBUG"ルームに参加
- 4人揃うまで待機（タイムアウト: 10秒）
- ホストが自動的にゲームを開始

#### 設定
特別な設定は不要。MultiPlayModeを有効にしてPlayするだけ。

---

### 👥 **機能2: プレイヤー情報のカスタマイズ**

#### 設定方法

```
[Player Debug Settings]
✅ Use Debug Player Info

Debug Player Infos (4要素)
  Element 0  ← 1番目に入室したプレイヤー（ActorNumber=1）
    Player Name: "アリス"
    Icon Index: 0
    
  Element 1  ← 2番目に入室したプレイヤー（ActorNumber=2）
    Player Name: "ボブ"
    Icon Index: 1
    
  Element 2  ← 3番目に入室したプレイヤー（ActorNumber=3）
    Player Name: "キャロル"
    Icon Index: 2
    
  Element 3  ← 4番目に入室したプレイヤー（ActorNumber=4）
    Player Name: "デイブ"
    Icon Index: 3
```

#### パラメータ説明

- **リストのインデックス（Element番号）**: 入室順を表す
  - **Element 0 = 1番目に入室したプレイヤー**（ActorNumber=1）
  - **Element 1 = 2番目に入室したプレイヤー**（ActorNumber=2）
  - **Element 2 = 3番目に入室したプレイヤー**（ActorNumber=3）
  - **Element 3 = 4番目に入室したプレイヤー**（ActorNumber=4）
  - MultiPlayModeでは入室順は固定（起動順と同じ）
  
- **Player Name**: プレイヤー名（UI表示に使用）

- **Icon Index**: プレイヤーアイコンのインデックス（0-3）

#### 注意

- **ターン順はランダム**: FirstDealでランダムシャッフルされる
- カード配置は入室順基準

---

### 🃏 **機能3: 初期カード配置**

#### 設定方法

```
[Player Debug Settings]
✅ Use Debug Player Info

Debug Player Infos (4要素)
  Element 0  ← 1番目に入室したプレイヤー（ActorNumber=1）
    [Player Settings]
    Player Name: "アリス"
    Icon Index: 0
    
    [Initial Cards]
    ✅ Set Initial Cards
    Target Card
      Suit: Spade
      Number: 1
    Hand Cards (2要素)
      Element 0: Suit=Spade, Number=2
      Element 1: Suit=Spade, Number=3
      
  Element 1  ← 2番目に入室したプレイヤー（ActorNumber=2）
    [Player Settings]
    Player Name: "ボブ"
    Icon Index: 1
    
    [Initial Cards]
    ✅ Set Initial Cards
    Target Card
      Suit: Heart
      Number: 1
    Hand Cards (1要素)
      Element 0: Suit=Heart, Number=2
      
  Element 2  ← 3番目に入室したプレイヤー（ActorNumber=3）
    [Player Settings]
    Player Name: "キャロル"
    Icon Index: 2
    
    [Initial Cards]
    ☐ Set Initial Cards  ← カード設定しない（FirstDealのまま）
      
  Element 3  ← 4番目に入室したプレイヤー（ActorNumber=4）
    [Player Settings]
    Player Name: "デイブ"
    Icon Index: 3
    
    [Initial Cards]
    ☐ Set Initial Cards

[Initial Cards Debug]
Clear Cards Before Setup: ✅  (既存カードをクリア)
First Deal Wait Time: 2  (FirstDeal完了待機時間)
```

#### パラメータ説明

- **リストのインデックス（Element番号）**: 入室順を表す
  - **Element 0 = 1番目に入室したプレイヤー**（ActorNumber=1）
  - **Element 1 = 2番目に入室したプレイヤー**（ActorNumber=2）
  - MultiPlayModeでは入室順は固定なので設定は有効

- **Player Name**: プレイヤー名（UI表示に使用）

- **Icon Index**: プレイヤーアイコンのインデックス（0-3）

- **Set Initial Cards**: ✅にするとこのプレイヤーに初期カードを設定
  - ☐の場合はFirstDealで配られたカードのまま

- **Target Card**: プレイヤーのTargetカード（1枚）
  - Suit: スート（Spade, Heart, Diamond, Club）
  - Number: 数値（1-13）

- **Hand Cards**: プレイヤーの手札（0-3枚）
  - 複数枚設定可能

- **Clear Cards Before Setup**: ✅にすると既存カードを山札に戻してから設定

- **First Deal Wait Time**: FirstDeal完了を待つ時間（秒）
  - デフォルト: 2秒
  - エラーが出る場合は3-4秒に増やす

#### 実行結果

上記の設定例では：
```
ActorNumber 1（Element 0）: 名前=アリス, Target=Spade 1, Hands=[Spade 2, Spade 3]
ActorNumber 2（Element 1）: 名前=ボブ, Target=Heart 1, Hands=[Heart 2]
ActorNumber 3（Element 2）: 名前=キャロル, カードはFirstDealのまま
ActorNumber 4（Element 3）: 名前=デイブ, カードはFirstDealのまま
```

**ポイント**: Element 2と3は`Set Initial Cards = ☐`なので、名前とアイコンだけ設定され、カードはFirstDealで配られたままになります。

---

## 実行フロー

```
1. GameScene起動
   ↓
2. Photon接続（固定リージョン: jp）
   ↓
3. ルーム参加/作成（ルーム名: DEBUG）
   ↓
4. プレイヤー待機（最大4人、タイムアウト10秒）
   ↓
5. 全クライアント: GameManager.Initialize()
   - 入室順（ActorNumber順）でプレイヤー初期化
   - デバッグ設定があれば名前とアイコンを適用
   ↓
6. ホストのみ: GameManager.StartGame()
   - Dealer.FirstDeal()でカード自動配布
   - ターン順がランダムシャッフル
   ↓
7. 2秒待機（FirstDeal完了待ち）
   ↓
8. ホストのみ: デバッグカード設定適用（SetInitialCards=trueのプレイヤーがいる場合）
   └─ SetupDebugInitialCards()
      ├─ 既存カードをクリア（オプション）
      └─ 入室順（Element番号）でカードを配置
   ↓
9. RPC送信で全クライアントに同期
   ↓
10. ゲーム開始（ターン順はランダム）
```

## トラブルシューティング

### エラー: "RPC method not found"

**原因**: PhotonViewコンポーネントが設定されていない

**解決**:
1. GameObjectに`PhotonView`コンポーネントを追加
2. Unityを再起動してスクリプト再コンパイル

---

### エラー: "Connection lost. TimeoutDisconnect"

**原因**: MultiPlayModeでバックグラウンドインスタンスがタイムアウト

**解決**: 既に対策済み（自動的に`Application.runInBackground = true`を設定）

---

### カードが設定されない

**確認**:
1. `Use Debug Player Info`にチェックが入っているか
2. 対象のElementで`Set Initial Cards`にチェックが入っているか
3. `Debug Player Infos`のリストに要素が追加されているか
4. `First Deal Wait Time`が十分か（2秒で足りない場合は3-4秒に）

**Consoleログ確認**:
```
[GameSceneDebugEntry] FirstDeal完了後、デバッグカード設定を適用します
[GameSceneDebugEntry] 入室順 X (Element X) → ActorNumber Y
[GameSceneDebugEntry] カード配置情報を受信しました
[GameSceneDebugEntry] ActorNumber Y の Target に ... を設定
```

---

### ターン順が反映されない

**確認**:
1. `Use Debug Player Info`にチェックが入っているか
2. `Override Turn Order After Deal`にチェックが入っているか
3. 全てのActorNumber（1-4）の設定があるか
4. Turn Orderが0から始まっているか（1ではなく0）

**Consoleログ確認**:
```
[GameSceneDebugEntry] カスタムターン順を適用（初期化時）:
[GameSceneDebugEntry] FirstDeal後の現在のターン順:
[GameSceneDebugEntry] デバッグ用ターン順を再設定:
```

---

## コンテキストメニュー

Inspector上でスクリプトを右クリックすると使用できるメニュー：

- **Start Game**: 手動でゲームを開始（Auto Start Game = ☐ の場合）
- **Stop Game**: ゲームを停止
- **Reset Game**: ゲームをリセット

---

## ベストプラクティス

### デバッグ設定の組み合わせ

#### パターンA: 基本テスト
```
Use Debug Player Info: ☐
→ 全て自動、プレイヤー名はデフォルト、カードとターン順はランダム
```

#### パターンB: プレイヤー名のみカスタマイズ
```
Use Debug Player Info: ✅
  各Element: Set Initial Cards = ☐
→ プレイヤー名とアイコンのみ設定、カードとターン順はランダム
```

#### パターンC: 特定プレイヤーのみカードを制御
```
Use Debug Player Info: ✅
  Element 0: Set Initial Cards = ✅  ← このプレイヤーだけカード設定
  Element 1: Set Initial Cards = ☐
  Element 2: Set Initial Cards = ☐
  Element 3: Set Initial Cards = ☐
→ 1人目だけ初期カードを制御、他3人はランダム
```

#### パターンD: 完全制御
```
Use Debug Player Info: ✅
  全Element: Set Initial Cards = ✅
→ 全プレイヤーの名前、アイコン、初期カードを完全制御
```

### 推奨設定

- **First Deal Wait Time**: 2秒（通常）、3-4秒（重い場合）
- **Connection Timeout**: 15秒
- **Wait For Players Timeout**: 10秒

---

## 注意事項

1. **PhotonViewは必須**
   - RPCによるネットワーク同期に使用
   - 設定しないと全クライアントに反映されない

2. **リストのインデックス = 入室順**
   - **Element 0 = 1番目に入室**（ActorNumber=1）
   - **Element 1 = 2番目に入室**（ActorNumber=2）
   - MultiPlayModeでは入室順は固定（起動順と同じ）

3. **ActorNumberは自動割り当て**
   - Inspector上で指定できない（入室順で自動決定）
   - ActorNumber = 入室順 + 1（1から始まる）

4. **カード設定は入室順基準**
   - Element 0 → ActorNumber 1（1番目に入室）
   - Element 1 → ActorNumber 2（2番目に入室）
   - シンプルで分かりやすい

5. **ターン順はランダム**
   - FirstDealでランダムシャッフルされる
   - ターン順を制御したい場合は、GameModeをDebugに変更するなど別の方法が必要

---

## サンプル設定

### シナリオ: 特定の役を作ってテスト

```
[Player Debug Settings]
✅ Use Debug Player Info

Debug Player Infos (4要素)
  Element 0  ← 1番目に入室（ActorNumber=1、ストレートフラッシュ）
    [Player Settings]
    Player Name: "テストプレイヤー1"
    Icon Index: 0
    
    [Initial Cards]
    ✅ Set Initial Cards
    Target Card: Spade, 1
    Hand Cards (3要素)
      Element 0: Spade, 2
      Element 1: Spade, 3
      Element 2: Spade, 4
    
  Element 1  ← 2番目に入室（ActorNumber=2、フォーカード）
    [Player Settings]
    Player Name: "テストプレイヤー2"
    Icon Index: 1
    
    [Initial Cards]
    ✅ Set Initial Cards
    Target Card: Heart, 1
    Hand Cards (3要素)
      Element 0: Spade, 1
      Element 1: Diamond, 1
      Element 2: Club, 1
    
  Element 2  ← 3番目に入室（ActorNumber=3、通常）
    [Player Settings]
    Player Name: "テストプレイヤー3"
    Icon Index: 2
    
    [Initial Cards]
    ✅ Set Initial Cards
    Target Card: Diamond, 5
    Hand Cards (2要素)
      Element 0: Heart, 7
      Element 1: Club, 9
    
  Element 3  ← 4番目に入室（ActorNumber=4、通常）
    [Player Settings]
    Player Name: "テストプレイヤー4"
    Icon Index: 3
    
    [Initial Cards]
    ✅ Set Initial Cards
    Target Card: Club, 10
    Hand Cards (1要素)
      Element 0: Diamond, 2

[Initial Cards Debug]
Clear Cards Before Setup: ✅
First Deal Wait Time: 2
```

---

## FAQ

### Q: MultiPlayModeでインスタンスが4つ起動しない

**A**: Unity Editorの設定を確認
- Edit → Preferences → Multiplayer Play Mode
- "Enable Multiplayer Play Mode"にチェック
- "Virtual Player Count": 4

---

### Q: ターン順を固定したい

**A**: 現在の仕様では、ターン順は常にFirstDealでランダムシャッフルされます。
ターン順を固定したい場合は、以下の方法があります：
1. GameManagerの`GameMode`を`Debug`に設定し、`FixedPlayerStrategy`を使用
2. カスタムDealerStrategyを作成してシャッフルを無効化

---

### Q: カードが設定されない

**A**: 以下を確認
1. `Use Debug Player Info`にチェックが入っているか
2. 対象Elementの`Set Initial Cards`にチェックが入っているか
3. `First Deal Wait Time`を3-4秒に増やす
4. ConsoleでRPC受信ログが出ているか

---

### Q: 特定のカードが見つからないエラー

**A**: 
- FirstDealで既にそのカードが配られている可能性
- `Clear Cards Before Setup`にチェックを入れる
- または異なるカードを指定

---

## デバッグログの見方

### 正常な実行ログ

```
[GameSceneDebugEntry] デバッグモード開始
[GameSceneDebugEntry] 固定リージョンを設定: jp
[GameSceneDebugEntry] Photonサーバーに接続完了
[GameSceneDebugEntry] ロビーに参加完了
[GameSceneDebugEntry] ルーム参加成功: DEBUG
[GameSceneDebugEntry] ActorNumber: 1
[GameSceneDebugEntry] プレイヤー待機中... (現在: 4/4)
[GameSceneDebugEntry] 全プレイヤーが揃いました: 4
[GameSceneDebugEntry] 入室順 0 (ActorNumber 1): デバッグ設定を適用 - Name=アリス, Icon=0
[GameSceneDebugEntry] 入室順 1 (ActorNumber 2): デバッグ設定を適用 - Name=ボブ, Icon=1
[GameSceneDebugEntry] 入室順 2 (ActorNumber 3): デバッグ設定を適用 - Name=キャロル, Icon=2
[GameSceneDebugEntry] 入室順 3 (ActorNumber 4): デバッグ設定を適用 - Name=デイブ, Icon=3
[GameSceneDebugEntry] デバッグプレイヤー情報を使用（入室順 = ActorNumber順）
[GameSceneDebugEntry] 最終的なプレイヤーリスト（入室順）:
[GameSceneDebugEntry] 入室順 0 (ActorNumber 1): アリス (Type: Local, Icon: 0)
[GameSceneDebugEntry] 入室順 1 (ActorNumber 2): ボブ (Type: Remote, Icon: 1)
[GameSceneDebugEntry] GameManager初期化完了
[GameSceneDebugEntry] ゲーム開始
[GameSceneDebugEntry] FirstDealの完了を待機中... (2秒)
[GameSceneDebugEntry] FirstDeal完了後、デバッグカード設定を適用します
[GameSceneDebugEntry] 入室順 0 (Element 0) → ActorNumber 1
[GameSceneDebugEntry] 入室順 1 (Element 1) → ActorNumber 2
[GameSceneDebugEntry] カード配置情報を全クライアントに送信しました
[GameSceneDebugEntry] カード配置情報を受信しました (クライアント: X)
[GameSceneDebugEntry] ActorNumber 1 の Target に Spade 1 を設定
[GameSceneDebugEntry] ActorNumber 1 の Hands に Spade 2 を追加
[GameSceneDebugEntry] デバッグ用初期カード設定完了
```

---

## まとめ

### 基本的な使い方

1. **GameObjectにアタッチ**（PhotonView込み）
2. **GameManagerを参照設定**
3. **MultiPlayModeを有効化**
4. **Play**するだけで4人プレイ開始

### カスタマイズしたい場合

1. `Use Debug Player Info`にチェック
2. `Debug Player Infos`リストに4要素追加（4人分）
3. 各Elementで設定：
   - 名前とアイコン（常に適用）
   - `Set Initial Cards`でカード設定のオン/オフ
4. 全て**入室順（Element番号）基準**で設定

### 重要なポイント

- **1つのリストで全て管理**: `DebugPlayerInfo`で名前、アイコン、カードを一括設定
- **リストのインデックス = 入室順**
  - Element 0 = 1番目に入室 = ActorNumber 1
  - Element 1 = 2番目に入室 = ActorNumber 2
  - MultiPlayModeでは入室順は固定（起動順と同じ）なので設定は有効
- **プレイヤー毎にカード設定を選択可能**: `Set Initial Cards`のオン/オフで制御
- **ActorNumberは自動割り当て**（Inspector上で指定不要、入室順で決まる）
- **ターン順はランダム**: FirstDealでシャッフルされる
- **PhotonViewは必須**（RPCによるネットワーク同期）

