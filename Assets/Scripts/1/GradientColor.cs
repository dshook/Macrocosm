using UnityEngine;
using Shapes;

[RequireComponent(typeof(Quad))]
[ExecuteInEditMode]
public class GradientColor : MonoBehaviour {

  public Color startColor = Color.red;
  public Color endColor = Color.blue;

  Color _startColor;
  Color _endColor;

  Quad quad;

  public void Start () {
    quad = GetComponent<Quad>();
    UpdateColors();
  }

  void Update(){
    if(_startColor != startColor || _endColor != endColor){
      UpdateColors();
    }
  }

  void UpdateColors(){

    quad.ColorA = startColor;
    quad.ColorD = startColor;
    quad.ColorB = endColor;
    quad.ColorC = endColor;

    _startColor = startColor;
    _endColor = endColor;
  }
}