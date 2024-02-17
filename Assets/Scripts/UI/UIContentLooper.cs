using UnityEngine;

//Move the contents and then reorder the children so it stays in a loop
//Assumes the contents natual position is 0,0
public class UIContentLooper : MonoBehaviour
{

  public float childItemWidth = 1f;
  public bool calculateMinItems = true;
  [Tooltip("Only if calculate min items is false")]
  public int fixedMinItems = 0;

  public float moveTime = 1f;
  public Vector2 moveAmount = new Vector2(-80f, 0);

  public RectTransform content;

  int goToChildIndex = 0;
  float timeAccum = 0f;
  public Vector2 contentOriginalPosition;

  int calculatedMinItems;
  public int MinItems{ get{return calculateMinItems ? calculatedMinItems : fixedMinItems; }}

  void Awake(){

    //Adding as public var to set in editor since it seemed to be getting the wrong position on device booting up
    // contentOriginalPosition = content.anchoredPosition;
  }

  public void Reset(){
    goToChildIndex = 0;
    timeAccum = 0f;
    content.anchoredPosition = contentOriginalPosition;
  }

  void Update(){
    if(goToChildIndex <= 0){
      return;
    }

    calculatedMinItems = Mathf.RoundToInt(content.rect.width / childItemWidth);

    timeAccum += Time.smoothDeltaTime;

    if(timeAccum >= moveTime){
      //reset
      content.anchoredPosition = contentOriginalPosition;
      var childToMove = content.GetChild(0);
      childToMove.SetAsLastSibling();
      childToMove.localScale = Vector3.zero;
      LeanTween.scale(childToMove.gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutExpo);

      //Have to force update here to get the SequenceIndicator with follower to update to the right position
      Canvas.ForceUpdateCanvases();

      timeAccum = 0f;
      goToChildIndex--;
    }

    content.anchoredPosition = contentOriginalPosition + (moveAmount * (timeAccum / moveTime));
  }

  public void Next(){
    if(content.childCount < MinItems){
      return;
    }
    goToChildIndex++;
  }

}