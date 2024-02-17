using UnityEngine;

[ExecuteInEditMode]
public class WorldAspectRatioScaler : MonoBehaviour
{

  // Set this to your target aspect ratio, eg. (16, 9) or (4, 3). (or iphone 6 scale)
  public Vector2 targetAspect = new Vector2(750, 1334);

  public bool flip = false;

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
    // Determine ratios of screen/window & target, respectively.
    float screenRatio = Screen.width / (float)Screen.height;
    float targetRatio = targetAspect.x / targetAspect.y;

    if (Mathf.Approximately(screenRatio, targetRatio))
    {
      // Screen or window is the target aspect ratio: use the whole area.
      transform.localScale = Vector3.one;
      // _camera.rect = new Rect(0, 0, 1, 1);
    }
    else if (screenRatio > targetRatio)
    {
      // Screen or window is wider than the target: pillarbox.
      float normalizedWidth = targetRatio / screenRatio;
      float barThickness = (1f - normalizedWidth) / 2f;
      //Because we're in portrait mode always we can set the scale to one and
      //we'll get the height maxed out with empty space on the horizontal sides
      if(flip){
        transform.localScale = new Vector3(normalizedWidth, normalizedWidth, 1);
      }else{
        transform.localScale = Vector3.one;
      }
      // transform.localScale = new Vector3(normalizedWidth, normalizedWidth, 1);
      // _camera.rect = new Rect(barThickness, 0, normalizedWidth, 1);
    }
    else
    {
      // Screen or window is narrower than the target: letterbox.
      float normalizedHeight = screenRatio / targetRatio;
      float barThickness = (1f - normalizedHeight) / 2f;
      // _camera.rect = new Rect(0, barThickness, 1, normalizedHeight);
      if(flip){
        transform.localScale = Vector3.one;
      }else{
        transform.localScale = new Vector3(normalizedHeight, normalizedHeight, 1);
      }
    }
  }
}