using Tetrage.Core.Contracts;

namespace Tetrage.Factories
{
    /// <summary>
    /// ゲームステージ生成用Factory
    /// </summary>
    public class StageFactory
    {
        private readonly ICardPileFactory _deckFactory;
        private readonly ICardPileFactory _pileFactory;

        public StageFactory(ICardPileFactory deckFactory, ICardPileFactory pileFactory)
        {
            _deckFactory = deckFactory;
            _pileFactory = pileFactory;
        }

        /// <summary>
        /// ステージ(MonoBehaviour)生成と初期配置
        /// </summary>
        public void SetupStage()
        {
            // TODO: StageMonoBehaviourの生成
            // デッキと捨て山をFactoryで生成して紐付け
        }
    }
} 