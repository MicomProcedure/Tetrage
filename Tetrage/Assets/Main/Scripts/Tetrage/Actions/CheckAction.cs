using Tetrage.Models;

namespace Tetrage.Actions
{
    public class CheckAction : GameAction
    {
        private Player _requester;
        private Player _targetPlayer;

        public CheckAction(Player requester, Player targetPlayer)
        {
            _requester = requester;
            _targetPlayer = targetPlayer;
        }

        public override bool Validate(Player player)
        {

            return _targetPlayer.Target != null;

            return true;
        }

        public override void Execute(Player player)
        {
            
        }
    }
}
