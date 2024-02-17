using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// [AddComponentMenu("Layout/Aspect Ratio Fitter", 142)]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
/// <summary>
///   Resizes a RectTransform to fit a specified aspect ratio.
/// </summary>
public class AspectRatioFitterCustom : UIBehaviour, ILayoutSelfController
{
    /// <summary>
    /// Specifies a mode to use to enforce an aspect ratio.
    /// </summary>
    public enum AspectMode
    {
        /// <summary>
        /// The aspect ratio is not enforced
        /// </summary>
        None,
        /// <summary>
        /// Changes the height of the rectangle to match the aspect ratio.
        /// </summary>
        WidthControlsHeight,
        /// <summary>
        /// Changes the width of the rectangle to match the aspect ratio.
        /// </summary>
        HeightControlsWidth,
        /// <summary>
        /// Sizes the rectangle such that it's fully contained within the parent rectangle.
        /// </summary>
        FitInParent,
        /// <summary>
        /// Sizes the rectangle such that the parent rectangle is fully contained within.
        /// </summary>
        EnvelopeParent
    }

    [SerializeField] private AspectMode m_AspectMode = AspectMode.None;

    /// <summary>
    /// The mode to use to enforce the aspect ratio.
    /// </summary>
    public AspectMode aspectMode { get { return m_AspectMode; } set { if (SetPropertyUtility.SetStruct(ref m_AspectMode, value)) SetDirty(); } }

    [SerializeField] private float m_AspectRatio = 1;

    /// <summary>
    /// The aspect ratio to enforce. This means width divided by height.
    /// </summary>
    public float aspectRatio { get { return m_AspectRatio; } set { if (SetPropertyUtility.SetStruct(ref m_AspectRatio, value)) SetDirty(); } }

    [SerializeField] private float m_MinSize = 1;
    /// <summary>
    /// The minimum size the object will resize to
    /// </summary>
    public float minSize { get { return m_MinSize; } set { if (SetPropertyUtility.SetStruct(ref m_MinSize, value)) SetDirty(); } }

    [SerializeField] private float m_MaxSize = 1;
    /// <summary>
    /// The maximum size the object will resize to
    /// </summary>
    public float maxSize { get { return m_MaxSize; } set { if (SetPropertyUtility.SetStruct(ref m_MaxSize, value)) SetDirty(); } }

    [System.NonSerialized]
    private RectTransform m_Rect;

    // This "delayed" mechanism is required for case 1014834.
    private bool m_DelayedSetDirty = false;

    private RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    private DrivenRectTransformTracker m_Tracker;

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDisable()
    {
        m_Tracker.Clear();
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    /// <summary>
    /// Update the rect based on the delayed dirty.
    /// Got around issue of calling onValidate from OnEnable function.
    /// </summary>
    protected virtual void Update()
    {
        if (m_DelayedSetDirty)
        {
            m_DelayedSetDirty = false;
            SetDirty();
        }
    }

    /// <summary>
    /// Function called when this RectTransform or parent RectTransform has changed dimensions.
    /// </summary>
    protected override void OnRectTransformDimensionsChange()
    {
        UpdateRect();
    }

    private void UpdateRect()
    {
        if (!IsActive())
            return;

        m_Tracker.Clear();

        switch (m_AspectMode)
        {
#if UNITY_EDITOR
            case AspectMode.None:
            {
                if (!Application.isPlaying)
                    m_AspectRatio = Mathf.Clamp(rectTransform.rect.width / rectTransform.rect.height, 0.001f, 1000f);

                break;
            }
#endif
            case AspectMode.HeightControlsWidth:
            {
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
                var newWidth = Mathf.Clamp(rectTransform.rect.height * m_AspectRatio, minSize, maxSize);
                var newHeight = Mathf.Clamp(newWidth / m_AspectRatio, minSize, maxSize);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
                break;
            }
            case AspectMode.WidthControlsHeight:
            {
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
                var newHeight = Mathf.Clamp(rectTransform.rect.width / m_AspectRatio, minSize, maxSize);
                var newWidth = Mathf.Clamp(newHeight * m_AspectRatio, minSize, maxSize);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                break;
            }
            case AspectMode.FitInParent:
            case AspectMode.EnvelopeParent:
            {
                m_Tracker.Add(this, rectTransform,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.SizeDeltaY);

                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;

                Vector2 sizeDelta = Vector2.zero;
                Vector2 parentSize = GetParentSize();
                if ((parentSize.y * aspectRatio < parentSize.x) ^ (m_AspectMode == AspectMode.FitInParent))
                {
                    sizeDelta.y = Mathf.Clamp(GetSizeDeltaToProduceSize(parentSize.x / aspectRatio, 1), minSize, maxSize);
                }
                else
                {
                    sizeDelta.x = Mathf.Clamp(GetSizeDeltaToProduceSize(parentSize.y * aspectRatio, 0), minSize, maxSize);
                }
                rectTransform.sizeDelta = sizeDelta;

                break;
            }
        }
    }

    private float GetSizeDeltaToProduceSize(float size, int axis)
    {
        return size - GetParentSize()[axis] * (rectTransform.anchorMax[axis] - rectTransform.anchorMin[axis]);
    }

    private Vector2 GetParentSize()
    {
        RectTransform parent = rectTransform.parent as RectTransform;
        if (!parent)
            return Vector2.zero;
        return parent.rect.size;
    }

    /// <summary>
    /// Method called by the layout system. Has no effect
    /// </summary>
    public virtual void SetLayoutHorizontal() {}

    /// <summary>
    /// Method called by the layout system. Has no effect
    /// </summary>
    public virtual void SetLayoutVertical() {}

    /// <summary>
    /// Mark the AspectRatioFitter as dirty.
    /// </summary>
    protected void SetDirty()
    {
        UpdateRect();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        m_AspectRatio = Mathf.Clamp(m_AspectRatio, 0.001f, 1000f);
        m_DelayedSetDirty = true;
    }

#endif

    internal static class SetPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}