using UnityEngine;
using Tetrage.Models;

namespace Tetrage.UI
{

    public static class CardClickDispatcher
    {
        public static event System.Action<Card> OnCardClicked;
        public static void Invoke(Card c) => OnCardClicked?.Invoke(c);
    }
}
