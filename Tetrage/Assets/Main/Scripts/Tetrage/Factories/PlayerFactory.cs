using UnityEngine; // MonoBehaviourを扱うため追加
using System.Collections.Generic;
using Tetrage.Models;

namespace Tetrage.Factories
{
    /// <summary>
    /// プレイヤー生成用Factory
    /// </summary>
    public class PlayerFactory
    {
        /// <summary>
        /// 指定した人数のプレイヤーモデルを生成します。
        /// </summary>
        /// <param name="playerCount">生成するプレイヤーの人数。</param>
        /// <returns>生成されたIPlayerインターフェースのリスト。</returns>
        public List<IPlayer> CreatePlayers(int playerCount)
        {
            var players = new List<IPlayer>();

            // 不正な入力チェック
            if (playerCount <= 0)
            {
                Debug.LogWarning("プレイヤーの人数は1以上である必要があります。");
                return players;
            }

            for (int i = 0; i < playerCount; i++)
            {
                // 新しいGameObjectを作成（プレイヤーの名前を設定）
                GameObject playerGameObject = new GameObject($"Player_{i + 1}");

                // PlayerコンポーネントをGameObjectに追加
                // AddComponent<T>() は、TがMonoBehaviourを継承している場合にのみ有効です。
                Player playerComponent = playerGameObject.AddComponent<Player>();

                // PlayerIDを設定
                playerComponent.PlayerID = i + 1; // 1から始まるIDを割り当てる

                // 生成したPlayerコンポーネント（IPlayer型として）をリストに追加
                players.Add(playerComponent);

                Debug.Log($"Player {playerComponent.PlayerID} が生成されました。");
            }

            return players;
        }
    }
}