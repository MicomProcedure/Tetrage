using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Core.Contracts
{
    public interface IPlayerFactory
    {
        public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex);
        public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex, CardPile target, CardPile hands, CardPile tmp);

    }
}