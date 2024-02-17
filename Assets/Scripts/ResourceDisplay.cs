using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceDisplay : MonoBehaviour {
  public TMP_Text costText;
  public Unity.VectorGraphics.SVGImage resourceIcon;

  public GameResource resource {get; set;}
  public ResourceLoaderService loader {get; set;}

  public int? availableAmount {get; set;}

  public NumberFormatLength numberFormat = NumberFormatLength.Normal;

  public void Init(){
    resourceIcon.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[resource.type]);
    UpdateText();
  }

  public void UpdateText(){
    costText.text = resource.amount.Format(numberFormat);

    //Change the color of the resources you don't have enough of to build yet
    if(availableAmount != null){
      if(availableAmount >= resource.amount){
        costText.color = Color.white;
      }else{
        costText.color = Colors.darkGray;
      }
    }
  }
}