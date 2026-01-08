namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワーク経由でイベントをブロードキャストするインターフェース
    /// </summary>
    public interface INetworkBroadcaster
    {
        /// <summary>
        /// 全クライアントにイベントを送信する
        /// </summary>
        void Raise<T>(EventCode code, T payload);

        /// <summary>
        /// 指定したActorNumberのクライアントにのみ送信する
        /// </summary>
        void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers);

        /// <summary>
        /// 指定したActorNumberの単一クライアントにのみ送信する
        /// </summary>
        void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber);
    }
}

