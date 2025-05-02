using Tetrage.Models;
using UnityEngine;

namespace Tetrage.UI
{
    public class CardPileView : MonoBehaviour
    {
        [SerializeField] private CardPile _model; // CardPileの純粋モデルへの参照

        void Start()
        {
            // モデルのイベントから、Viewの更新を行う

        }

    }
}
