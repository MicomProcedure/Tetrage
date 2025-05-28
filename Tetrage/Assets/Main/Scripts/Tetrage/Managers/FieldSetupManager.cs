using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Factories;

namespace Tetrage.Managers
{
    public class FieldSetupManager
    {
        private readonly IStageFactory _stageFactory;
        private readonly PlayerBuilder _playerBuilder;
        private readonly List<IPlayer> _participants = new List<IPlayer>();


        public FieldSetupManager(List<IPlayer> participants, IStageFactory stageFactory, PlayerBuilder playerBuilder)
        {
            _participants = participants;
            _stageFactory = stageFactory;
            _playerBuilder = playerBuilder;
        }

        public void SetupField()
        {
            var stage = _stageFactory.SetupStage();

        }

    }
}