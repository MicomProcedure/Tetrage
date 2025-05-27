using Tetrage.Core.Contracts;
using Tetrage.Models;

namespace Tetrage.Presenters
{
    public class StagePresenter
    {
        private IStageView _view;
        private Stage _model;

        public StagePresenter(IStageView view, Stage model)
        {
            _view = view;
            _model = model;
        }
    }
}