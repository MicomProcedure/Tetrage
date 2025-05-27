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
    /// カードパイルビューの種類を表す列挙型
    /// </summary>
    public enum CardPileType
    {
        Target, // ターゲットのカードパイルビュー
        Hands,  // 手札のカードパイルビュー
        Tmp,    // プレイヤーの一時保持手札のカードパイルビュー
        Stack,  // 山札のカードパイルビュー
        Trash,  //　捨て札のカードパイルビュー
        Basic,  // 基本のカードパイルビュー
    }


}