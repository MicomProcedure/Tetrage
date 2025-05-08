namespace Tetrage
{
    /// <summary>
    /// ゲーム初期化処理の契約
    /// </summary>
    public interface IGameInitializer
    {
        /// <summary>
        /// ゲーム全体の初期化を実行します。
        /// </summary>
        void InitializeGame();
    }
} 