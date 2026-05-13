using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#region Summary
/// <summary>
/// 枠用 Image を Button と同じ親の兄弟として生成し、兄弟順で Button（同一オブジェクトの Image 含む）より奥に描画する。Rect は Button に追従する。毎フレームの追従は followRectInUpdate で切り替える。Button がヒエラルキ上で非表示になると枠の GameObject も非表示にする（逆方向の同期は行わない）。
/// </summary>
#endregion

[RequireComponent(typeof(Button))]
public class ButtonFrameSetter : MonoBehaviour
{
    #region Serialized Fields

    [Header("枠線の基本設定")]
    [SerializeField] private Color frameColor = Color.black;
    [SerializeField] private float frameThickness = 5f;

    [Header("角丸設定")]
    [SerializeField] private bool useRoundedFrame = false;
    [SerializeField] private Sprite roundedFrameSprite;

    [Header("枠線オブジェクト（自動生成）")]
    [SerializeField] private Image frameImage;

    [Header("追従")]
    [Tooltip("有効時のみ Update で Rect を同期する。無効時は OnEnable / ApplyFrame 時に一度合わせる。")]
    [SerializeField] private bool followRectInUpdate = false;

    #endregion

    #region Private Fields

    private RectTransform buttonRect;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (!ValidateButtonRect())
        {
            return;
        }

        EnsureFrameInstance();
    }

    private void OnEnable()
    {
        if (!ValidateButtonRect())
        {
            return;
        }

        EnsureFrameInstance();
        ApplyVisualSettings();
        SyncFrameGeometry();
        SyncFrameGameObjectActiveWithButton();
    }

    /// <summary>
    /// Button 側がヒエラルキ上で見えないときは枠も隠す（枠から Button へは触れない）。
    /// </summary>
    private void OnDisable()
    {
        SyncFrameGameObjectActiveWithButton();
    }

    private void Update()
    {
        // Inspector で追従がオフのときは毎フレームの同期を行わない
        if (!followRectInUpdate)
        {
            return;
        }

        // 親のレイアウトやアニメーションで Rect が変わる場合に追従する
        if (!ValidateButtonRect() || frameImage == null)
        {
            return;
        }

        SyncFrameGeometry();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 枠線を適用する（Inspector からも実行可能）。
    /// </summary>
    public void ApplyFrame()
    {
        if (!ValidateButtonRect())
        {
            return;
        }

        EnsureFrameInstance();
        ApplyVisualSettings();
        SyncFrameGeometry();
        SyncFrameGameObjectActiveWithButton();
    }

    #endregion

    #region Private Methods

    private bool ValidateButtonRect()
    {
        if (buttonRect == null)
        {
            buttonRect = GetComponent<RectTransform>();
        }

        return buttonRect != null;
    }

    /// <summary>
    /// Button と同じヒエラルキ上の表示状態に枠の GameObject を合わせる（枠側の操作では Button は変えない）。
    /// </summary>
    private void SyncFrameGameObjectActiveWithButton()
    {
        if (frameImage == null)
        {
            return;
        }

        // activeSelf だけだと親が非表示にしたとき枠が残るため、実際に描画されるかで揃える
        frameImage.gameObject.SetActive(gameObject.activeInHierarchy);
    }

    private void EnsureFrameInstance()
    {
        if (buttonRect.parent == null)
        {
            Debug.LogWarning("ButtonFrameSetter: RectTransform の親が無いため、枠を兄弟として配置できません。");
            return;
        }

        if (frameImage != null)
        {
            var frameT = frameImage.rectTransform;
            // Button の子に置かれている旧構成や、親がずれている場合は Button と同じ親へ付け直す
            if (frameT.parent != buttonRect.parent)
            {
                var insertIndex = buttonRect.GetSiblingIndex();
                frameT.SetParent(buttonRect.parent, false);
                frameT.SetSiblingIndex(insertIndex);
            }

            EnsureFrameLayoutIgnored(frameImage.gameObject);
            return;
        }

        var frameObj = new GameObject("Frame");
        var insert = buttonRect.GetSiblingIndex();
        frameObj.transform.SetParent(buttonRect.parent, false);
        frameObj.transform.SetSiblingIndex(insert);
        frameImage = frameObj.AddComponent<Image>();
        frameImage.raycastTarget = false;
        EnsureFrameLayoutIgnored(frameObj);
    }

    /// <summary>
    /// 親の Horizontal/Vertical/Grid Layout に枠が参加しないようにする。
    /// </summary>
    private static void EnsureFrameLayoutIgnored(GameObject frameRoot)
    {
        var layoutElement = frameRoot.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = frameRoot.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
    }

    private void ApplyVisualSettings()
    {
        if (frameImage == null)
        {
            return;
        }

        frameImage.color = frameColor;

        if (useRoundedFrame)
        {
            if (roundedFrameSprite != null)
            {
                frameImage.sprite = roundedFrameSprite;
                frameImage.type = Image.Type.Sliced;
            }
            else
            {
                Debug.LogWarning("Rounded Frame が選択されていますが、roundedFrameSprite が設定されていません。");
            }
        }
        else
        {
            frameImage.sprite = null;
            frameImage.type = Image.Type.Simple;
        }
    }

    private void SyncFrameGeometry()
    {
        if (frameImage == null || buttonRect.parent == null)
        {
            return;
        }

        var frameRect = frameImage.rectTransform;

        // 親の Graphic より手前に子が描画されるため、枠は Button と同じ親の兄弟にし、Button の直前の index に置く
        if (frameRect.parent != buttonRect.parent)
        {
            EnsureFrameInstance();
            if (frameRect.parent != buttonRect.parent)
            {
                return;
            }
        }

        var buttonIndex = buttonRect.GetSiblingIndex();
        var desiredFrameIndex = Mathf.Max(0, buttonIndex - 1);
        if (frameRect.GetSiblingIndex() != desiredFrameIndex)
        {
            frameRect.SetSiblingIndex(desiredFrameIndex);
        }

        // Button と同一レイアウト（親座標系）に合わせ、枠分だけ size を広げる
        frameRect.anchorMin = buttonRect.anchorMin;
        frameRect.anchorMax = buttonRect.anchorMax;
        frameRect.pivot = buttonRect.pivot;
        frameRect.anchoredPosition = buttonRect.anchoredPosition;
        frameRect.localRotation = buttonRect.localRotation;
        frameRect.localScale = buttonRect.localScale;
        frameRect.sizeDelta = buttonRect.sizeDelta + new Vector2(frameThickness, frameThickness);
    }

    #endregion
}

#if UNITY_EDITOR
#region Editor

[CustomEditor(typeof(ButtonFrameSetter))]
public class ButtonFrameSetterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var script = (ButtonFrameSetter)target;
        if (GUILayout.Button("枠線を適用"))
        {
            script.ApplyFrame();
        }
    }
}

#endregion
#endif
