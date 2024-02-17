using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ShaderChanger : MonoBehaviour {

	public float changeAmt = 1.0f;
  public string floatPropName;
  public bool useShared = false;
  public bool animateWhilePaused = false;

  Renderer rend;

	void Start () {
    rend = GetComponent<Renderer>();
	}

	void Update () {
    if(!string.IsNullOrEmpty(floatPropName)){
      var cur = rend.material.GetFloat(floatPropName);
      var newAmt = cur + changeAmt * (animateWhilePaused ? Time.unscaledDeltaTime : Time.deltaTime);
      if(useShared){
        rend.sharedMaterial.SetFloat(floatPropName, newAmt);
      }else{
        rend.material.SetFloat(floatPropName, newAmt);
      }
    }
	}

}
