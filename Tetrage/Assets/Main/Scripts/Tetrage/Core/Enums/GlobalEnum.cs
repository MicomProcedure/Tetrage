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
    /// カードの束の所有者を表す列挙型
    /// </summary>
    public enum CardOwner
    {
        Player,
        Stage,
        Dealer,
        Null,
    }
}