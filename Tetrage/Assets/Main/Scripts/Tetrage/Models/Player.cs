using UnityEngine;
using System.Collections.Generic;
// using Tetrage.Actions;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Ids;

namespace Tetrage.Models
{
    /// <summary>
    /// ゲーム内のプレイヤーを表します。
    /// </summary>
    public class Player : IPlayer, IIdentifiable<PlayerId>
    {
        private static int _playerCount = 0;

        // UserID フィールドを追加 (読み取り専用プロパティとして公開)
        public string UserId { get; }

        /// <summary>
        /// プレイヤーの一意な識別子（後方互換用）。強い型 <see cref="Id"/> を暗黙変換して返す。
        /// </summary>
        public int PlayerId => Id;

        /// <summary>
        /// 強い型のプレイヤーID（不変）
        /// </summary>
        public PlayerId Id { get; }

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
            Id = new PlayerId(_playerCount++);

            Target = target;
            Hands = hands;
            Tmp = tmp;

            // UserID が指定されなかった場合は、Id を元にしたデフォルト値を設定
            UserId = userId ?? $"Player_{(int)Id}";
        }

        /// <summary>
        /// プレイヤーIDを指定して初期化するコンストラクタ（推奨）。
        /// </summary>
        public Player(PlayerId id, string userId, CardPile target, CardPile hands, CardPile tmp)
        {
            Id = id;

            Target = target;
            Hands = hands;
            Tmp = tmp;

            // UserID が指定されなかった場合は、Id を元にしたデフォルト値を設定
            UserId = userId ?? $"Player_{(int)Id}";
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


    }
}