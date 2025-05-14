using UnityEngine;
using System.Collections;

namespace Tetrage.Animations
{
    /// <summary>
    /// アニメーション関連のヘルパークラス
    /// </summary>
    public class AnimationHelper : MonoBehaviour
    {
        private static AnimationHelper _instance;
        public static AnimationHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AnimationHelper");
                    _instance = go.AddComponent<AnimationHelper>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// 指定されたGameObjectをA地点からB地点まで移動させます
        /// </summary>
        /// <param name="target">移動させるGameObject</param>
        /// <param name="startPosition">開始位置</param>
        /// <param name="endPosition">終了位置</param>
        /// <param name="duration">移動にかかる時間（秒）</param>
        public void MoveTo(GameObject target, Vector3 startPosition, Vector3 endPosition, float duration = 1.0f)
        {
            StartCoroutine(MoveToCoroutine(target, startPosition, endPosition, duration));
        }

        /// <summary>
        /// 指定されたGameObjectをA地点からB地点までイージングをかけて移動させます
        /// </summary>
        /// <param name="target">移動させるGameObject</param>
        /// <param name="startPosition">開始位置</param>
        /// <param name="endPosition">終了位置</param>
        /// <param name="duration">移動にかかる時間（秒）</param>
        public void MoveToWithEasing(GameObject target, Vector3 startPosition, Vector3 endPosition, float duration = 1.0f)
        {
            StartCoroutine(MoveToWithEasingCoroutine(target, startPosition, endPosition, duration));
        }

        private IEnumerator MoveToCoroutine(GameObject target, Vector3 startPosition, Vector3 endPosition, float duration)
        {
            if (target == null) yield break;

            float elapsedTime = 0f;
            target.transform.position = startPosition;

            while (elapsedTime < duration)
            {
                if (target == null) yield break;

                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                
                // 線形補間で位置を計算
                target.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                
                yield return null;
            }

            // 確実に終了位置に移動させる
            if (target != null)
            {
                target.transform.position = endPosition;
            }
        }

        private IEnumerator MoveToWithEasingCoroutine(GameObject target, Vector3 startPosition, Vector3 endPosition, float duration)
        {
            if (target == null) yield break;

            float elapsedTime = 0f;
            target.transform.position = startPosition;

            while (elapsedTime < duration)
            {
                if (target == null) yield break;

                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                
                // イージング関数を適用（easeInOutQuad）
                t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                
                // 補間で位置を計算
                target.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                
                yield return null;
            }

            // 確実に終了位置に移動させる
            if (target != null)
            {
                target.transform.position = endPosition;
            }
        }
    }
} 