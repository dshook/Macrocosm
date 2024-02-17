using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
/// <summary>
///   Changes A rect transforms x & y position based off a percentage of parent size
/// </summary>
public class PercentageOfParentPositioner : UIBehaviour, ILayoutSelfController
{
  public Vector2 percentage = Vector2.one;

  public bool controlX = true;
  public bool controlY = true;

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

  private RectTransform m_ParentRect;
  private RectTransform parentRectTransform
  {
    get
    {
      if (m_ParentRect == null)
        m_ParentRect = transform.parent.GetComponent<RectTransform>();
      return m_ParentRect;
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

  float prevParentWidth;
  float prevParentHeight;
  /// <summary>
  /// Update the rect based on the delayed dirty.
  /// Got around issue of calling onValidate from OnEnable function.
  /// </summary>
  protected virtual void Update()
  {

    var parentWidth = parentRectTransform.rect.width;
    var parentHeight = parentRectTransform.rect.height;

    if(parentWidth != prevParentWidth || parentHeight != prevParentHeight){
      prevParentWidth = parentWidth;
      prevParentHeight = parentHeight;
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
    UpdateRect();
  }

  private void UpdateRect()
  {
    m_Tracker.Clear();

    if (!IsActive())
      return;

    if(controlX){
      m_Tracker.Add(this, rectTransform, DrivenTransformProperties.AnchoredPositionX);
    }
    if(controlY){
      m_Tracker.Add(this, rectTransform, DrivenTransformProperties.AnchoredPositionY);
    }

    var newX = controlX ? parentRectTransform.rect.width * percentage.x : rectTransform.anchoredPosition.x;
    var newY = controlY ? parentRectTransform.rect.height * percentage.y : rectTransform.anchoredPosition.y;
    rectTransform.anchoredPosition = new Vector2(newX, newY);
  }

  /// <summary>
  /// Method called by the layout system. Has no effect
  /// </summary>
  public virtual void SetLayoutHorizontal() { }

  /// <summary>
  /// Method called by the layout system. Has no effect
  /// </summary>
  public virtual void SetLayoutVertical() { }

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
    m_DelayedSetDirty = true;
  }

#endif
}