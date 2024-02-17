using UnityEngine;

//No bars or anything, just set update scale based on aspect ratio
[ExecuteInEditMode]
public class WorldAspectRatioScalerDumb : MonoBehaviour
{
  public enum VectorComponent{
    X, Y, Z
  }

  public VectorComponent source;
  public VectorComponent dest;

  public float ratio;

  void Start()
  {
    UpdateCrop();
  }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
  void Update(){
    UpdateCrop();
  }
#endif

  // Call this method if your window size or target aspect change.
  public void UpdateCrop()
  {
    var sourceValue = 0f;
    if(source == VectorComponent.X){
      sourceValue = transform.localScale.x;
    } else if(source == VectorComponent.Y){
      sourceValue = transform.localScale.y;
    } else if(source == VectorComponent.Z){
      sourceValue = transform.localScale.z;
    }

    var destValue = sourceValue * ratio;

    if(dest == VectorComponent.X){
      transform.localScale = new Vector3(destValue, transform.localScale.y, transform.localScale.z);
    } else if(dest == VectorComponent.Y){
      transform.localScale = new Vector3(transform.localScale.x, destValue, transform.localScale.z);
    } else if(dest == VectorComponent.Z){
      transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, destValue);
    }
  }
}