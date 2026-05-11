using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Tetrage.Core.Enums;
using Tetrage.Data;
using R3;
using System.Collections.Generic;
using Tetrage.Audio;
namespace Tetrage.UI
{
    public class CardView : MonoBehaviour
    {
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読して Dispose などの後片付けを行います。
        /// </summary>
        public Observable<Unit> Destroyed => _destroyed;
        private readonly Subject<Unit> _destroyed = new();
        private readonly Subject<Unit> _flipAnimationHalfway = new();
        private readonly Subject<Unit> _flipAnimationCompleted = new();
        private CompositeDisposable _disposables = new();

        private void OnDestroy()
        {
            _destroyed.OnNext(Unit.Default);

            _flipAnimationCompleted.OnCompleted();
            _flipAnimationHalfway.OnCompleted();
            _destroyed.OnCompleted();

            _disposables.Dispose();
        }

        #region Serialized Fields

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private ClickableMB cardClickableMB;

        [Header("Card Data")]
        [SerializeField] private CardImageMapper cardImageMapper;

        [Header("Suit Back Images (Player Only)")]
        [SerializeField] private GameObject spadeBackSprite;
        [SerializeField] private GameObject heartBackSprite;
        [SerializeField] private GameObject diamondBackSprite;
        [SerializeField] private GameObject clubBackSprite;

        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // 黄色っぽい色
        //[SerializeField] private float highlightIntensity = 1.2f; // ハイライト時の明るさ倍率(まだ使ってない)

        #endregion

        #region Private Fields

        private Color _originalColor;
        private bool _isHighlighted = false;
        private Suit _currentSuit;
        private int _currentNumber;
        private List<GameObject> _suitBackImages = new List<GameObject>();
        private bool _isFlipAnimationInProgress = false;
        public bool IsFlipAnimationInProgress => _isFlipAnimationInProgress;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!ValidateSerializedFields())
            {
                enabled = false;
                return;
            }

            _originalColor = spriteRenderer.color;

            _suitBackImages.Add(spadeBackSprite);
            _suitBackImages.Add(heartBackSprite);
            _suitBackImages.Add(diamondBackSprite);
            _suitBackImages.Add(clubBackSprite);

        }

        void Start(){
            DisableSuitBackUI();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateSerializedFields();
        }
