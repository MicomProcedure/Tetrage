using Tetrage.Core.Contracts;
using System.Collections.Generic;

namespace Tetrage.Managers
{
    public class FieldSetupManager
    {
        private readonly IStageFactory _stageFactory;
        private readonly IPlayerFactory _playerFactory;
        private readonly List<IPlayer> _participants = new List<IPlayer>();


        public FieldSetupManager(List<IPlayer> participants)
        {
            _participants = participants;
        }

        public void SetupField()
        {
            var stage = _stageFactory.SetupStage();
            var player = _playerFactory.CreatePlayer();
        }

    }
}