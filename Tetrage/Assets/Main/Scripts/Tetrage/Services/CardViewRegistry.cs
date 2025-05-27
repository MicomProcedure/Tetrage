using Tetrage.Models;
using Tetrage.UI;
using System.Collections.Generic;

namespace Tetrage.Services
{
    public static class CardViewRegistry
    {
        private static readonly Dictionary<Card, CardView> _cardViews = new Dictionary<Card, CardView>();
        
        public static void Register(Card card, CardView view)
        {
            if (_cardViews.ContainsKey(card)) {
                UnityEngine.Debug.LogError("指定されたカードはすでに登録されているため登録不可能です。");
                return;
            }

            _cardViews[card] = view;
        }
        
        public static void Unregister(Card card)
        {
            if (!_cardViews.ContainsKey(card)) {
                UnityEngine.Debug.LogError("指定されたカードは登録されていないため削除不可能です。");
                return;
            }
            _cardViews.Remove(card);
        }
        
        public static CardView? GetView(Card card)
        {
            return _cardViews.TryGetValue(card, out var view) ? view : null;
        }
        
        public static Dictionary<Card, CardView> GetAll() => _cardViews;
        
        // テスト用・シーン切り替え用
        public static void Clear() => _cardViews.Clear();
    }
}