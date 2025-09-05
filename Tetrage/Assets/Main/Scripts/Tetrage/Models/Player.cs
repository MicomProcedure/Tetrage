using UnityEngine;
using System.Collections.Generic;
// using Tetrage.Actions;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Tetrage.Models
{
    /// <summary>
    /// ゲーム内のプレイヤーを表します。
    /// </summary>
    public class Player : IPlayer
    {
        private static int _playerCount = 0;

        // UserID フィールドを追加 (読み取り専用プロパティとして公開)
        public string UserId { get; }

        /// <summary>
        /// プレイヤーの一意な識別子。
        /// </summary>
        public int PlayerId { get; }

        /// <summary>
        /// プレイヤーの最初の一枚(本来のカード)
        /// </summary>
        public CardPile Target { get; private set; }


        /// <summary>
        /// プレイヤーが所持している手札の一覧。
        /// </summary>
        public CardPile Hands { get; private set; }

        /// <summary>
        /// 一時的に保持しているカードの一覧。
        /// </summary>
        public CardPile Tmp { get; private set; }

        public bool IsReach { get; private set; }


        public Player(string userId, CardPile target, CardPile hands, CardPile tmp) // UserID をコンストラクタで受け取るように変更
        {
            PlayerId = _playerCount++;

            Target = target;
            Hands = hands;
            Tmp = tmp;

            // UserID が指定されなかった場合は、PlayerID を元にしたデフォルト値を設定
            UserId = userId ?? $"Player_{PlayerId}";
        }


        public int GetPlayerCount()
        {
            return _playerCount;
        }

        /// <summary>
        /// プレイヤーをReach状態にする。
        /// </summary>
        public void Reach()
        {
            IsReach = true;
        }

        /// <summary>
        /// 指定されたアクションを実行します。
        /// </summary>
        /// <param name="action">実行するゲームアクション。</param>
        // public async UniTask PerformAction(GameAction action) // async UniTask に変更
        // {
        //     // アクションが有効か検証
        //     if (!action.Validate())
        //     {
        //         Debug.LogWarning($"Action '{action.GetType().Name}' for Player {UserId} is not valid.");
        //         return;
        //     }

        //     // アクションの実行を待ちます
        //     await action.Execute();
        // }
    }
}