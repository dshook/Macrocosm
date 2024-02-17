using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HexCity;

public class CityPopupDisplay : MonoBehaviour {
  public TMP_Text descripComp;
  public Transform descripRows;

  public enum CityPopupDisplayType {
    Health,
    Happiness,
    Food,
    Production,
    Science
  }

  public HexCity city;
  public CityPopupDisplayType displayType;

  public VerticalLayoutGroup layoutGroup;
  public RectTransform rectTransform;

  public CopyHeightFitter[] fittersToUpdate;

  IEnumerable<CityChangeInfo> changes;

  public void Init(){
    switch(displayType){
      case CityPopupDisplayType.Health:
        changes = (IEnumerable<CityChangeInfo>)city.healthFactors.Values;
        break;
      case CityPopupDisplayType.Happiness:
        changes = (IEnumerable<CityChangeInfo>)city.happinessFactors.Values;
        break;
      case CityPopupDisplayType.Food:
        changes = (IEnumerable<CityChangeInfo>)city.foodFactors.Values;
        break;
      case CityPopupDisplayType.Production:
        changes = (IEnumerable<CityChangeInfo>)city.productionFactors.Values;
        break;
      case CityPopupDisplayType.Science:
        changes = (IEnumerable<CityChangeInfo>)city.scienceFactors.Values;
        break;
    }

    Update();
  }

  void Update(){
    UpdateTitleAndDescrip();
    UpdateRows();
  }

  void UpdateTitleAndDescrip(){
    var threshold = displayType == CityPopupDisplayType.Health ? city.healthThreshold : city.happinessThreshold;

    string title = "";
    string level = "";
    string descrip = "";

    switch(displayType){
      case CityPopupDisplayType.Health:
        title = "Health";
        level = threshold.title;
        descrip = threshold.descrip;
        break;
      case CityPopupDisplayType.Happiness:
        title = "Happiness";
        level = threshold.title;
        descrip = threshold.descrip;
        break;
      case CityPopupDisplayType.Food:
        title = "Food";
        break;
      case CityPopupDisplayType.Production:
        title = "Production";
        break;
      case CityPopupDisplayType.Science:
        title = "Science";
        break;
    }

    if(descripComp != null){
      if(!string.IsNullOrEmpty(level) && !string.IsNullOrEmpty(descrip)){
        descripComp.text = string.Format("{0} - {1}<br>{2}", title, level, descrip);
      }else{
        descripComp.text = string.Format("{0}<br>", title);
      }
    }
  }

  void UpdateRows(){
    var i = 0;
    foreach(var change in changes){
      if(change.change == 0 && !change.changePct.HasValue && !change.changeFloat.HasValue){ continue;}
      var descripRow = GetOrMakeRow(i);
      var descrip = descripRow.Find("descrip").GetComponent<TMP_Text>();
      var value = descripRow.Find("value").GetComponent<TMP_Text>();

      descrip.text = change.descrip;
      if(change.changePct.HasValue){
        value.text = change.changePct.Value.ToString("0%");
      }else if(change.changeFloat.HasValue){
        value.text = change.changeFloat.Value.ToString("0.0");
      }else{
        value.text = change.change.ToString("+0;-#");
      }
      i++;
    }

    var showTotal = displayType == CityPopupDisplayType.Health || displayType == CityPopupDisplayType.Happiness;

    if(showTotal){
      var totalRow = GetOrMakeRow(i);
      var total = totalRow.Find("descrip").GetComponent<TMP_Text>();
      var totalValue = totalRow.Find("value").GetComponent<TMP_Text>();

      total.text = "Total";
      totalValue.text = changes.Sum(c => c.change).ToString("+0;-#");
    }

    //delete extra rows that may still be hanging around, accounting for the totals row
    if(descripRows.childCount > i + 1){
      for(int d = descripRows.childCount - 1; d >= i + 1; d--){
        Destroy(descripRows.GetChild(d).gameObject);
      }
    }

    //Update the layout group and content size fitters so you don't get a weird pop in
    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    if(fittersToUpdate != null){
      foreach(var fitter in fittersToUpdate){
        fitter.LateUpdate();
      }
    }
  }

  //Get the row at the index, or create a new one if that index doesn't exist
  Transform GetOrMakeRow(int index){
    if(descripRows.childCount > index){
      return descripRows.GetChild(index);
    }
    //use first descrip row child as template child
    var descripRowTemplate = descripRows.GetChild(0);

    var newRow = GameObject.Instantiate(descripRowTemplate, Vector3.zero, Quaternion.identity);
    newRow.transform.SetParent(descripRows, false);

    return newRow;
  }

}