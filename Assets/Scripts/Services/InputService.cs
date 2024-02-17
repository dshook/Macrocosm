using System;
using System.Collections.Generic;
using System.Linq;
using strange.extensions.mediation.impl;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputService : View
{
  [Inject] TutorialModel tutorialModel { get; set; }

  [Inject] DragStartedSignal dragStarted {get; set;}
  [Inject] DragEndedSignal dragEnded {get; set;}
  [Inject] ClickSignal clickSignal {get; set;}
  [Inject] CameraService cameraService {get; set;}

  public float mouseZoomStep = 1f;
  public float touchZoomStep = 0.05f;

  public bool debug = false;

  bool startedTouchOnGameObject = false;

  //Drag stuff
  const float dragTriggerTime = 1f; // how long the button has to be held down to automatically be considered a drag
  const float dragDistTrigger = 1.5f; // how long of a distance you have to move the tap to be considered a drag
  Vector2 dragStart;
  float inputDownTimer = 0f;
  bool isDown = false;
  bool isDragging = false;

  private Vector2[] lastZoomPositions = new Vector2[]{Vector2.zero, Vector2.zero}; // Touch mode only
  private Vector2[] newZoomPositions = new Vector2[]{Vector2.zero, Vector2.zero};
  float? touchZoomOffset = null;
  int dragFingerId;

  public const string defaultButton = "Fire1";

  protected override void Awake(){
    base.Awake();

    #if !UNITY_EDITOR
      debug = false;
    #endif

  }

  public bool touchSupported {
    get{ return Input.touchSupported; }
  }

  public Touch[] touches {
    get { return Input.touches; }
  }

  void Update(){
    if (Input.touchSupported) {
      HandleTouch();
    } else {
      HandleNonTouch();
    }
  }

  void HandleNonTouch(){
    //Figure out all the complicated drag vs zooming vs clicking stuff here
    if(ButtonIsDown()){
      if(!isDown){
        isDown = true;
        dragStart = Input.mousePosition;
      }
      inputDownTimer += Time.deltaTime;

      //Check the conditions (distance or time) for dragging
      if(inputDownTimer > dragTriggerTime || Vector2.Distance(Input.mousePosition, dragStart) > dragDistTrigger){
        if(!isDragging){
          StartDragging();
        }
        isDragging = true;
      }
    }else{
      //check if this is the first fram that the click/tap was released
      if(isDown && !isDragging){
        clickSignal.Dispatch(Input.mousePosition);
      }else if(isDown && isDragging){
        EndDragging(Input.mousePosition, false);
      }
      inputDownTimer = 0f;
      isDragging = false;
      isDown = false;
    }
  }

  void HandleTouch(){
    //If there's any touch start events see if they are over a UI game object to fix the GetButtonUp calls
    if(Input.touchCount > 0 ){
      for(var t = 0; t < Input.touchCount; t++){
        var touch = Input.GetTouch(t);
        if(touch.phase == TouchPhase.Began){
          var overGO = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
          // Debug.Log(string.Format("Touch {0}, phase {1}, radius {2}, tapCount {3}, overGO {4}", touch.fingerId, touch.phase, touch.radius, touch.tapCount, overGO));
          if(overGO){
            startedTouchOnGameObject = true;
            break;
          }else{
            startedTouchOnGameObject = false;
          }
        }
      }

      //also increase the drag threshold timer if we have any inputs
      inputDownTimer += Time.deltaTime;
    }

    touchZoomOffset = null;

    switch(Input.touchCount){
      case 0:
      {
        //check if this is the first fram that the click/tap was released
        if(isDown && !isDragging && !startedTouchOnGameObject){
          clickSignal.Dispatch(Input.mousePosition);
        }else if(isDown && isDragging){
          EndDragging(Input.mousePosition, false);
        }
        inputDownTimer = 0f;
        isDown = false;
        break;
      }
      case 1: //click or dragging
      {
        var touch = Input.GetTouch(0);
        if(touch.phase == TouchPhase.Began){
          if(!isDown){
            isDown = true;
            dragStart = touch.position;
            dragFingerId = touch.fingerId;
          }
        }
        // if(isDragging && touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled){
        //   EndDragging(touch.position, false);
        //   break;
        // }

        //Check the conditions (distance or time) for dragging
        if(!startedTouchOnGameObject &&
          (inputDownTimer > dragTriggerTime ||
           Vector2.Distance(Input.mousePosition, dragStart) > dragDistTrigger ||
           touch.phase == TouchPhase.Moved
          )
        ){
          if(dragFingerId == touch.fingerId){
            if(!isDragging){
              StartDragging();
            }
            isDragging = true;
          }else{
            //end dragging if you switched fingers
            EndDragging(Input.mousePosition, true);
            inputDownTimer = 0f;
          }
        }
        break;
      }
      case 2: //could be zooming
      {
        var dragTouch = FindDragTouch();
        EndDragging(dragTouch.HasValue ? dragTouch.Value.position : (Vector2)Input.mousePosition, true);

        var touchZero = Input.GetTouch(0);
        var touchOne = Input.GetTouch(1);

        newZoomPositions[0] = touchZero.position;
        newZoomPositions[1] = touchOne.position;
        if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began) {
          lastZoomPositions[0] = newZoomPositions[0];
          lastZoomPositions[1] = newZoomPositions[1];
        } else {
          // Zoom based on the distance between the new positions compared to the
          // distance between the previous positions.
          float newDistance = Vector2.Distance(newZoomPositions[0], newZoomPositions[1]);
          float oldDistance = Vector2.Distance(lastZoomPositions[0], lastZoomPositions[1]);
          touchZoomOffset = newDistance - oldDistance;

          lastZoomPositions[0] = newZoomPositions[0];
          lastZoomPositions[1] = newZoomPositions[1];
        }

        break;
      }
      default: //multitouch
      {
        var dragTouch = FindDragTouch();
        EndDragging(dragTouch.HasValue ? dragTouch.Value.position : (Vector2)Input.mousePosition, true);
        break;
      }
    }
  }

  void StartDragging(){
    isDragging = true;
    dragStarted.Dispatch(dragStart);
  }
  void EndDragging(Vector2 endPos, bool fingerStillDown){
    isDragging = false;
    dragFingerId = -1;
    dragEnded.Dispatch(endPos, fingerStillDown);
  }

  Touch? FindDragTouch(){
    for(var t = 0; t < Input.touchCount; t++){
      var touch = Input.GetTouch(t);
      if(touch.fingerId == dragFingerId){
        return touch;
      }
    }
    return null;
  }

  //Returns the world position of any touch or mouse down event that happened this frame, otherwise null
  public Tuple<Vector2, int> GetTouchDown(bool checkUI = true){
    if(tutorialModel.paused) return null;

    if(Input.touchSupported){
      for(var t = 0; t < Input.touchCount; t++){
        var touch = Input.GetTouch(t);
        if(touch.phase == TouchPhase.Began){
          if(checkUI && IsPointerOverGameObject(touch.fingerId)){
            continue;
          }
          return new Tuple<Vector2, int>(GetPointerWorldPosition(touch.position), touch.fingerId);
        }
      }
      return null;
    }else{
      if(GetButtonDown(checkUI: checkUI)){
        return new Tuple<Vector2, int>(GetPointerWorldPosition(Input.mousePosition), 0);
      }else{
        return null;
      }
    }
  }

  //Same as above but for touch ending
  public Tuple<Vector2, int> GetTouchUp(bool checkUI = true){
    if(tutorialModel.paused) return null;

    if(Input.touchSupported){
      for(var t = 0; t < Input.touchCount; t++){
        var touch = Input.GetTouch(t);
        if(touch.phase == TouchPhase.Ended){
          if(checkUI && IsPointerOverGameObject(touch.fingerId)){
            continue;
          }
          return new Tuple<Vector2, int>(GetPointerWorldPosition(touch.position), touch.fingerId);
        }
      }
      return null;
    }else{
      if(GetButtonUp(checkUI: checkUI)){
        return new Tuple<Vector2, int>(GetPointerWorldPosition(Input.mousePosition), 0);
      }else{
        return null;
      }
    }
  }

  //Gets the position of the touch if we can find it by finger Id, or the mouse position for non touch
  public Vector2? GetTouchScreenPosition(int fingerId){
    if(Input.touchSupported){
      for(var t = 0; t < Input.touchCount; t++){
        var touch = Input.GetTouch(t);
        if(touch.fingerId == fingerId){
          return touch.position;
        }
      }
    }else{
      return Input.mousePosition;
    }
    return null;
  }

  public bool GetTouchBeganOnCollider(Collider2D collider, bool checkUI = true){
    if(tutorialModel.paused) return false;

    if(Input.touchSupported){
      for(var t = 0; t < Input.touchCount; t++){
        var touch = Input.GetTouch(t);
        if(touch.phase != TouchPhase.Began){
          continue;
        }
        if(checkUI && IsPointerOverGameObject(touch.fingerId)){
          continue;
        }

        if(collider.OverlapPoint(GetPointerWorldPosition(touch.position))){
          return true;
        }
      }
      return false;

    }else{
      if(GetButtonDown(checkUI: checkUI)){
        return collider.OverlapPoint(GetPointerWorldPosition(Input.mousePosition));
      }else{
        return false;
      }
    }
  }

  public bool GetButtonDown(string button = defaultButton, bool checkUI = true){
    if(tutorialModel.paused) return false;

    if (Input.GetButtonDown(button) )
    {
      if (checkUI && IsPointerOverGameObject()){
        return false;
      }
      return true;
    }

    return false;
  }

  public bool GetButtonUp(string button = defaultButton, bool checkUI = true){
    if(tutorialModel.paused) return false;

    if (Input.GetButtonUp(button) )
    {
      if (checkUI && IsPointerOverGameObject()){
        return false;
      }
      return true;
    }

    return false;
  }

  public bool ButtonIsDown(string button = defaultButton, bool checkUI = true){
    if(tutorialModel.paused) return false;

    if(Input.GetButton(button)){
      if(debug){
        RaycastHit2D hit = Physics2D.Raycast(pointerWorldPosition, Vector3.forward);
        if(hit.transform != null){
          Debug.Log("Clicked " + hit.transform.name);
        }else{
          PointerEventData pointer = new PointerEventData(EventSystem.current);
          pointer.position = Input.mousePosition;
          List<RaycastResult> raycastResults = new List<RaycastResult>();
          EventSystem.current.RaycastAll(pointer, raycastResults);

          if(raycastResults.Count > 0)
          {
            foreach(var go in raycastResults)
            {
              Debug.Log("Clicked " + go.gameObject.name);
            }
          }else{
            Debug.Log("Clicked Nothing");
          }
        }
      }
      if (checkUI && IsPointerOverGameObject()){
        return false;
      }
      return true;
    }
    return false;
  }

  ///<returns>true if mouse or first touch is over any event system object ( usually gui elements )</returns>
  public bool IsPointerOverGameObject(int? fingerId = null)
  {
    //check mouse
    if (EventSystem.current.IsPointerOverGameObject()){
      // Debug.Log("Pointer over from mouse");
      return true;
    }

    //check touch
    if (Input.touchCount > 0)
    {
      if (EventSystem.current.IsPointerOverGameObject(fingerId.HasValue ? fingerId.Value : Input.GetTouch(0).fingerId)){
        // Debug.Log("Pointer over from touch count");
        return true;
      }
    }

    if(startedTouchOnGameObject){
      // Debug.Log("Pointer over from start touch");
      return true;
    }

    return false;
  }

  public Vector2 pointerWorldPosition{
    get
    {
      return GetPointerWorldPosition(Input.mousePosition);
    }
  }

  public Vector2 GetPointerWorldPosition(Vector3 mousePosition){
    return cameraService.Cam.ScreenToWorldPoint( new Vector3(mousePosition.x, mousePosition.y, cameraService.Cam.nearClipPlane));
  }

  public Vector3 pointerPosition{
    get
    {
      return Input.mousePosition;
    }
  }

  public IEnumerable<Vector2> pointerPositions {
    get
    {
      if(Input.touchSupported){
        return Input.touches.Select(t => t.position);
      }else{
        return Enumerable.Repeat((Vector2)Input.mousePosition, 1);
      }
    }
  }

  public float zoomDelta {
    get{
      if(Input.touchSupported){
        return touchZoomStep * touchZoomOffset ?? 0;
      }else{
        return mouseZoomStep * Input.mouseScrollDelta.y;
      }
    }
  }
}

