using UnityEngine;
using TMPro;
using Shapes;


[ExecuteInEditMode]
public class TransportShipDisplay : MonoBehaviour
{
  public Rectangle background;
  public Rectangle fillBar;

  public SpriteRenderer shipRenderer;

  public Color color = Color.gray;
  public Color fillColor;

  [Range(0f, 1f)]
  public float fillAmt = 0.5f;

  // void Awake()
  // {
  // }

  void Update()
  {

    if(background == null || fillBar == null || shipRenderer == null){ return; }

    background.Color = color;
    shipRenderer.color = color;

    fillBar.Height = background.Height * fillAmt;
    fillBar.Color = fillColor;

  }

}