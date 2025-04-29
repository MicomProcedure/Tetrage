using Tetrage.Models;

namespace Tetrage.Actions
{
    public class CheckAction : GameAction
    {
        private Player _requester;
        private Player _targetPlayer;

        public CheckAction(Player requester) : base(requester)
        {
            _requester = requester;
 
        }

        public override bool Validate()
        {

            return _targetPlayer.Target != null;

            return true;
        }

        public override void Execute()
        {
            
        }
    }
}
