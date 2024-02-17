using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;

public class GalaxyFactoryRow : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] SelectGalaxyResourceSignal selectResource {get; set;}
  [Inject] GalaxyResourceSelectedSignal resourceSelected {get; set;}
  [Inject] GalaxyResourceSelectCancelSignal resourceSelectCancel {get; set;}

  public StarSettlementData starSettlementData;
  public GalaxyBuilding factory;
  public StarFactoryData factoryData{
    get{
      return _factoryData;
    }
    set{
      _factoryData = value;

      UpdateFromOutputResource(factoryData.output);
      UpdateOutputResource();
    }
  }
  StarFactoryData _factoryData;

  public GalaxyFactoryButton input1;
  public GalaxyFactoryButton input2;
  public GalaxyFactoryButton output;

  uint lastClickedIndex;

  protected override void Awake () {
    base.Awake();

    input1.button.onClick.AddListener(() => ClickResourceButton(0));
    input2.button.onClick.AddListener(() => ClickResourceButton(1));
    output.button.onClick.AddListener(ClickOutputButton);

    resourceSelectCancel.AddListener(() => {
      resourceSelected.RemoveListener(OnResourceSelected);
      resourceSelected.RemoveListener(OnOutputResourceSelected);
    });
  }

  public void Init(){
    input1.text.text = factory.factoryInputAmount.ToShortFormat();
    input2.text.text = factory.factoryInputAmount.ToShortFormat();
  }

  void Update () {
  }

  void ClickResourceButton(uint inputIndex){
    lastClickedIndex = inputIndex;
    resourceSelected.AddListener(OnResourceSelected);
    selectResource.Dispatch(FilterTier1Resources, false);
  }

  void OnResourceSelected(GameResourceType? type){
    if(lastClickedIndex == 0){
      input1.resource = type;
    }else{
      input2.resource = type;
    }
    resourceSelected.RemoveListener(OnResourceSelected);
    UpdateOutputResource();
  }

  void ClickOutputButton(){
    resourceSelected.AddListener(OnOutputResourceSelected);
    selectResource.Dispatch(FilterTier2Resources, true);
  }

  void OnOutputResourceSelected(GameResourceType? type){
    resourceSelected.RemoveListener(OnOutputResourceSelected);
    UpdateFromOutputResource(type);
  }

  void UpdateFromOutputResource(GameResourceType? type){
    output.resource = type;

    if(type.HasValue){
      //update the inputs based on what was selected, assume only two now
      if(!GalaxyResource.resourceDependencies.ContainsKey(type.Value)){
        output.resource = null;
        return;
      }

      var dependency = GalaxyResource.resourceDependencies[type.Value];
      input1.resource = dependency.dependents[0];
      input2.resource = dependency.dependents[1];

      factoryData.output = type.Value;

      var outputAmount = factoryData.GetNewYearsAmountProduced(starSettlementData, factory.factoryInputAmount);
      output.text.text = outputAmount == null ? string.Empty : outputAmount.Value.ToShortFormat();
    }else{
      input1.resource = null;
      input2.resource = null;
      factoryData.output = null;
      output.text.text =  string.Empty;
    }

  }

  void UpdateOutputResource(){
    GameResourceType? outputResource = null;
    GalaxyResource.GalaxyDependency outputDep = null;
    if(
      input1.resource.HasValue &&
      input2.resource.HasValue &&
      input1.resource.Value != input2.resource.Value
    ){
      var resourceDeps = GalaxyResource.resourceDependencies.FirstOrDefault(kv =>
        kv.Value.dependents.Contains(input1.resource.Value) &&
        kv.Value.dependents.Contains(input2.resource.Value)
      );

      if(resourceDeps.Key != 0 && !GalaxyResource.UnusedResourceTypes.Contains(resourceDeps.Key) ){
        outputResource = resourceDeps.Key;
        outputDep = resourceDeps.Value;
      }else{
        outputResource = null;
        outputDep = null;
      }
    }

    factoryData.output = outputResource;
    var outputAmount = factoryData.GetNewYearsAmountProduced(starSettlementData, factory.factoryInputAmount);

    output.resource = outputResource;
    output.text.text = outputAmount == null ? string.Empty : outputAmount.Value.ToShortFormat();
  }

  bool FilterTier1Resources(GameResourceType type){
    return GalaxyResource.Tier1ResourceTypes.Contains(type);
  }

  bool FilterTier2Resources(GameResourceType type){
    return GalaxyResource.Tier2ResourceTypes.Contains(type) && !GalaxyResource.UnusedResourceTypes.Contains(type);
  }
}
