using UnityEngine;
using Tetrage.Models;
using R3;

namespace Tetrage.UI
{

    public static class CardClickDispatcher
    {
        private static readonly Subject<Card> _cardClicked = new();
        public static Observable<Card> CardClicked => _cardClicked;
        public static void Publish(Card c) => _cardClicked.OnNext(c);
    }
}
