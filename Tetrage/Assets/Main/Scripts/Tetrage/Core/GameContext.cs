using System.Collections.Generic;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core
{
    /// <summary>
    /// ゲーム全体の読み取り専用コンテキスト。状態の書き込みは GameplayDomainEventHandler だけが行う想定。
    /// </summary>
    public sealed class GameContext : IGameContext
    {
        private IPlayer _currentPlayer;
        private IReadOnlyList<IPlayer> _players;
        private IPlayer _userPlayer;
        private Stage _stage;
        private readonly IGameplayEventBus _events;
        private int _turnIndex;

        public GameContext(Stage stage, IReadOnlyList<IPlayer> players, IPlayer userPlayer, IGameplayEventBus events)
        {
            _stage = stage;
            _players = players;
            _userPlayer = userPlayer;
            _events = events;
        }
        public IPlayer CurrentPlayer => _currentPlayer;
        public IPlayer UserPlayer => _userPlayer;
        public IReadOnlyList<IPlayer> Players => _players;
        public Stage Stage => _stage;
        public IGameplayEventBus Events => _events;
        public int TurnIndex => _turnIndex;

        // 以下は Applier からのみ呼ばれる setter（公開しない）
        public void SetCurrentPlayerInternal(IPlayer player) { _currentPlayer = player; }
        public void SetPlayersInternal(IReadOnlyList<IPlayer> ordered) { _players = ordered; }
        public void SetUserPlayerInternal(IPlayer userPlayer) { _userPlayer = userPlayer; }
        public void SetStageInternal(Stage stage) { _stage = stage; }
        public void ResetTurnIndexInternal() { _turnIndex = 0; }
        public void IncrementTurnIndexInternal() { _turnIndex++; }
    }
}


