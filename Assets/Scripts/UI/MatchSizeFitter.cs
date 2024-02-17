using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
/// <summary>
///   Resizes a RectTransform to take a percentage size of its parent
/// </summary>
public class MatchSizeFitter : UIBehaviour, ILayoutSelfController
{
  public RectTransform transformToMatch;

  public Vector2 padding = Vector2.zero;

  public Vector2 minSize = Vector2.zero;

  public bool controlWidth = true;
  public bool controlHeight = true;

  private RectTransform m_Rect;
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
  private bool m_DelayedSetDirty = false;

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

  float prevMatchedWidth;
  float prevMatchedHeight;
  /// <summary>
  /// Update the rect based on the delayed dirty.
  /// Got around issue of calling onValidate from OnEnable function.
  /// </summary>
  protected virtual void LateUpdate()
  {
    if(transformToMatch == null){ return; }

    var matchedWidth = transformToMatch.rect.width;
    var matchedHeight = transformToMatch.rect.height;

    if(controlWidth && matchedWidth != prevMatchedWidth){
      prevMatchedWidth = matchedWidth;
      m_DelayedSetDirty = true;
    }
    if(controlHeight && matchedHeight != prevMatchedHeight){
      prevMatchedHeight = matchedHeight;
      m_DelayedSetDirty = true;
    }

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
    // UpdateRect(); stack overflow lol
  }

  private void UpdateRect()
  {
    m_Tracker.Clear();

    if (!IsActive() || transformToMatch == null)
      return;

    if(controlWidth){
      m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);

      var newWidth = transformToMatch.rect.width + padding.x;
      newWidth = Mathf.Clamp(newWidth, minSize.x, newWidth);
      rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }

    if(controlHeight){
      m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);

      var newHeight = transformToMatch.rect.height + padding.y;
      newHeight = Mathf.Clamp(newHeight, minSize.y, newHeight);
      rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
    }
  }

  /// <summary>
  /// Method called by the layout system. Has no effect
  /// </summary>
  public virtual void SetLayoutHorizontal() { }

  /// <summary>
  /// Method called by the layout system. Has no effect
  /// </summary>
  public virtual void SetLayoutVertical() { }

  protected void SetDirty()
  {
    UpdateRect();
  }

#if UNITY_EDITOR
  protected override void OnValidate()
  {
    m_DelayedSetDirty = true;
  }

#endif
}