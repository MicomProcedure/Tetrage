using Tetrage.Models;
using Tetrage.Core.Contracts;

namespace Tetrage.Core.Contracts
{
    public interface IPlayerFactory
    {
        public IPlayer CreatePlayer(string userId, Card target, CardPile hands, CardPile tmp);

    }
}