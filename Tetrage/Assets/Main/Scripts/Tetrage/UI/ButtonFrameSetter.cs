using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Button))]
public class ButtonFrameSetter : MonoBehaviour
{
    [Header("枠線の設定")]
    [SerializeField] private Color frameColor = Color.black;
    [SerializeField] private float frameThickness = 5f;

    [Header("枠線オブジェクト（自動生成可）")]
    [SerializeField] private Image frameImage;

    private RectTransform buttonRect;

    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 枠線を適用する（Inspector上からも実行可能）
    /// </summary>
    public void ApplyFrame()
    {
        if (frameImage == null)
        {
            // 枠線オブジェクトを自動生成
            GameObject frameObj = new GameObject("Frame");
            frameObj.transform.SetParent(transform.parent, false);

            frameImage = frameObj.AddComponent<Image>();
            frameImage.raycastTarget = false; // クリックを邪魔しないように
        }

        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();

        // 枠線の見た目設定
        frameImage.color = frameColor;

        // RectTransform設定
        RectTransform frameRect = frameImage.rectTransform;
        frameRect.anchorMin = buttonRect.anchorMin;
        frameRect.anchorMax = buttonRect.anchorMax;
        frameRect.pivot = buttonRect.pivot;
        frameRect.sizeDelta = buttonRect.sizeDelta + new Vector2(frameThickness, frameThickness);
        frameRect.anchoredPosition = buttonRect.anchoredPosition;

        // ★ 枠線をボタンの1つ奥（behind）に固定
        int buttonIndex = transform.GetSiblingIndex();
        int frameIndex = Mathf.Max(0, buttonIndex - 1);
        frameImage.transform.SetSiblingIndex(frameIndex);
    }
}

#if UNITY_EDITOR
// Inspector上に「枠線を適用」ボタンを追加
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

