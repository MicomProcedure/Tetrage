using UnityEngine;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Models;
using Tetrage.Factories;
using Tetrage.UI;
using Tetrage.Core.Contracts;
namespace Tetrage.Tests
{
    /// <summary>
    /// Factory を使って Card と CardPile (View付き) をシーン上に生成し、デバッグ確認するクラス
    /// </summary>
    public class FactoryDebugRunner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private CardPileView pileViewPrefab;

        [Header("Parents")]
        [SerializeField] private Transform cardParent;
        [SerializeField] private Transform pileParent;
        [Header("Spawn Settings")]
        [SerializeField] private int countPerSuit = 1;
        [SerializeField] private int pileCount = 10;
        private const int SINGLE = 1;

        private ICardFactory _cardModelFactory;
        private CardWithViewFactory _cardFactory;
        private ICardPileFactory _pileModelFactory;
        private CardPileWithViewFactory _pileFactory;
        private Dictionary<Card, CardView> _cardViewsDict = new Dictionary<Card, CardView>();

        private Card _card;
        private List<Card> _cards = new List<Card>();
        private CardPile _pile;

        private void Awake()
        {
            // モデルファクトリとデコレータファクトリの初期化
            _cardModelFactory = new CardModelFactory();
            _cardFactory = new CardWithViewFactory(_cardModelFactory, cardViewPrefab, cardParent, _cardViewsDict); // カードモデルとビューの対応辞書を渡す

            _pileModelFactory = new CardPileFactory();
            _pileFactory = new CardPileWithViewFactory(_pileModelFactory, pileViewPrefab, pileParent, _cardViewsDict); // CardWithViewFactoryで生成されたカードモデルとビューの対応辞書を渡す
        }

        [ContextMenu("Spawn Sample Cards")]
        private void SpawnSampleCards()
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };

            // _cardParent = pi
            foreach (var suit in suits)
            {
                for (int i = 1; i <= countPerSuit; i++)
                {
                    // カードを生成してCardPileに追加
                    var card = _cardFactory.CreateCard(suit, i);
                    _cards.Add(card);
                }
            }       
            // CardPileを作成
            _pile = _pileFactory.CreatePile("SampleCards", _cards, suits.Length * countPerSuit);

        }

        [ContextMenu("Spawn One Sample Card")] 
        private void SpawnOneSampleCard()
        {
            
            // カードを生成してCardPileに追加
            _card = _cardFactory.CreateCard(Suit.Spade, SINGLE);
            _cards.Add(_card);
            // CardPileを作成
            _pile = _pileFactory.CreatePile("SingleCard", _cards, SINGLE);
        }

        [ContextMenu("Spawn Sample Pile")]
        private void SpawnSamplePile()
        {
            _pile = _pileFactory.CreatePile("Test", pileCount);
        }

        [ContextMenu("Flip Sample Card")]
        private void FlipSampleCard()
        {
            if (_card == null)
            {
                Debug.LogWarning("まずは『Spawn One Sample Card』でカードを生成してください");
                return;
            }

            _card.Flip();
            Debug.Log($"カードを{(_card.IsVisible ? "表" : "裏")}向きにしました");
        }
    }
} 