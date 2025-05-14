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
        [Tooltip("0. Basic, 1. Hands, 2. Tmp, 3. Stack, 4. Trashの順に格納すること")]
        [SerializeField] private List<BasicCardPileView> pileViewPrefabs;
        private Dictionary<CardPileViewType, BasicCardPileView> _pileViewPrefabDict = new Dictionary<CardPileViewType, BasicCardPileView>()
        {
            {CardPileViewType.Basic, null},
            {CardPileViewType.Hands, null},
            {CardPileViewType.Tmp, null},
            {CardPileViewType.Stack, null},
            {CardPileViewType.Trash, null},
        }; // カードパイルビューのプレハブ

        [Header("Parents")]
        [SerializeField] private Transform cardParent;
        [SerializeField] private Transform pileParent;
        [Header("Spawn Settings")]
        [SerializeField] private int countPerSuit = 1;
        [SerializeField] private int _pileCapacity = 10;
        [Tooltip("カードパイル間の間隔")]
        [SerializeField] private float _pileOffset = 2f;
        [Tooltip("カードパイルビューの種類")]
        [SerializeField] private CardPileViewType _pileViewType = CardPileViewType.Basic;
        [Header("CardPile LayoutSettings")]
        [SerializeField] private float _pileWidth = 10f;
        [SerializeField] private float _cardViewMinSpacing = 0f;
        [SerializeField] private float _cardViewMaxSpacing = 1000f;
        [SerializeField] private Vector3 _cardViewPositionOffset = new Vector3(0, 0, 0);
        // このスクリプトで生成した回数
        [SerializeField] private int _spawnCount = 0;
        private const int SINGLE = 1;

        private Dictionary<Card, CardView> _cardViewsDict = new Dictionary<Card, CardView>();

        private List<Card> _cards = new List<Card>();
        private List<CardPile> _cardPiles = new List<CardPile>();
        public List<CardPile> CardPiles => _cardPiles; // 生成したカードパイルを外部に公開するリスト

        private ICardFactory _cardModelFactory;
        private CardWithViewFactory _cardFactory;
        private CardPileBuilder _pileBuilder;


        private void Awake()
        {
            _pileViewPrefabDict[CardPileViewType.Basic] = pileViewPrefabs[0];
            _pileViewPrefabDict[CardPileViewType.Hands] = pileViewPrefabs[1];
            _pileViewPrefabDict[CardPileViewType.Tmp] = pileViewPrefabs[2];
            _pileViewPrefabDict[CardPileViewType.Stack] = pileViewPrefabs[3];
            _pileViewPrefabDict[CardPileViewType.Trash] = pileViewPrefabs[4];
            // モデルファクトリとデコレータファクトリの初期化
            _cardModelFactory = new CardModelFactory();
            _cardFactory = new CardWithViewFactory(_cardModelFactory, cardViewPrefab, cardParent, _cardViewsDict);
            // CardPileBuilder の初期化
            _pileBuilder = new CardPileBuilder(new CardPileFactory())
                .UseCardFactory(_cardFactory)
                .UseView(_pileViewPrefabDict[_pileViewType], pileParent, _cardViewsDict);
        }

        [ContextMenu("Spawn Sample Cards")]
        private void SpawnSampleCards()
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            var count = Mathf.Max(suits.Length * countPerSuit, _pileCapacity);

            // ビルダーで山札生成（初期カード付き）
            var pile = _pileBuilder
                .UseView(_pileViewPrefabDict[_pileViewType], pileParent, _cardViewsDict)
                .WithName("SampleCards")
                .WithMaxCount(count)
                .WithInitialCards(_cardFactory, suits, countPerSuit)
                .WithLayout(_pileWidth, _cardViewMinSpacing, _cardViewMaxSpacing, new Vector3(0, _spawnCount * _pileOffset, 0) + _cardViewPositionOffset)    // カードパイルの位置オフセット(生成されるたびにずれる)
                .Build();
            // デバッグ用にカードモデルを保持
            foreach (var card in pile.Cards)
                _cards.Add(card);
            // デバッグ用にカードパイルを保持
            _cardPiles.Add(pile);

            _spawnCount++;
        }

        [ContextMenu("Spawn One Sample Card")]
        private void SpawnOneSampleCard()
        {

            // 山札生成（1枚のみ）
            var pile = _pileBuilder
                .UseView(_pileViewPrefabDict[_pileViewType], pileParent, _cardViewsDict)
                .WithName("SingleCard")
                .WithMaxCount(SINGLE)
                .WithInitialCards(_cardFactory, new[] { Suit.Spade }, SINGLE)
                .WithLayout(_pileWidth, _cardViewMinSpacing, _cardViewMaxSpacing, new Vector3(0, _spawnCount * _pileOffset, 0) + _cardViewPositionOffset)    // カードパイルの位置オフセット(生成されるたびにずれる)
                .Build();
            // デバッグ用にカードを取得
            var card = pile.Cards.FirstOrDefault();
            if (card != null)
            {
                _cards.Add(card);
            }

            // デバッグ用にカードパイルを保持
            _cardPiles.Add(pile);
            _spawnCount++;
        }

        [ContextMenu("Spawn Sample Pile")]
        private void SpawnSamplePile()
        {
            // 山札生成（初期カードなし）
            var pile = _pileBuilder
                .UseView(_pileViewPrefabDict[_pileViewType], pileParent, _cardViewsDict)
                .WithName("Test")
                .WithMaxCount(_pileCapacity)
                .WithLayout(_pileWidth, _cardViewMinSpacing, _cardViewMaxSpacing, new Vector3(0, _spawnCount * _pileOffset, 0) + _cardViewPositionOffset)    // カードパイルの位置オフセット(生成されるたびにずれる)
                .Build();
            // デバッグ用にカードパイルを保持
            _cardPiles.Add(pile);
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