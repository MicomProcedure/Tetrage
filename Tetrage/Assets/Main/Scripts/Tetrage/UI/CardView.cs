using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Tetrage.Core.Enums;
using Tetrage.Data;
using R3;

namespace Tetrage.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読して Dispose などの後片付けを行います。
        /// </summary>
        public event Action Destroyed;
        private readonly Subject<Unit> _clicked = new();

        private void OnDestroy()
        {
            Destroyed?.Invoke();
            _clicked.Dispose();
        }

        #region Serialized Fields

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMeshProUGUI suitText;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private Animator animator;

        [Header("Card Data")]
        [SerializeField] private CardImageMapper cardImageMapper;

        [Header("Suit Back Sprites (Player Only)")]
        [SerializeField] private Sprite spadeBackSprite;
        [SerializeField] private Sprite heartBackSprite;
        [SerializeField] private Sprite diamondBackSprite;
        [SerializeField] private Sprite clubBackSprite;

        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // 黄色っぽい色
        //[SerializeField] private float highlightIntensity = 1.2f; // ハイライト時の明るさ倍率(まだ使ってない)

        #endregion

        #region Private Fields

        private Color _originalColor;
        private bool _isHighlighted = false;
        private Suit _currentSuit;
        private int _currentNumber;
        private bool _isFaceUp = false;
        private bool _showSuitOnBack = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _originalColor = spriteRenderer.color;
        }

        #endregion

        #region Events

        public Observable<Unit> Clicked => _clicked;
        public event Action FlipAnimationHalfway;

        #endregion

        #region Click Handling

        public void OnPointerClick(PointerEventData e)
        {
            Debug.Log($"CardView: OnPointerClick {e.pointerId}");
            _clicked.OnNext(Unit.Default);
        }

        #endregion

        #region Animation

        public void PlayFlipAnimation() => animator.SetTrigger("FlipSuccess");
        public void ScaleUpAnimation() => animator.SetBool("IsInTmpPile", true);
        public void ScaleDownAnimation() => animator.SetBool("IsInTmpPile", false);

        // アニメーションイベントから呼び出されるメソッド
        public void OnFlipAnimationHalfway()
        {
            Debug.Log("OnFlipAnimationHalfway called");
            FlipAnimationHalfway?.Invoke();
        }

        #endregion

        #region Card Display (Presenterから呼び出される)

        /// <summary>
        /// スートシンボルを設定（CardPresenter互換用）
        /// テキストから逆推定してSuit情報を保持
        /// </summary>
        public void SetSuitSymbol(string symbol)
        {
            suitText.text = symbol;
            
            // シンボル文字列からSuitを推定
            if (symbol.Contains("♠"))
                _currentSuit = Suit.Spade;
            else if (symbol.Contains("♥"))
                _currentSuit = Suit.Heart;
            else if (symbol.Contains("♦"))
                _currentSuit = Suit.Diamond;
            else if (symbol.Contains("♣"))
                _currentSuit = Suit.Club;
            
            UpdateCardSprite();
        }

        /// <summary>
        /// 数字を設定してカード画像を更新
        /// </summary>
        public void SetNumber(int number)
        {
            _currentNumber = number;
            numberText.text = number.ToString();
            UpdateCardSprite();
        }

        /// <summary>
        /// カード情報を直接設定（推奨メソッド）
        /// SuitとNumberを直接受け取って画像を更新
        /// </summary>
        public void SetCardData(Suit suit, int number)
        {
            _currentSuit = suit;
            _currentNumber = number;
            
            // テキスト表示も更新
            if (cardImageMapper != null)
            {
                suitText.text = GetSuitSymbolWithColor(suit);
            }
            numberText.text = number.ToString();
            
            UpdateCardSprite();
        }

        /// <summary>
        /// カードを表向きにする
        /// </summary>
        public void ShowFace()
        {
            _isFaceUp = true;
            UpdateCardDisplay();
        }

        /// <summary>
        /// カードを裏向きにする
        /// </summary>
        public void ShowBack()
        {
            _isFaceUp = false;
            UpdateCardDisplay();
        }

        /// <summary>
        /// スート情報を含む裏面でカードを表示する（プレイヤーカード用）
        /// </summary>
        public void ShowBackWithSuit(Suit suit)
        {
            _isFaceUp = false;
            _currentSuit = suit;
            _showSuitOnBack = true; // スート付き裏面モード
            UpdateCardDisplay();
        }

        #endregion

        #region Highlight

        /// <summary>
        /// カードをハイライト状態にする
        /// </summary>
        public void Highlight()
        {
            _isHighlighted = true;
            UpdateCardDisplay();
        }

        /// <summary>
        /// カードのハイライト状態を解除する
        /// </summary>
        public void Unhighlight()
        {
            _isHighlighted = false;
            UpdateCardDisplay();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// カード画像の表示を更新（表/裏、ハイライト状態を反映）
        /// </summary>
        private void UpdateCardDisplay()
        {
            if (_isFaceUp)
            {
                // 表向き：カード画像を表示
                UpdateCardSprite();
                spriteRenderer.color = _isHighlighted ? highlightColor : Color.white;
                //suitText.enabled = true;
                //numberText.enabled = true;
            }
            else
            {
                // 裏向き：裏面画像を表示
                if (cardImageMapper != null)
                {
                    Sprite backSprite;
                    
                    // ▼ 追加: スート付き裏面モードの場合はスート別画像を使用
                    if (_showSuitOnBack)
                    {
                        backSprite = GetSuitBackSpriteLocal(_currentSuit);
                        // スート別画像が未設定の場合は通常の裏面にフォールバック
                        if (backSprite == null)
                        {
                            backSprite = cardImageMapper.GetCardBackSprite();
                        }
                    }
                    else
                    {
                        // 通常モード：Mapperからデフォルトの裏面を取得
                        backSprite = cardImageMapper.GetCardBackSprite();
                    }
                    
                    if (backSprite != null)
                    {
                        spriteRenderer.sprite = backSprite;
                    }
                }
                spriteRenderer.color = _isHighlighted ? highlightColor * 0.3f : Color.white;
                suitText.enabled = false;
                numberText.enabled = false;
            }
        }

        /// <summary>
        /// CardImageMapperから適切なカード画像を取得して設定
        /// </summary>
        private void UpdateCardSprite()
        {
            if (cardImageMapper == null)
            {
                Debug.LogWarning("CardImageMapper is not assigned to CardView.");
                return;
            }

            if (!_isFaceUp) return;

            var sprite = cardImageMapper.GetCardSprite(_currentSuit, _currentNumber);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"Card sprite not found for {_currentSuit} {_currentNumber}");
            }
        }
        /// <summary>
        /// CardViewに直接設定されたスート別裏面スプライトを取得
        /// </summary>
        private Sprite GetSuitBackSpriteLocal(Suit suit)
        {
            return suit switch
            {
                Suit.Spade => spadeBackSprite,
                Suit.Heart => heartBackSprite,
                Suit.Diamond => diamondBackSprite,
                Suit.Club => clubBackSprite,
                _ => null
            };
        }

        /// <summary>
        /// スートのシンボルと色を含むリッチテキスト文字列を取得
        /// </summary>
        private string GetSuitSymbolWithColor(Suit suit)
        {
            return suit switch
            {
                Suit.Spade => "<color=black>♠</color>",
                Suit.Heart => "<color=red>♥</color>",
                Suit.Diamond => "<color=red>♦</color>",
                Suit.Club => "<color=black>♣</color>",
                _ => "?"
            };
        }

        #endregion
    }
}