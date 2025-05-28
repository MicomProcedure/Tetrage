namespace Tetrage.Core.Contracts
{
    public interface IPlayerFactory
    {
        public IPlayer CreatePlayer(string userId);

    }
}