using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Core.Ids;

namespace Tetrage.Factories
{
	/// <summary>
	/// 生成された Player を必ずレジストリに登録するデコレーターファクトリ。
	/// </summary>
	public sealed class RegisteringPlayerFactory : IPlayerFactory
	{
		private readonly IPlayerFactory _inner;
		private readonly IdRegistry<PlayerId, Player> _playerRegistry;

		public RegisteringPlayerFactory(IPlayerFactory inner, IdRegistry<PlayerId, Player> playerRegistry)
		{
			_inner = inner;
			_playerRegistry = playerRegistry;
		}

		public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex)
		{
			var p = _inner.CreatePlayer(id, userId, iconIndex);
			if (p is Player mp)
			{
				_playerRegistry.Register(mp);
			}
			return p;
		}

		public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex, CardPile target, CardPile hands, CardPile tmp)
		{
			var p = _inner.CreatePlayer(id, userId, iconIndex, target, hands, tmp);
			if (p is Player mp)
			{
				_playerRegistry.Register(mp);
			}
			return p;
		}
	}
}


