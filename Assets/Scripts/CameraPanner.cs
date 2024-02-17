using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class CameraPanner : View {

  [Range(0f, 1f)]
  public float damping = 0.95f;

  public float maxZoomSize = 20f;
  public float minZoomSize = 3f;

  public Bounds bounds;

  [Inject] InputService input {get; set;}

  [Inject] DragStartedSignal dragStarted {get; set;}
  [Inject] DragEndedSignal dragEnded {get; set;}
  [Inject] ClickSignal clickSignal {get; set;}
  [Inject] CameraService cameraService {get; set;}

  Camera cam;

  Vector2 dragStart;
  bool isDragging;

  Vector2 postDragVelocity;
  const int framePosCount = 3;
  //circular buffer to store mouse world positions for the last framePosCount frames
  Vector2[] prevFramePos = new Vector2[framePosCount];
  int prevFrameIdx = 0;

  protected override void Awake() {
    base.Awake();

    cam = cameraService.Cam;

    dragStarted.AddListener(OnDragStart);
    dragEnded.AddListener(OnDragEnd);
  }


  void Update(){

    if(isDragging){
      var curPos = input.pointerWorldPosition;
      prevFramePos[prevFrameIdx] = input.pointerPosition;
      prevFrameIdx = (prevFrameIdx + 1) % framePosCount;

      cam.transform.localPosition -= (Vector3)curPos - (Vector3)dragStart;
    }else if(postDragVelocity.magnitude > 0.01f){
      postDragVelocity *= damping;
      cam.transform.localPosition += (Vector3)postDragVelocity;
    }

    #if UNITY_EDITOR
    KeyControls();
    #endif

    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - input.zoomDelta, minZoomSize, maxZoomSize);

    CheckBounds();

  }

  void CheckBounds(){
    bool hitBounds = false;

    if(cam.transform.localPosition.x < bounds.min.x){
      cam.transform.localPosition = cam.transform.localPosition.SetX(bounds.min.x);
      hitBounds = true;
    }
    if(cam.transform.localPosition.y < bounds.min.y){
      cam.transform.localPosition = cam.transform.localPosition.SetY(bounds.min.y);
      hitBounds = true;
    }

    if(cam.transform.localPosition.x > bounds.max.x){
      cam.transform.localPosition = cam.transform.localPosition.SetX(bounds.max.x);
      hitBounds = true;
    }
    if(cam.transform.localPosition.y > bounds.max.y){
      cam.transform.localPosition = cam.transform.localPosition.SetY(bounds.max.y);
      hitBounds = true;
    }

    if(hitBounds){
      if(isDragging){
        // Debug.Log("hit bounds dragging");
      }else{
        postDragVelocity = Vector3.zero;
        // Debug.Log("hit bounds not dragging");
      }
    }
  }

  void OnDragStart(Vector2 pos){
    isDragging = true;
    dragStart = input.GetPointerWorldPosition(pos);

    //reset previous frame positions
    for(int i = 0; i < framePosCount; i++){
      prevFramePos[i] = Vector2.negativeInfinity;
    }
    prevFramePos[0] = pos;
    prevFrameIdx = 1;

    postDragVelocity = Vector3.zero;
  }

  void OnDragEnd(Vector2 pos, bool fingerStillDown){
    isDragging = false;
    var dragEnd = input.GetPointerWorldPosition(pos);

    if(fingerStillDown){
      postDragVelocity = Vector3.zero;
    }else{
      prevFramePos[prevFrameIdx] = pos;

      var avg = (Vector3)GetAvgFrameVector();
      avg = cam.ScreenToWorldPoint( avg.SetZ(cam.nearClipPlane));
      postDragVelocity = avg - (Vector3)dragEnd;
      // Debug.Log("drag end, vel: " + postDragVelocity + " DragStart: " + dragStart + "DragEnd: " + dragEnd);
    }
  }

  Vector3 GetAvgFrameVector()
  {
    float x = 0f;
    float y = 0f;
    int vectorCount = 0;

    //since the order doesn't matter while averaging over the cirular buffer just go through all the values that are non-sentinal
    for(int i = 0; i < framePosCount; i++)
    {
      var pos = prevFramePos[i];
      if(pos.x == float.NegativeInfinity || pos.y == float.NegativeInfinity) continue;

      x += pos.x;
      y += pos.y;
      vectorCount++;
    }

    if(vectorCount == 0){
      return Vector2.zero;
    }

    return new Vector2(x / vectorCount, y / vectorCount);
  }

  public void Disable(){
    isDragging = false;
    ResetDragVelocity();
    this.enabled = false;
  }

  public void Enable(){
    if(!this.enabled){
      ResetDragVelocity();
      this.enabled = true;
    }
  }


  public void ResetZoom(){
    cameraService.ResetPositionAndSize();
  }

  public void ResetDragVelocity(){
    postDragVelocity = Vector3.zero;
    //reset previous frame positions
    for(int i = 0; i < framePosCount; i++){
      prevFramePos[i] = Vector2.negativeInfinity;
    }
  }

  public void KeyControls(){
    Vector2 panVector = Vector2.zero;
    float speed = 1f;

    if(UnityEngine.Input.GetKey(KeyCode.UpArrow)){
      panVector = panVector.AddY(speed);
    }
    if(UnityEngine.Input.GetKey(KeyCode.DownArrow)){
      panVector = panVector.AddY(-speed);
    }
    if(UnityEngine.Input.GetKey(KeyCode.RightArrow)){
      panVector = panVector.AddX(speed);
    }
    if(UnityEngine.Input.GetKey(KeyCode.LeftArrow)){
      panVector = panVector.AddX(-speed);
    }

    cam.transform.localPosition += (Vector3)panVector * Time.smoothDeltaTime;


    float zoomDelta = 0;
    if(UnityEngine.Input.GetKey(KeyCode.PageUp)){
      zoomDelta = -1.5f;
    }
    if(UnityEngine.Input.GetKey(KeyCode.PageDown)){
      zoomDelta = 1.5f;
    }
    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (zoomDelta * Time.smoothDeltaTime), minZoomSize, maxZoomSize);
  }
}
