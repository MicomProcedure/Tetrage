namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 簡易シリアライザ（JsonUtilityの代替として byte[] をそのまま運ぶ用途）
    /// ここではシリアライズ層は最小化し、PUNの RaiseEvent に object を渡す。
    /// </summary>
    public sealed class PhotonJsonSerializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            // Unity AOTと相性を考慮し JsonUtility を使う場合は string→byte[]
            var json = UnityEngine.JsonUtility.ToJson(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }
    }
}

