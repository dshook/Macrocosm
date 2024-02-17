using PygmyMonkey.ColorPalette;
using TMPro;
using UnityEngine;

public class CelestialBodyResourceDisplay : MonoBehaviour {
  public StageSevenDataModel stageSevenData {get; set;}
  public CelestialBodyData data {get; set;}

  public GameResourceType resourceType {get; set;}
  public GalaxyTransitionSignal galaxyTransitionSignal {get; set;}

  public TMP_Text resourceText;
  public TMP_Text resourceTextUnderlay;

  GalaxyTransitioner transitioner;
  ColorPalette resourcePalette;

  public void Init(){
    resourcePalette = ColorPaletteData.Singleton.fromName("Stage 7 Resource Abundances");

    if(!GalaxyResource.GalaxyResourceAbbr.ContainsKey(resourceType)){
      Debug.LogWarning("No resource abbreviation for " + resourceType);
      return;
    }

    var elementAbbr = GalaxyResource.GalaxyResourceAbbr[resourceType];

    resourceText.text = resourceTextUnderlay.text = elementAbbr;
    Update();

    galaxyTransitionSignal.AddListener(OnTransition);

    //icon way of doing it:

    // var resourceRenderer = newIcon.GetComponent<SpriteRenderer>();
    // var resourceAbundanceRenderer = newIcon.transform.GetChild(0).GetComponent<SpriteRenderer>();
    // resourceRenderer.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[rd.type]);
    // // resourceRenderer.color = ColorPaletteData.Singleton.fromName("Stage 7 Resource Abundances").getColorFromName(rd.abundance.ToString()).color;
    // resourceAbundanceRenderer.sprite = loader.Load<Sprite>(CelestialResourceDeposit.abundanceIcons[rd.abundance]);
  }

  void OnDestroy(){
    galaxyTransitionSignal.RemoveListener(OnTransition);
  }

  void Update(){
    if(transitioner != null && transitioner.inProgress){
      //Disable color changing while transitioning
      return;
    }

    //keep the abundance up to date as settlements are mining them
    resourceText.color = GalaxyResource.GetAbundanceColor(resourcePalette, abundance);
  }

  ResourceAbundance abundance{
    get{
      var settlement = stageSevenData.settlements.TryGet(data.id);
      if(settlement != null){
        var settlementResource = settlement.resources.TryGet(resourceType);
        if(settlementResource != null && settlementResource.totalAmount.HasValue){
          return GalaxyResource.GetAbundance(settlementResource.totalAmount.Value);
        }
      }
      for(var i = 0; i < data.resourceDeposits.Length; i++){
        if(data.resourceDeposits[i].type != resourceType){
          continue;
        }
        return data.resourceDeposits[i].abundance;
      }

      throw new System.Exception("CB Resource display couldn't find resource type " + resourceType);
    }
  }

  void OnTransition(GalaxyTransitionInfo transitionInfo){
    transitioner = transitionInfo.transitioner;
  }
}