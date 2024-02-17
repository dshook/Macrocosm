using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class PositionSortOrder : MonoBehaviour
{
  [SerializeField]
  SpriteRenderer spriteRenderer;

  [SerializeField]
  [Tooltip("Sets the sort order based on y position. Make sure the pivot is set right")]
  public bool dynamicOrder = true;

  void Start()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
  }

  private void LateUpdate()
  {
      //Takes the current y position and multiplies it by -100 to capture differences up to 3 decimals
      if (dynamicOrder && spriteRenderer.isVisible)
      {
        spriteRenderer.sortingOrder = (int) (transform.position.y * -100);
      }
  }
}
