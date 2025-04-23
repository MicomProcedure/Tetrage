using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tetrage.UI
{
    public class CardView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer CardSpriteRenderer;
        [SerializeField] private SpriteRenderer frontSpriteRenderer;
        [SerializeField] private SpriteRenderer backSpriteRenderer;
        [SerializeField] private TextMeshProUGUI suitText;
        [SerializeField] private TextMeshProUGUI numberText;

        private void Start()
        {
            CardFlip();
        }

        private void Update()
        {

        }

        public void CardFlip()
        {
            //表裏を変更する
            CardSpriteRenderer.sprite = frontSpriteRenderer.sprite;
        }

    }
}

