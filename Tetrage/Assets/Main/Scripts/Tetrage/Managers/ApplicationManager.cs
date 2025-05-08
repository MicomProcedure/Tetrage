// using UnityEngine;
// using Tetrage.Core.Contracts;
// using Tetrage.Factories;
// using Tetrage.Managers;

// namespace Tetrage
// {
//     /// <summary>
//     /// アプリケーション全体のライフサイクル管理とDIルート
//     /// </summary>
//     public class ApplicationManager : MonoBehaviour
//     {
//         public static ApplicationManager Instance { get; private set; }
//         private IGameInitializer _gameInitializer;

//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);

//                 // DIコンテナの構築
//                 var deckFactory = new CardDeckFactory();
//                 var pileFactory = new CardPileFactory();
//                 var playerFactory = new PlayerFactory();
//                 var stageFactory = new StageFactory(deckFactory, pileFactory);
//                 // var dealer = new Dealer(deckFactory, pileFactory, playerFactory);

//                 // ルートとなるゲームマネージャー
//                 // _gameInitializer = new GameManager(dealer, stageFactory);
//             }
//             else
//             {
//                 Destroy(gameObject);
//             }
//         }

//         private void Start()
//         {
//             _gameInitializer?.InitializeGame();
//         }
//     }
// } 