using Tetrage.Core.Contracts;
using UnityEngine.Assertions;

namespace Tetrage.Managers
{
    /// <summary>
    /// FieldSetupManagerで使用する依存性オブジェクトを管理するクラス
    /// 設定データ（FieldSetupSettings）と依存性を明確に分離するための責任分離設計
    /// </summary>
    public class FieldSetupDependencies
    {
        /// <summary>
        /// カードモデル生成用ファクトリ
        /// </summary>
        public ICardFactory CardFactory { get; }

        /// <summary>
        /// ステージモデル生成用ファクトリ
        /// </summary>
        public IStageFactory StageModelFactory { get; }

        /// <summary>
        /// プレイヤーモデル生成用ファクトリ
        /// </summary>
        public IPlayerFactory PlayerModelFactory { get; }

        /// <summary>
        /// カードパイル生成用ファクトリ
        /// </summary>
        public ICardPileFactory CardPileFactory { get; }

        /// <summary>
        /// FieldSetupDependenciesのコンストラクタ
        /// </summary>
        /// <param name="cardModelFactory">カードモデル生成用ファクトリ</param>
        /// <param name="stageModelFactory">ステージモデル生成用ファクトリ</param>
        /// <param name="playerModelFactory">プレイヤーモデル生成用ファクトリ</param>
        /// <param name="cardPileFactory">カードパイル生成用ファクトリ</param>
        public FieldSetupDependencies(
            ICardFactory cardFactory,
            IStageFactory stageModelFactory,
            IPlayerFactory playerModelFactory,
            ICardPileFactory cardPileFactory)
        {
            Assert.IsNotNull(cardFactory, "cardFactory が null です");
            Assert.IsNotNull(stageModelFactory, "stageModelFactory が null です");
            Assert.IsNotNull(playerModelFactory, "playerModelFactory が null です");
            Assert.IsNotNull(cardPileFactory, "cardPileFactory が null です");

            CardFactory = cardFactory;
            StageModelFactory = stageModelFactory;
            PlayerModelFactory = playerModelFactory;
            CardPileFactory = cardPileFactory;
        }
    }
}