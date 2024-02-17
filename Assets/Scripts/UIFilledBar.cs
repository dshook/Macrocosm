using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode]
public class UIFilledBar : MonoBehaviour
{
  public MaskableGraphic background;
  public MaskableGraphic fill;
  public RectTransform fillRectTransform;
  public TMP_Text labelText;

  public Color color = Colors.lightGray;
  public Color bgColor = Colors.mediumGray;

  [Range(0f, 1f)]
  public float fillAmt = 0.5f;

  RectTransform rectTransform;

  // [Range(0, 20f)]
  // public float fillPadding = 0f;

  public string label;
  public bool manuallyUpdateLabel = false;


  void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
  }

  void Update()
  {
    if(background == null || fill == null){ return; }

    background.color = bgColor;
    fill.color = color;

    /** Old stretchy way of doing it
    fillRectTransform.anchorMin = Vector2.zero;
    fillRectTransform.anchorMax = Vector2.up;
    fillRectTransform.pivot = new Vector2(0, 0.5f);
    fillRectTransform.anchoredPosition = new Vector2(fillPadding, 0f);

    fillRectTransform.sizeDelta = new Vector2((rectTransform.rect.width - fillPadding * 2) * fillAmt, -fillPadding * 2);
    */

    //New slidy way of doing it
    fillRectTransform.anchoredPosition = new Vector2(-fillRectTransform.rect.width * (1 - fillAmt), 0);


    if(labelText != null && !manuallyUpdateLabel){
      if(string.IsNullOrEmpty(label)){
        labelText.gameObject.SetActive(false);
      }else{
        labelText.gameObject.SetActive(true);
        labelText.text = label;
      }
    }
  }

}