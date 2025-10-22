using Tetrage.Core.Ids;

namespace Tetrage.Core.Contracts
{
    public interface IPlayerFactory
    {
        public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex);

    }
}