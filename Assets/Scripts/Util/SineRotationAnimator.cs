
using UnityEngine;

//Rotates something back and forth
public class SineRotationAnimator : MonoBehaviour {
  public float period = 2f;
  public Vector3 maxRotation = Vector3.zero;

  float accum = 0f;

  void Update(){
    accum += Time.deltaTime;

    var sinAmp = Mathf.Sin((accum * (Mathf.PI / 2f)) / period);
    var rot = Quaternion.Euler( maxRotation * sinAmp );

    transform.rotation = rot;
  }
}