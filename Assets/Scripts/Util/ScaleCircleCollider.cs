using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(CircleCollider2D))]
public class ScaleCircleCollider : MonoBehaviour
{
  public float minScaledColliderSize = 1f;

  CircleCollider2D circleCollider;

  float origSize = 0f;

  void Awake(){
    circleCollider = GetComponent<CircleCollider2D>();
    origSize = circleCollider.radius;
  }

  void LateUpdate()
  {
    var effectiveSize = transform.lossyScale.x * circleCollider.radius;
    var minEffectiveRadius = minScaledColliderSize;

    if(effectiveSize <= minEffectiveRadius || Mathf.Approximately(effectiveSize, minEffectiveRadius)){
      circleCollider.radius = (minScaledColliderSize / transform.lossyScale.x);
    }else{
      circleCollider.radius = origSize;
    }
  }
}
