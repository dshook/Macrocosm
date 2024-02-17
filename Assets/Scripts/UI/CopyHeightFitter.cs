using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
/// <summary>
///   Resizes a RectTransform to take a percentage size of its parent
/// </summary>
public class CopyHeightFitter : UIBehaviour
{
  public RectTransform copyFrom;

  [Tooltip("If true, use children instead of copy from reference")]
  public bool useFirstChild = false;

  public float padding = 0f;

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

  public virtual void LateUpdate()
  {
    if(useFirstChild && transform.childCount > 0){
      copyFrom = transform.GetChild(0).GetComponent<RectTransform>();
    }

    if(copyFrom == null){ return; }

    var newHeight = copyFrom.sizeDelta.y + padding;
    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
  }
}