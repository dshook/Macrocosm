using UnityEngine;
using TMPro;
using PygmyMonkey.ColorPalette;

public class GalaxyIncomeDisplay : GalaxyResourceDisplayBase {
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] public ResourceLoaderService loader {get; set;}
  [Inject] PaletteService palettes {get; set;}

  public Unity.VectorGraphics.SVGImage resourceIcon;
  public TMP_Text resourceText;
  public TMP_Text resourceChange;
  public TMP_Text resourceAbundance;


  public GalaxyResource _resource;
  public override GalaxyResource resource{
    get{
      return _resource;
    }
    set{
      if(_resource != value){
        _resource = value;
        resourceText.text = resource.type.ToString();
        resourceDeltaId = "resource-delta-" + resourceText.text;
        iconNeedsUpdate = true;
      }
    }
  }

  Color buttonSelectedColor;
  Color buttonNotSelectedColor;
  Color greenColor;
  Color redColor;
  Color exportColor;

  string resourceId;
  string resourceDeltaId;
  bool iconNeedsUpdate = true;

  protected override void Awake () {
    base.Awake();

    greenColor = palettes.primary.getColorFromName("Green").color;
    redColor = palettes.primary.getColorFromName("Red").color;

  }

  protected override void OnDisable()
  {
    base.OnDisable();
    if(stringChanger != null){
      stringChanger.ClearValue(resourceDeltaId);
    }
  }

  protected override void OnEnable()
  {
    base.OnEnable();
  }

  void Update () {
    if(loader != null && iconNeedsUpdate && resourceIcon != null){
      resourceIcon.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[resource.type]);
      iconNeedsUpdate = false;
    }

    if(resourceDeltas != null && resourceDeltas.ContainsKey(resource.type)){
      var delta = resourceDeltas[resource.type].amount;

      stringChanger.UpdateStringShortFormat(resourceChange, resourceDeltaId, delta, "+");
      // if(delta >= 0){
      //   resourceChange.color = greenColor;
      // }else{
      //   resourceChange.color = redColor;
      // }
    }else{
      if(resourceChange != null){
        resourceChange.text = string.Empty;
      }
    }

    if(resource.totalAmount.HasValue && resourceAbundance != null){
      var abundance = GalaxyResource.GetAbundance(resource.totalAmount.Value);
      resourceAbundance.text = GalaxyResource.ResourceAbundanceNames[abundance];

      var color = GalaxyResource.GetAbundanceColor(palettes.resourceAbundances, abundance);
      resourceAbundance.color = color;
      resourceChange.color = color;
    }

  }

}