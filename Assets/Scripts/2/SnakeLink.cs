using UnityEngine;

public class SnakeLink : MonoBehaviour {

  //Transforms to the previous and next members of the snake
  public Transform towardsHead;
  public Transform towardsTail;

  public float stretchFactor = 1f;

  void Update () {
    if(towardsHead == null || towardsTail == null){
      return;
    }

    transform.localPosition = Vector2.Lerp(towardsTail.localPosition, towardsHead.localPosition, 0.5f);

    var deltaVec = towardsHead.position - towardsTail.position;

    transform.rotation = Quaternion.LookRotation(Vector3.forward, deltaVec);

    transform.localScale = transform.localScale.SetY(stretchFactor * deltaVec.magnitude);
  }


}
