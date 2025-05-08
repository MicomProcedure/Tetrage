using NUnit.Framework;
using Tetrage.Models;
using UnityEngine;

namespace Tetrage.Core.Factories
{
    public class DeckFactory : MonoBehaviour
    {
        /* -------- 1. 唯一のインスタンスを公開 -------- */
        public static DeckFactory Instance { get; internal set; } // ここのinteralについていまいちわかってない。　テストがしやすい、とだけ

        /* -------- 2. Awake で重複チェック -------- */
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);             // 既に存在→この複製を破棄
                return;
            }

            Instance = this;                     // 初回生成
            //DontDestroyOnLoad(gameObject);       // シーンをまたいで保持したい場合
        }

        [SerializeField] private GameObject _cardPrefab; // Cardのプレハブ



    }
}
