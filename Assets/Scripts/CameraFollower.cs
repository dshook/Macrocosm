using UnityEngine;

public class CameraFollower : MonoBehaviour {

  public float followAmount = 1f;
	public Vector3 offset;

  Camera cam;
  void Awake() {
    cam = Camera.main;
  }

  void LateUpdate(){
    transform.position = Vector3.Lerp(Vector2.zero, cam.transform.position + offset, followAmount);
  }

}
