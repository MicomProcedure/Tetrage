using Tetrage.Core.Contracts;
using System.Collections.Generic;

namespace Tetrage.Managers
{
    public class FieldSetupManager
    {
        private readonly IStageFactory _stageFactory;
        private readonly IPlayerFactory _playerFactory;
        private readonly List<IPlayer> _participants = new List<IPlayer>();


        public FieldSetupManager(List<IPlayer> participants, IStageFactory stageFactory, IPlayerFactory playerFactory)
        {
            _participants = participants;
            _stageFactory = stageFactory;
            _playerFactory = playerFactory;
        }

        public void SetupField()
        {
            var stage = _stageFactory.SetupStage();

        }

    }
}