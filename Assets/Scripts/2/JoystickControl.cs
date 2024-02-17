using strange.extensions.mediation.impl;
using UnityEngine;

public class JoystickControl : View {
  [Inject] InputService input {get; set;}

  public RectTransform rectTransform;
  public RectTransform control;

  public bool Dragging { get{ return dragging; }}
  bool dragging = false;
  float radius;

  int fingerId = -1;

  Camera worldCamera;

  public Vector2 direction {
    get{
      return control.anchoredPosition.normalized;
    }
  }

  protected override void Awake () {
    base.Awake();

    worldCamera = GetComponentInParent<Canvas>().worldCamera;
    radius = rectTransform.rect.width / 2;
  }

  void Update(){
    var touchDown = input.GetTouchDown();
    if(touchDown != null){
      fingerId = touchDown.Item2;
      dragging = true;
    }

    var touchUp = input.GetTouchUp(false);
    if(touchUp != null && touchUp.Item2 == fingerId){
      StopDragging();
    }

    if(dragging){
      UpdateControl();
    }else{
      control.anchoredPosition = Vector2.Lerp(control.anchoredPosition, Vector2.zero, 0.5f);
    }
  }

  void StopDragging(){
    dragging = false;
    fingerId = -1;
  }

  void UpdateControl(){
    var touchPosition = input.GetTouchScreenPosition(fingerId);
    if(touchPosition == null){
      StopDragging();
      return;
    }

    Vector2 localpoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, touchPosition.Value, worldCamera, out localpoint);
    control.localPosition = localpoint;

    ConstrainControl();
  }

  void ConstrainControl(){
    if(Vector2.Distance(Vector2.zero, control.anchoredPosition) >= radius){
      control.anchoredPosition = radius * control.anchoredPosition.normalized;
    }
  }


  public void Reset(){
    control.anchoredPosition = Vector2.zero;
  }

}