using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;

public class GalaxyFactoryButton : View {
  [Inject] public ResourceLoaderService loader {get; set;}
  [Inject] public StageRulesService stageRules {get; set;}

  public Button button;
  public Unity.VectorGraphics.SVGImage resourceIcon;
  public Sprite selectResourceSprite;
  public TMP_Text text;

  public GameResourceType? _resource = null;
  public GameResourceType? resource{
    get{
      return _resource;
    }
    set{
      _resource = value;
      iconNeedsUpdate = true;
    }
  }

  bool iconNeedsUpdate = true;

  protected override void Awake () {
    base.Awake();

  }

  void Update () {
    if(loader != null && iconNeedsUpdate){
      if(resource.HasValue){
        resourceIcon.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[resource.Value]);
      }else{
        resourceIcon.sprite = selectResourceSprite;
      }
      iconNeedsUpdate = false;
    }
  }

}