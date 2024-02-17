using Shapes;
using UnityEngine;
using PygmyMonkey.ColorPalette;

public class PlanetRingGenerator : MonoBehaviour {

  public GameObject ringPrefab;

  [Range(1, 6)]
  public int ringCount = 1;

  public float ringMinThickness = 0.02f;
  public float ringMaxThickness = 0.1f;

  public float minGapBetween = 0f;
  public float maxGapBetween = 0.08f;

  public float startingRadius = 0.2f;
  public float maxRadius = 0.4f;

  public Vector3 minRotation = Vector3.zero;
  public Vector3 maxRotation = Vector3.zero;

  public void UpdateDisplay(){

    //use immediate mode when running in editor
    transform.DestroyChildren(!Application.isPlaying);

    var radius = startingRadius;

    for(var i = 0; i < ringCount; i++){
      var newRing = GameObject.Instantiate<GameObject>(ringPrefab, this.transform);
      var torus = newRing.GetComponent<Torus>();

      torus.Radius = radius;
      torus.Thickness = Random.Range(ringMinThickness, ringMaxThickness);

      radius += Random.Range(2f * torus.Thickness, 2f * torus.Thickness + Random.Range(minGapBetween, maxGapBetween));

      torus.Color = StageSevenManager.PlanetRingPalette.getRandomColor();

      if(radius >= maxRadius){
        break;
      }
    }

    transform.rotation = Quaternion.Euler(
      Random.Range(minRotation.x, maxRotation.x),
      Random.Range(minRotation.y, maxRotation.y),
      Random.Range(minRotation.z, maxRotation.z)
    );
  }
}