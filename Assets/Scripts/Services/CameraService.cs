using UnityEngine;
using strange.extensions.mediation.impl;

public class CameraService: View
{

  Camera cam;

  public Camera Cam {
    get { return cam; }
  }

  protected override void Awake(){
    base.Awake();

    cam = Camera.main;
  }

  void Update(){
    calculatedScreenRect = false;
  }

  bool calculatedScreenRect = false;
  Rect _cameraWorldRect;

  public Rect cameraWorldRect{
    get{
      if(!calculatedScreenRect){
        _cameraWorldRect = calculateCameraWorldRect(cam);

        calculatedScreenRect = true;
      }

      return _cameraWorldRect;
    }
  }

  //Only use for scripts that need to execute in edit mode
  public static Rect calculateCameraWorldRect(Camera cam){
    var upperRightScreen = new Vector3(Screen.width, Screen.height, 0);
    var upperRight = cam.ScreenToWorldPoint(upperRightScreen) - cam.transform.position;
    return new Rect(cam.transform.position.x - upperRight.x, cam.transform.position.y - upperRight.y, upperRight.x * 2, upperRight.y * 2);
  }

  public void ResetPositionAndSize(){
    cam.transform.position = Constants.defaultCameraPosition;
    cam.orthographicSize = Constants.defaultCameraOrthoSize;
  }

}

