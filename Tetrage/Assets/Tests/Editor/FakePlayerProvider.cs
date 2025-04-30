using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Actions;
using Tetrage.Core.Enums;

namespace Tetrage.Test.Editor
{
    /// <summary>
    /// GameAction のテストを簡潔に行うためのベースクラス。
    /// FakePlayerProvider と 3 人の Player(MonoBehaviour) をセットアップします。
    /// </summary>
    public class GameActionTestBase
    {
        protected class FakePlayerProvider : IPlayerProvider
        {
            public Player CurrentPlayer { get; private set; }
            public IReadOnlyList<Player> Players { get; private set; }
            public event System.Action OnPlayersChanged;

            public FakePlayerProvider(IEnumerable<Player> players, Player current)
            {
                Players = new List<Player>(players);
                CurrentPlayer = current;
            }

            public void RaisePlayersChanged()
            {
                OnPlayersChanged?.Invoke();
            }
        }

        protected List<GameObject> _cleanupObjects;
        protected FakePlayerProvider _provider;
        protected List<Player> _players;

        [SetUp]
        public void SetUp()
        {
            _cleanupObjects = new List<GameObject>();
            _players = new List<Player>();

            // 3 人分の Player コンポーネントを作成
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject($"Player_{i}");
                var player = go.AddComponent<Player>();
                player.PlayerID = i;
                _players.Add(player);
                _cleanupObjects.Add(go);
            }

            // 各プレイヤーに異なる手札とターゲットを設定
            for (int i = 0; i < _players.Count; i++)
            {
                var p = _players[i];
                // 3 枚の手札を追加（スート・番号はユニークに）
                for (int j = 0; j < 3; j++)
                {
                    var card = new GameObject($"Card_{i}_{j}").AddComponent<Card>();
                    card.suit = (Suit)((i + j) % 4);
                    card.Number = i * 10 + j + 1;
                    card.isVisible = true;
                    p.Hands.Add(card);
                    _cleanupObjects.Add(card.gameObject);
                }
                // Target カード
                var tgt = new GameObject($"Target_{i}").AddComponent<Card>();
                tgt.suit = Suit.Spade;
                tgt.Number = i;
                tgt.isVisible = false;
                p.Target = tgt;
                _cleanupObjects.Add(tgt.gameObject);
            }

            // FakeProvider を作成。最初のプレイヤーを CurrentPlayer に設定
            _provider = new FakePlayerProvider(_players, _players[0]);
        }

        [TearDown]
        public void TearDown()
        {
            // 作成したオブジェクトをすべて破棄
            foreach (var go in _cleanupObjects)
                Object.DestroyImmediate(go);
        }
    }
}