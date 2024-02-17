using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PygmyMonkey.ColorPalette;

public class GalaxyResourceDisplay : GalaxyResourceDisplayBase {
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] public ResourceLoaderService loader {get; set;}

  public ShinyButton importButton;
  public ShinyButton exportButton;
  public Unity.VectorGraphics.SVGImage resourceIcon;
  public TMP_Text resourceText;
  public TMP_Text resourceAmt;
  public TMP_Text resourceChange;

  public GalaxyResource _resource;
  public override GalaxyResource resource{
    get{
      return _resource;
    }
    set{
      if(_resource != value){
        _resource = value;
        var resourceName = GalaxyResource.GalaxyResourceNames[resource.type];
        if(resourceText != null){
          resourceText.text = resourceName;
        }

        if(stringChanger != null){
          //Clear old values when changing to new resource
          stringChanger.ClearValue(resourceId);
          stringChanger.ClearValue(resourceDeltaId);

          resourceId = $"{stringNamespace}-r-{(int)resource.type}";
          resourceDeltaId = $"{stringNamespace}-rd-{(int)resource.type}";
        }

        iconNeedsUpdate = true;
      }
      UpdateButtons();
      Update();
    }
  }

  UIColor buttonSelectedColor;
  UIColor buttonNotSelectedColor;
  Color greenColor;
  Color redColor;
  Color exportColor;

  string resourceId;
  string resourceDeltaId;
  bool iconNeedsUpdate = true;

  protected override void Awake () {
    base.Awake();

    if(importButton != null){
      importButton.onClick.AddListener(ImportClick);
    }
    if(exportButton != null){
      exportButton.onClick.AddListener(ExportClick);
    }
    buttonSelectedColor = UIColor.Green;
    buttonNotSelectedColor = UIColor.DarkPurple;
    greenColor = ColorPaletteData.Singleton.fromName("Primary").getColorFromName("Green").color;
    redColor = ColorPaletteData.Singleton.fromName("Primary").getColorFromName("Red").color;
    //TODO: this logic should be consolidated in a helper somewhere
    exportColor = ColorPaletteData.Singleton.fromName("UI").getColorFromName(buttonSelectedColor.ToString() + "Primary").color;

  }

  protected override void OnDisable()
  {
    base.OnDisable();
    if(stringChanger != null){
      stringChanger.ClearValue(resourceId);
      stringChanger.ClearValue(resourceDeltaId);
    }
  }

  protected override void OnEnable()
  {
    base.OnEnable();
    UpdateButtons();
  }

  void Update () {
    TryUpdatingIcon();
    UpdateText();
  }

  void TryUpdatingIcon(){
    if(loader != null && iconNeedsUpdate){
      resourceIcon.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[resource.type]);
      iconNeedsUpdate = false;
    }
  }

  void UpdateText(){
    if(stringChanger == null){
      return;
    }

    if(resourceAmt != null ){
      stringChanger.UpdateStringShortFormat(resourceAmt, resourceId, resource.amount);
    }
    if(resourceDeltas != null && resourceDeltas.ContainsKey(resource.type)){
      var delta = resourceDeltas[resource.type].amount;
      stringChanger.UpdateStringShortFormat(resourceChange, resourceDeltaId, delta);
      if(resource.isExportingResource(resourceDeltas)){
        resourceChange.color = exportColor;
      }else if(delta >= 0){
        resourceChange.color = greenColor;
      }else{
        resourceChange.color = redColor;
      }
    }else{
      if(resourceChange != null){
        resourceChange.text = string.Empty;
      }
    }
  }

  void ImportClick(){
    if(resource.importing){
      resource.importing = false;
    }else{
      resource.importing = true;
      resource.exporting = false;
    }
    UpdateButtons();
    if(OnImportExportChanged != null){
      OnImportExportChanged();
    }
  }

  void ExportClick(){
    if(resource.exporting){
      resource.exporting = false;
    }else{
      resource.exporting = true;
      resource.importing = false;
    }
    UpdateButtons();
    if(OnImportExportChanged != null){
      OnImportExportChanged();
    }
  }

  void UpdateButtons(){
    if(importButton != null && exportButton != null){
      importButton.isSelected = resource.importing;
      exportButton.isSelected = resource.exporting;

      importButton.color = resource.importing ? buttonSelectedColor : buttonNotSelectedColor;
      exportButton.color = resource.exporting ? buttonSelectedColor : buttonNotSelectedColor;

    }
  }

}