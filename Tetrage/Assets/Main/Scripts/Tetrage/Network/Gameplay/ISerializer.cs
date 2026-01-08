namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// オブジェクトのシリアライズ/デシリアライズを行うインターフェース
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// オブジェクトをバイト配列にシリアライズする
        /// </summary>
        byte[] Serialize<T>(T obj);

        /// <summary>
        /// バイト配列からオブジェクトをデシリアライズする
        /// </summary>
        T Deserialize<T>(byte[] data);
    }
}

