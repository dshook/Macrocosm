using UnityEngine;
using TMPro;
using strange.extensions.mediation.impl;
using Unity.VectorGraphics;

public class GalaxyFactoryResourceEfficiencyDisplay : View {
  [Inject] public ResourceLoaderService loader {get; set;}

  public SVGImage resourceIcon;
  public TMP_Text resourceAmt;

  public GameResourceType iconResourceType;
  public GalaxyResource resource;
  public float settlementEfficiencyBonus;

  bool iconNeedsUpdate = true;

  protected override void Awake () {
    base.Awake();

  }

  protected override void OnEnable()
  {
    base.OnEnable();
  }

  public void Init(){
    TryUpdatingIcon();
    UpdateText();
  }

  void Update () {
    TryUpdatingIcon();
    // UpdateText();
  }

  void TryUpdatingIcon(){
    if(loader != null && iconNeedsUpdate){
      resourceIcon.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[iconResourceType]);
      iconNeedsUpdate = false;
    }
  }

  void UpdateText(){
    var pctBonusAmount = 0f;
    if(resource != null){
      pctBonusAmount = GalaxyResource.GetResourceEfficiencyBonus(resource.amount);
    }
    resourceAmt.text = (pctBonusAmount + settlementEfficiencyBonus).ToString("0%");
  }
}