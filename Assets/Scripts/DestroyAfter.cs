using UnityEngine;

public class DestroyAfter : MonoBehaviour {

  public float timeToLive = 1.5f;

  float timer = 0f;

  void Update () {
    timer += Time.deltaTime;

    //time to die
    if(timer > timeToLive){
      Destroy(this.gameObject);
    }
  }

}
