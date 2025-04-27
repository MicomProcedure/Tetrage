using UnityEngine;
using Tetrage.Models;

namespace Tetrage.Actions
{
    public class OpenAction : GameAction
    {
        private Player _requester;
        private Player _targetPlayer;
        private Card _card;

        public OpenAction(Player requester, Card card, Player targetPlayer)
        {
            _requester = requester;
            _card = card;
            _targetPlayer = targetPlayer;
        }

        public override bool Validate(Player player)
        {
            // Openの条件判定

            return true;
        }

        public override void Execute(Player player)
        {

        }
    }
}
