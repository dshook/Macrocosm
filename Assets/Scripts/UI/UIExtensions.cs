using UnityEngine;
using UnityEngine.UI;

public static class UIExtensions{

  //Stupid hack to make the layout groups properly update their sizes after things are added to them :(
  //Don't think these are bulletproof though wiht the content size fitters
  // LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform); seems to work better actually
  public static void ForceUpdate(this HorizontalLayoutGroup hlg){
    Canvas.ForceUpdateCanvases();
    hlg.CalculateLayoutInputVertical();
    hlg.CalculateLayoutInputHorizontal();
    hlg.SetLayoutVertical();
    hlg.SetLayoutHorizontal();
  }

  public static void ForceUpdate(this VerticalLayoutGroup vlg){
    Canvas.ForceUpdateCanvases();
    vlg.CalculateLayoutInputHorizontal();
    vlg.CalculateLayoutInputVertical();
    vlg.SetLayoutHorizontal();
    vlg.SetLayoutVertical();
  }
}