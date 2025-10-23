using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Core.Ids;
using System.Collections.Generic;

namespace Tetrage.Factories
{
	/// <summary>
	/// 生成された CardPile を必ずレジストリに登録するデコレーターファクトリ。
	/// </summary>
	public sealed class RegisteringCardPileFactory : ICardPileFactory
	{
		private readonly ICardPileFactory _inner;
		private readonly IdRegistry<PileId, CardPile> _pileRegistry;

		public RegisteringCardPileFactory(ICardPileFactory inner, IdRegistry<PileId, CardPile> pileRegistry)
		{
			_inner = inner;
			_pileRegistry = pileRegistry;
		}

		public CardPile CreatePile(PileId id, string name, int maxCount)
		{
			var pile = _inner.CreatePile(id, name, maxCount);
			_pileRegistry.Register(pile);
			return pile;
		}

		public CardPile CreatePile(PileId id, string name, IEnumerable<Card> initialCards, int maxCount = int.MaxValue)
		{
			var pile = _inner.CreatePile(id, name, initialCards, maxCount);
			_pileRegistry.Register(pile);
			return pile;
		}
	}
}


