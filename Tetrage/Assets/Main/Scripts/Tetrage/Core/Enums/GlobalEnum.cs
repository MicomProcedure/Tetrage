namespace Tetrage.Core.Enums
{
    /// <summary>
    /// ゲームのフェーズを表す列挙型
    /// </summary>
    public enum GamePhase
    {
        Starting, // 初期準備フェーズ
        Playing, // 実際のプレイフェーズ
        Ending // 勝敗判定・結果表示フェーズ
    }

    /// <summary>
    /// カードのスートを表す列挙型
    /// </summary>
    public enum Suit
    {
        Spade,
        Heart,
        Diamond,
        Club
    }

    /// <summary>
    /// カードパイルの種類を表す列挙型
    /// </summary>
    public enum CardPileType
    {
        Target, // ターゲットのカードパイル
        Hands,  // 手札のカードパイル
        Tmp,    // プレイヤーの一時保持手札のカードパイル
        Stack,  // 山札のカードパイル
        Trash,  //　捨て札のカードパイル
        Basic,  // 基本のカードパイル
    }

    public enum PlayerType
    {
        LocalPlayer,
        RemotePlayer,
        Bot,
    }

    /// <summary>
    /// アクションの種類を表す列挙型
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// カードを引くアクション
        /// </summary>
        Draw,
        
        /// <summary>
        /// カードを表向きにするアクション
        /// </summary>
        Open,
        
        /// <summary>
        /// リーチ状態にするアクション
        /// </summary>
        Reach,
        
        /// <summary>
        /// チェックアクション
        /// </summary>
        Check
    }

}