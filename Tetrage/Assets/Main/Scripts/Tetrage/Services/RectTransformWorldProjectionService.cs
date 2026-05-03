using UnityEngine;

namespace Tetrage.Services
{
    /// <summary>
    /// UI上のTransform/RectTransform座標を、指定カメラのワールド座標へ変換するサービス。
    /// </summary>
    public static class RectTransformWorldProjectionService
    {
        #region Public Methods

        /// <summary>
        /// UIマーカーを、指定平面上のワールド座標へ射影する。
        /// </summary>
        public static bool TryProjectMarkerToWorldOnPlane(
            Transform marker,
            Camera worldCamera,
            Vector3 planePoint,
            Vector3 planeNormal,
            out Vector3 worldPosition)
        {
            worldPosition = planePoint;

            if (marker == null || worldCamera == null)
            {
                return false;
            }

            Vector2 screenPoint = GetMarkerScreenPoint(marker);
            Ray ray = worldCamera.ScreenPointToRay(screenPoint);
            Plane targetPlane = new Plane(planeNormal, planePoint);

            // UIスクリーン座標からワールド平面への投影が成立する場合のみ更新する。
            if (!targetPlane.Raycast(ray, out float enter))
            {
                return false;
            }

            worldPosition = ray.GetPoint(enter);
            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// マーカーのRectTransform/Transform位置をスクリーン座標として取得する。
        /// </summary>
        private static Vector2 GetMarkerScreenPoint(Transform marker)
        {
            var rectTransform = marker as RectTransform;
            if (rectTransform == null)
            {
                return RectTransformUtility.WorldToScreenPoint(null, marker.position);
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 markerCenter = (corners[0] + corners[2]) * 0.5f;

            return RectTransformUtility.WorldToScreenPoint(uiCamera, markerCenter);
        }

        #endregion
    }
}
