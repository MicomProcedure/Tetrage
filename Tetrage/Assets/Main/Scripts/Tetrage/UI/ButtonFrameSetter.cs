using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Button))]
public class ButtonFrameSetter : MonoBehaviour
{
    [Header("枠線の基本設定")]
    [SerializeField] private Color frameColor = Color.black;
    [SerializeField] private float frameThickness = 5f;

    [Header("角丸設定")]
    [SerializeField] private bool useRoundedFrame = false; // ← 角丸を使うか？
    [SerializeField] private Sprite roundedFrameSprite;     // ← 角丸用スプライト（スライス済み）

    [Header("枠線オブジェクト（自動生成）")]
    [SerializeField] private Image frameImage;

    private RectTransform buttonRect;

    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 枠線を適用する（Inspectorからも押せる）
    /// </summary>
    public void ApplyFrame()
    {
        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();

        if (frameImage == null)
        {
            // 枠線オブジェクト自動生成
            GameObject frameObj = new GameObject("Frame");
            frameObj.transform.SetParent(transform.parent, false);
            frameImage = frameObj.AddComponent<Image>();
            frameImage.raycastTarget = false;
        }

        // ---------- 見た目の設定 ----------
        frameImage.color = frameColor;

        if (useRoundedFrame)
        {
            // ◇◆ 角丸枠線を採用 ◆◇
            if (roundedFrameSprite != null)
            {
                frameImage.sprite = roundedFrameSprite;
                frameImage.type = Image.Type.Sliced; // ← 角丸に必要
            }
            else
            {
                Debug.LogWarning("Rounded Frame が選択されていますが、roundedFrameSprite が設定されていません。");
            }
        }
        else
        {
            // ◇◆ 通常の四角枠線 ◆◇
            frameImage.sprite = null;
            frameImage.type = Image.Type.Simple;
        }

        // ---------- RectTransform の設定 ----------
        RectTransform frameRect = frameImage.rectTransform;
        frameRect.anchorMin = buttonRect.anchorMin;
        frameRect.anchorMax = buttonRect.anchorMax;
        frameRect.pivot = buttonRect.pivot;

        frameRect.sizeDelta = buttonRect.sizeDelta + new Vector2(frameThickness, frameThickness);
        frameRect.anchoredPosition = buttonRect.anchoredPosition;

        // ボタンの1つ奥へ配置
        int buttonIndex = transform.GetSiblingIndex();
        int frameIndex = Mathf.Max(0, buttonIndex - 1);
        frameImage.transform.SetSiblingIndex(frameIndex);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ButtonFrameSetter))]
public class ButtonFrameSetterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ButtonFrameSetter script = (ButtonFrameSetter)target;
        if (GUILayout.Button("枠線を適用"))
        {
            script.ApplyFrame();
        }
    }
}
#endif
