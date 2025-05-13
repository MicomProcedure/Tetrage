using UnityEngine;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Models;
using Tetrage.Factories;
using Tetrage.UI;
using Tetrage.Core.Contracts;
using System.Linq;
namespace Tetrage.Tests
{
    /// <summary>
    /// Factory を使って Card と CardPile (View付き) をシーン上に生成し、デバッグ確認するクラス
    /// </summary>
    public class FactoryDebugRunner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private BasicCardPileView pileViewPrefab;

        [Header("Parents")]
        [SerializeField] private Transform cardParent;
        [SerializeField] private Transform pileParent;
        [Header("Spawn Settings")]
        [SerializeField] private int countPerSuit = 1;
        [SerializeField] private int pileCount = 10;
        [SerializeField] private float _pileOffset = 2f;
        // このスクリプトで生成した回数
        [SerializeField] private int _spawnCount = 0;
        private const int SINGLE = 1;

        private Dictionary<Card, CardView> _cardViewsDict = new Dictionary<Card, CardView>();

        private Card _card;
        private List<Card> _cards = new List<Card>();
        private CardPile _pile;

        private ICardFactory _cardModelFactory;
        private CardWithViewFactory _cardFactory;
        private CardPileBuilder _pileBuilder;


        private void Awake()
        {
            // モデルファクトリとデコレータファクトリの初期化
            _cardModelFactory = new CardModelFactory();
            _cardFactory = new CardWithViewFactory(_cardModelFactory, cardViewPrefab, cardParent, _cardViewsDict);
            // CardPileBuilder の初期化
            _pileBuilder = new CardPileBuilder(new CardPileFactory())
                .UseCardFactory(_cardFactory)
                .UseView(pileViewPrefab, pileParent, _cardViewsDict);
        }

        [ContextMenu("Spawn Sample Cards")]
        private void SpawnSampleCards()
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };

            // ビルダーで山札生成（初期カード付き）
            _pile = _pileBuilder
                .WithName("SampleCards")
                .WithMaxCount(suits.Length * countPerSuit)
                .WithInitialCards(_cardFactory, suits, countPerSuit)
                .WithLayout(positionOffset: new Vector3(0, _spawnCount*_pileOffset, 0))    // カードパイルの位置オフセット(生成されるたびにずれる)
                .Build();
            // デバッグ用にカードモデルを保持
            foreach (var card in _pile.Cards)
                _cards.Add(card);
            _spawnCount++;
        }

        [ContextMenu("Spawn One Sample Card")]
        private void SpawnOneSampleCard()
        {
 
            // 山札生成（1枚のみ）
            _pile = _pileBuilder
                .WithName("SingleCard")
                .WithMaxCount(SINGLE)
                .WithInitialCards(_cardFactory, new[]{Suit.Spade}, SINGLE)
                .WithLayout(positionOffset: new Vector3(0, _spawnCount*_pileOffset, 0))    // カードパイルの位置オフセット(生成されるたびにずれる)
                .Build();
            // デバッグ用にカードを取得
            var card = _pile.Cards.FirstOrDefault();
            if (card != null)
            {
                _card = card;
                _cards.Add(card);
            }
            _spawnCount++;
        }

        [ContextMenu("Spawn Sample Pile")]
        private void SpawnSamplePile()
        {
            // 山札生成（初期カードなし）
            _pile = _pileBuilder
                .WithName("Test")
                .WithMaxCount(pileCount)
                .WithLayout(positionOffset: new Vector3(0, _spawnCount*_pileOffset, 0))    // カードパイルの位置オフセット(生成されるたびにずれる)
                .Build();
            _spawnCount++;
        }

        [ContextMenu("Flip Sample Cards")]
        private void FlipSampleCard()
        {
            if (_cards.Count == 0)
            {
                Debug.LogWarning("まずはSpawnメソッドでカードを生成してください");
                return;
            }

            // 山札内のカードをすべて裏返し
            foreach (var c in _cards)
                c.Flip();
        }
    }
} 