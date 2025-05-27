using Tetrage.Core.Contracts;
using Tetrage.Models;

namespace Tetrage.Presenters
{
    public class PlayerPresenter
    {
        private IPlayerView _view;
        private IPlayer _model;

        public PlayerPresenter(IPlayerView view, IPlayer model)
        {
            _view = view;
            _model = model;
        }
    }
}