using UnityEngine;
using Shapes;
using strange.extensions.mediation.impl;

public class BoostDisplay : View {
  [Inject] StageRulesService stageRules { get; set; }

  public Snake snake;
  public ShinyButton boostButton;
  public Disc boostIndicator;

  public float startAngle;
  public float endAngle;


  void Update () {
    var totalBoost = stageRules.StageTwoRules.boostAmount;

    if(snake == null || boostIndicator == null || totalBoost == 0){
      return;
    }

    var boostPct = snake.BoostTime / totalBoost;
    var totalAngle = endAngle - startAngle;
    var angleDiff = totalAngle - (totalAngle * boostPct);

    boostIndicator.AngRadiansStart = startAngle + angleDiff / 2f;
    boostIndicator.AngRadiansEnd = endAngle - angleDiff / 2f;

  }


}
