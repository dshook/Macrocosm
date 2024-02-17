using PygmyMonkey.ColorPalette;
using Shapes;
using UnityEngine;

public class BgStar : MonoBehaviour {
  public ColorFader colorFader;
  public ShapeRenderer spriteRenderer;

  public void UpdateDisplay(ColorPalette palette){
    spriteRenderer.Color = palette.getColorAtIndex(7);
  }
}