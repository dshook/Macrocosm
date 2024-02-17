using UnityEngine;

//Animates all the bg cells, assumes their all children of this component
public class BgCellDancer : MonoBehaviour {

  public BeatManager beatManager;

  public float squashAmount = 0.8f;

  void Update(){

    if(beatManager.State != BeatManager.BeatManagerState.Playing){
      return;
    }

    float beatTime = (float)beatManager.SongBeats * 2 * Mathf.PI;

    var deformation = Strat1(beatTime);

    for(var c = 0; c < transform.childCount; c++){
      var bgCell = transform.GetChild(c);

      if(bgCell.localScale.x < squashAmount && bgCell.localScale.y < squashAmount){
        //Skip dancing for the cells tweening in
        continue;
      }

      bgCell.localScale = deformation;
    }
  }

  Vector2 Strat1(float beatTime){
    var squashOffset = (1f - squashAmount) / 2f;
    var horizontalStretch = (Mathf.Sin(beatTime - (Mathf.PI / 2f)) * squashOffset) + squashOffset + squashAmount;
    var verticalStretch   = (Mathf.Cos(beatTime                  ) * squashOffset) + squashOffset + squashAmount;

    return new Vector2(horizontalStretch, verticalStretch);
  }


}