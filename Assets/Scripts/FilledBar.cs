using UnityEngine;
using TMPro;
using Shapes;


[ExecuteInEditMode]
public class FilledBar : MonoBehaviour
{
  public Line background;
  public Line fill;
  public TMP_Text labelText;

  public Color color = Color.gray;
  public Color bgColor = Colors.mediumGray;

  [Range(0f, 1f)]
  public float fillAmt = 0.5f;

  public float length = 1f;

  public float lineThickness = 0.2f;
  public float fillPadding = 0.08f;

  public string label;
  public bool manuallyUpdateLabel = false;


  void Awake()
  {
  }

  void Update()
  {

    if(background == null || fill == null){ return; }

    background.Color = bgColor;
    fill.Color = color;

    background.Thickness = lineThickness;
    fill.Thickness = lineThickness - fillPadding;

    fill.Dashed = true;
    fill.DashSpace = DashSpace.Meters;
    fill.DashSnap = DashSnapping.Off;

    background.Start = fill.Start = new Vector3(-length / 2f, 0, 0);
    background.End = new Vector3(length / 2f, 0, 0);
    fill.End = background.End;


    fill.DashSize = fill.Thickness + length;
    fill.DashSpacing = fill.DashSize * 3f;

    //Meant for thicknesses < 1f
    fill.DashOffset = Mathf.Lerp(-fill.Thickness * 2.25f, -fill.Thickness / 8f, fillAmt);

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