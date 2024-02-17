 using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CounterSafeAreaBottom : MonoBehaviour
{

  public RectTransform safeArea;

  RectTransform rt;

  private void Start()
  {
    rt = GetComponent<RectTransform>();
  }


  private void Update()
  {
    rt.offsetMin = new Vector2(0, -safeArea.offsetMin.y);
  }
}