#endif

        #endregion

        #region Events

        public Observable<Unit> Clicked => cardClickableMB.Clicked;
        public Observable<Unit> FlipAnimationHalfway => _flipAnimationHalfway;
        public Observable<Unit> FlipAnimationCompleted => _flipAnimationCompleted;

        #endregion

        #region Animation

        public void PlayFlipAnimation()
        {
            _isFlipAnimationInProgress = true;
            animator.SetTrigger("FlipSuccess");

        }
        public void ScaleUpAnimation() => animator.SetBool("IsInTmpPile", true);
        public void ScaleDownAnimation() => animator.SetBool("IsInTmpPile", false);

        // アニメーションイベントから呼び出されるメソッド
        public void OnFlipAnimationHalfway()
        {
            _flipAnimationHalfway.OnNext(Unit.Default);
        }

        public void OnFlipAnimationCompleted()
        {
            _isFlipAnimationInProgress = false;
            _flipAnimationCompleted.OnNext(Unit.Default);
        }

        #endregion

        #region Card Display (Presenterから呼び出される)

        /// <summary>
        /// カード情報を直接設定（推奨メソッド）
        /// SuitとNumberを直接受け取って画像を更新
        /// </summary>
        public void SetCardData(Suit suit, int number)
        {
            _currentSuit = suit;
            _currentNumber = number;
        
            
            UpdateCardSprite();
        }

        /// <summary>
        /// 引数の状態に応じてカードの表示状態を更新する
        /// </summary>
        /// <param name="isFaceUp"></param>
        /// <param name="isSuitVisible"></param>
        public void SetFlip(bool isFaceUp, bool isSuitVisible = false)
        {
            if (isFaceUp)
            {
                ShowFace();
            }
            else
            {
                ShowBack();
            }

            // カードが裏向きかつスート可視状態の場合はスート別裏面UIを表示
            if (!isFaceUp && isSuitVisible)
            {
                EnableSuitBackUI(_currentSuit);
            }
            else
            {
                DisableSuitBackUI();
            }
        }

        /// <summary>
        /// 裏面スートUIの表示状態のみを更新する
        /// </summary>
        public void SetBackSuitUI(bool isVisible)
        {
            if (isVisible)
            {
                EnableSuitBackUI(_currentSuit);
                return;
            }

            DisableSuitBackUI();
        }

        /// <summary>
        /// カードを表向き表示にする
        /// </summary>
        public void ShowFace()
        {
            // 表向き：カード画像を表示
            DisableSuitBackUI();
            UpdateCardSprite();
            spriteRenderer.color = _isHighlighted ? highlightColor : Color.white;
        }

        /// <summary>
        /// カードを裏向き表示にする
        /// </summary>
        public void ShowBack()
        {                
            if (cardImageMapper != null)
            {
                // 裏面スプライトを表示
                var backSuitSprite = cardImageMapper.GetCardBackSprite();
                if (backSuitSprite != null)
                {
                    spriteRenderer.sprite = backSuitSprite;
                }
            }
            spriteRenderer.color = _isHighlighted ? highlightColor * 0.3f : Color.white;
        }

        /// <summary>
        /// スート情報を含む裏面でカードを表示する（プレイヤーカード用）
        /// </summary>
        public void ShowBackWithSuit(Suit suit)
        {
            _currentSuit = suit;
            DisableSuitBackUI();
            EnableSuitBackUI(suit);
            ShowBack();
        }

        /// <summary>
        /// カードのレイヤー順を設定する
        /// </summary>
        /// <param name="order"></param>
        public void SetOrderInLayer(int order)
        {
            spriteRenderer.sortingOrder = order;
        }

        #endregion

        #region Highlight

        /// <summary>
        /// カードをハイライト状態にする
        /// </summary>
        public void Highlight()
        {
            _isHighlighted = true;
        }

        /// <summary>
        /// カードのハイライト状態を解除する
        /// </summary>
        public void Unhighlight()
        {
            _isHighlighted = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// スート別裏面画像を無効にする
        /// </summary>
        private void DisableSuitBackUI()
        {
            foreach (var suitBackImage in _suitBackImages){
                suitBackImage.SetActive(false);
            }
        }

        /// <summary>
        /// スート別裏面画像を有効にする
        /// </summary>
        /// <param name="suit"></param>
        private void EnableSuitBackUI(Suit suit)
        {
            GetSuitBackImageLocal(suit).SetActive(true);
        }

  

        /// <summary>
        /// CardImageMapperから適切なカード画像を取得して設定
        /// </summary>
        public void UpdateCardSprite()
        {
            if (cardImageMapper == null)
            {
                Debug.LogWarning("CardImageMapper is not assigned to CardView.");
                return;
            }

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
        private GameObject GetSuitBackImageLocal(Suit suit)
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

        #region Validation
        private bool ValidateSerializedFields()
        {
            var isValid = true;

            if (spriteRenderer == null)
            {
                Debug.LogError($"{name}: SpriteRenderer が設定されていません。", this);
                isValid = false;
            }

            if (animator == null)
            {
                Debug.LogError($"{name}: Animator が設定されていません。", this);
                isValid = false;
            }

            if (cardImageMapper == null)
            {
                Debug.LogError($"{name}: CardImageMapper が設定されていません。", this);
                isValid = false;
            }

            // Player Only とありますが、現状は裏表示時にスート別裏面を使うため必須です。
            if (spadeBackSprite == null || heartBackSprite == null || diamondBackSprite == null || clubBackSprite == null)
            {
                Debug.LogError($"{name}: SuitBackImages（Spade/Heart/Diamond/Club）が設定されていません。", this);
                isValid = false;
            }

            if (cardClickableMB == null)
            {
                Debug.LogError($"{name}: CardClickableMB が設定されていません。", this);
                isValid = false;
            }

            return isValid;
        }
        #endregion

        #endregion
    }
}