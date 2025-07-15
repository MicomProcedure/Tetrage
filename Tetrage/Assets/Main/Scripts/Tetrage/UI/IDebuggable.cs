namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// デバッグ情報をGUIに表示する機能を持つコンポーネントのためのインターフェース
    /// </summary>
    public interface IDebuggable
    {
        /// <summary>
        /// IMGUIを使用してデバッグ情報を描画します
        /// </summary>
        void DrawDebugGUI();
    }
}