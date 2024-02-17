using UnityEngine;
using System.Collections.Generic;
using PygmyMonkey.ColorPalette;
using System.Linq;
using TMPro;
using Shapes;

//Data that's saved about the star regardless of settlement
[System.Serializable]
public class StarData {
  public StarExploreStatus exploreStatus;
}

public class Star : GalaxyCelestial {
  [Inject] GalaxyTransitionSignal galaxyTransitionSignal {get; set;}
  [Inject] CelestialColonizedSignal celestialColonized { get; set; }
  [Inject] GalaxyStarImportExportChangedSignal importExportChangedSignal {get; set;}
  [Inject] GalaxyBuildingFinishedSignal buildingFinishedSignal {get; set;}

  public ColorFader settlementIndicatorColorFader;
  public Disc settlementIndicatorRenderer;
  public CircleCollider2D circleCollider;

  public ShinyButtonWorld importButton;
  public ShinyButtonWorld exportButton;
  public GameObject bookmarkIndicator;
  public TMP_Text   routeResourceAmount;

  public Transform planetHolder;
  public Transform swarmHolder;
  public ColorFader swarmColorFader;

  public MultiText multiNameText;
  public TMP_Text nameText;
  public TMP_Text nameTextUnderlay;
  public ColorFader nameColorFader;

  public GeneratedStarData generatedData;
  public StarData data {
    get{
      return stageSevenData != null && stageSevenData.starData != null ?
        stageSevenData.starData.TryGet(generatedData.id) :
        null;
    }
    set{
      stageSevenData.starData[generatedData.id] = value;
    }
  }
  public StarSettlementData settlementData {get; set;}
  public List<CelestialBody> celestialBodies = new List<CelestialBody>(); //only filled in when they're instanciated

  //This is annoying to track but used for updating the display when buildings are built. Rather this than have a ref to galaxyTransitioner for all stars
  bool currentlyViewingThisStarInSystem = false;

  protected override void Awake() {
    base.Awake();

    galaxyTransitionSignal.AddListener(OnTransition);
    celestialColonized.AddListener(OnCelestialColonized);
    buildingFinishedSignal.AddListener(OnBuildingFinishedSignal);

    importButton.onClick.AddListener(ImportClick);
    exportButton.onClick.AddListener(ExportClick);

    UpdateDisplay(StageSevenManager.StarPalette);
    UpdateSettlementIndicatorSize(0);
  }

  protected override void OnDestroy(){
    if(galaxyTransitionSignal != null){
      galaxyTransitionSignal.RemoveListener(OnTransition);
    }
    if(celestialColonized != null){
      celestialColonized.RemoveListener(OnCelestialColonized);
    }
    if(buildingFinishedSignal != null){
      buildingFinishedSignal.RemoveListener(OnBuildingFinishedSignal);
    }
  }

  void Update(){
  }

  Vector3 GalaxyScaleSize {
    get{ return Vector3.one * generatedData.solarRadius; }
  }
  Vector3 SystemScaleSize {
    get{ return Vector3.one * generatedData.solarRadius * Galaxy.SystemViewStarScalePct; }
  }

  public void UpdateDisplay(ColorPalette palette){
    baseShapeRenderer.Color = palette.getColorAtIndex(colorMap[generatedData.type]);
    var settlementIndicatorColor = palette.getColorAtIndex(8);
    var textOutlineColor = palette.getColorAtIndex(9);
    if(settlementIndicatorRenderer != null){
      settlementIndicatorRenderer.Color = settlementIndicatorColor;
    }

    display.transform.localScale = GalaxyScaleSize;
    multiNameText.text = generatedData.name;
    nameText.color = settlementIndicatorColor;
    nameTextUnderlay.outlineColor = textOutlineColor;

    //Scale up the collider to make it easier to click
    circleCollider.radius = (baseShapeRenderer as Disc).Radius * 2f;

    bookmarkIndicator.SetActive(false);
    UpdateExploreDisplay();
  }

  public void UpdateExploreDisplay(){
    //Explore flag display
    if(data != null && settlementData == null){
      if(data.exploreStatus == StarExploreStatus.LooksBad){
        baseShapeRenderer.Color = baseShapeRenderer.Color.SetA(0.6f);
        bookmarkIndicator.SetActive(false);
      }
      if(data.exploreStatus == StarExploreStatus.LooksGood){
        bookmarkIndicator.SetActive(true);
        baseShapeRenderer.Color = baseShapeRenderer.Color.SetA(1f);
      }
    }
  }

  //Shared routes needs to be the max from any of the shared routes to other stars
  public void UpdateSettlementIndicatorSize(uint sharedRoutes){
    //Hardcoded from the GalaxyGenerator Min Star Size
    var minSettlementIndicatorSize = 0.06f * 0.5f;
    //Set the base size to the planet size
    var settlementIndicatorBaseSize = GalaxyScaleSize.x * 0.5f;
    // little padding (hardcoded to half the min galaxy generator star size now)
    var minPadding = 0.03f;

    var lineHalfWidth = GalaxyRouteLine.baseWidth / 4f;
    var routePadding = sharedRoutes * lineHalfWidth;

    settlementIndicatorRenderer.Radius = Mathf.Max(settlementIndicatorBaseSize + minPadding, minSettlementIndicatorSize + routePadding);
    var baseSettlementRadius = settlementIndicatorRenderer.Radius + settlementIndicatorRenderer.Thickness;

    //now update the positions of the import & export icons based on the settlement indicator size
    var iconDistance = baseSettlementRadius + 0.07f;
    var nameDistance = baseSettlementRadius + 0.05f;
    var up = Vector2.up * iconDistance;
    exportButton.transform.localPosition = up.Rotate(-30f);
    importButton.transform.localPosition = up.Rotate(30f);
    bookmarkIndicator.transform.localPosition = up.Rotate(-150f);
    multiNameText.transform.localPosition = (Vector2.up * nameDistance).Rotate(-150f);
    routeResourceAmount.transform.localPosition = exportButton.transform.localPosition.AddX(0.22f);
  }

  //For spawning Matryoshka Brain Layers
  public void UpdateSwarmDisplay(ushort layers, bool immediate){
    if(layers == 0){
      return;
    }

    swarmColorFader.FadeIn(immediate: immediate);

    const int ringsPerLayer = 15;
    const float spacingPerLayer = 0.3f;

    //If we already have the right number of rings for all the layers we're done
    if(swarmHolder.childCount == layers * ringsPerLayer){
      return;
    }

    //Otherwise just blow them away and start over to keep it simple
    var swarmRing = loader.Load<GameObject>("Prefabs/7/StarSwarmRing");
    swarmHolder.gameObject.SetActive(true);
    swarmHolder.DestroyChildren();


    for(var l = 0; l < layers; l++){
      for(var r = 0; r < ringsPerLayer; r++){
        var newRing = GameObject.Instantiate<GameObject>(swarmRing, swarmHolder);
        newRing.transform.rotation = Random.rotation;

        var disc = newRing.GetComponent<Disc>();
        disc.Radius = 0.4f + ((l + 1) * spacingPerLayer);
        disc.Color = StageSevenManager.VictoryLayersPalette.getColorAtIndex(l);

        var discAnimator = newRing.GetComponent<DiscOffsetAnimator>();
        discAnimator.speed = 0.5f - (l * 0.05f);
      }
    }

    swarmColorFader.UpdateComponents();
  }

  void HideSwarmDisplay(bool immediate){
    swarmColorFader.FadeOut(immediate: immediate);
    // swarmHolder.gameObject.SetActive(false);
  }

  GameResourceType? selectedRouteResource = null;
  public void ShowImportExportIndicators(GameResourceType routeResource){
    selectedRouteResource = routeResource;
    var hasResource = settlementData.resources.ContainsKey(routeResource);

    importButton.gameObject.SetActive(true);
    exportButton.gameObject.SetActive(true);

    UpdateResourceButtons();

    routeResourceAmount.gameObject.SetActive(true);
    routeResourceAmount.text = hasResource ? settlementData.resources[routeResource].amount.ToShortFormat() : string.Empty;
  }

  //For imports and exports while editing routes
  void UpdateResourceButtons(){
    var hasResource = selectedRouteResource.HasValue && settlementData.resources.ContainsKey(selectedRouteResource.Value);

    var importing = hasResource && settlementData.resources[selectedRouteResource.Value].importing;
    importButton.isSelected = importing;

    var exporting = hasResource && settlementData.resources[selectedRouteResource.Value].exporting;
    exportButton.isSelected = exporting;

    var buttonSelectedColor = UIColor.Green;
    var buttonNotSelectedColor = UIColor.DarkPurple;

    importButton.color = importButton.isSelected ? buttonSelectedColor : buttonNotSelectedColor;
    exportButton.color = exportButton.isSelected ? buttonSelectedColor : buttonNotSelectedColor;
  }

  public void HideImportExportIndicators(){
    importButton.gameObject.SetActive(false);
    exportButton.gameObject.SetActive(false);
    routeResourceAmount.gameObject.SetActive(false);

    selectedRouteResource = null;
  }


  void ImportClick(){
    if(!selectedRouteResource.HasValue){
      return; //Should never happen with when the buttons are enabled
    }
    if(!settlementData.resources.ContainsKey(selectedRouteResource.Value)){
      settlementData.resources[selectedRouteResource.Value] = new GalaxyResource(){
        type = selectedRouteResource.Value,
        amount = 0
      };
    }
    var resource = settlementData.resources[selectedRouteResource.Value];

    if(resource.importing){
      resource.importing = false;
    }else{
      resource.importing = true;
      resource.exporting = false;
    }

    UpdateResourceButtons();
    importExportChangedSignal.Dispatch(generatedData.id);
  }

  void ExportClick(){
    if(!selectedRouteResource.HasValue){
      return; //Should never happen with when the buttons are enabled
    }
    if(!settlementData.resources.ContainsKey(selectedRouteResource.Value)){
      settlementData.resources[selectedRouteResource.Value] = new GalaxyResource(){
        type = selectedRouteResource.Value,
        amount = 0
      };
    }
    var resource = settlementData.resources[selectedRouteResource.Value];

    if(resource.exporting){
      resource.exporting = false;
    }else{
      resource.exporting = true;
      resource.importing = false;
    }
    UpdateResourceButtons();
    importExportChangedSignal.Dispatch(generatedData.id);
  }

  void OnTransition(GalaxyTransitionInfo transitionInfo){
    var fadeTime = transitionInfo.skipAnimation ? 0f : GalaxyTransitioner.fadeTime;
    if(transitionInfo.to == GalaxyViewMode.Galaxy){
      if(transitionInfo.transitioner.SelectedStar != this && transitionInfo.transitioner.PrevSelectedStar != this){
        colorFader.FadeIn(fadeTime);
      }

      //Fade indicators back in
      if(generatedData.inhabited){
        settlementIndicatorColorFader.FadeIn();
        nameColorFader.FadeIn();
      }
    }

    //For Galaxy -> system and system -> system (prev & next button) transitions
    if(transitionInfo.from == GalaxyViewMode.Galaxy ||
      (transitionInfo.to == GalaxyViewMode.System && transitionInfo.from == GalaxyViewMode.System)
    ){
      if(generatedData.inhabited){
        settlementIndicatorColorFader.FadeOut(immediate: transitionInfo.skipAnimation);
        nameColorFader.FadeOut(immediate: transitionInfo.skipAnimation);
      }

      if(transitionInfo.transitioner.SelectedStar != this){
        colorFader.FadeOut(fadeTime);
      }else{
        colorFader.FadeIn(fadeTime);
      }
    }

    //Show swarm display for going to system view of a selected inhabited star
    if(transitionInfo.to == GalaxyViewMode.System && generatedData.inhabited && transitionInfo.transitioner.SelectedStar == this){
      currentlyViewingThisStarInSystem = true;

      UpdateSwarmDisplay(settlementData.GetMaxVictoryBuildingLevel(), transitionInfo.skipAnimation);
      // UpdateSwarmDisplay(6, transitionInfo.skipAnimation);
    }else{
      //Hide swarm for anything else
      HideSwarmDisplay(transitionInfo.skipAnimation);
      currentlyViewingThisStarInSystem = false;
    }

    //scale the size of the selected star when going to it
    if(transitionInfo.transitioner.SelectedStar == this && transitionInfo.to != GalaxyViewMode.Galaxy){
      LeanTween.scale(display, SystemScaleSize, fadeTime);
    }else{
      if(display.transform.localScale != GalaxyScaleSize){
        LeanTween.scale(display, GalaxyScaleSize, fadeTime);
      }
    }
  }

  void OnCelestialColonized(GalaxySettlementData settlementData){
    if(settlementData.parentStarId != generatedData.id){
      return;
    }

    if(stageSevenData.viewMode == GalaxyViewMode.Galaxy){
      settlementIndicatorColorFader.FadeIn();
      nameColorFader.FadeIn();
    }

    //Should only be able to colonize while not editing so should be safe to use the normal palette here
    UpdateDisplay(StageSevenManager.StarPalette);


    //This is a hack to set the star display size back to the system size if you're viewing the system when it gets colonized
    //Without this the start would go back to galaxy view size in update display.  Changing it in update display breaks other tweening stuff though
    if(stageSevenData.viewMode != GalaxyViewMode.Galaxy){
      display.transform.localScale = SystemScaleSize;
    }

    UpdateSettlementIndicatorSize(0);
  }

  void OnBuildingFinishedSignal(GalaxyBuildingData buildingData, uint starId, uint? celestialId){
    if(generatedData.id == starId && celestialId == null && currentlyViewingThisStarInSystem && GalaxyBuilding.allVictory.Contains(buildingData.buildingId)){
      UpdateSwarmDisplay(settlementData.GetMaxVictoryBuildingLevel(), false);
    }
  }

  static Dictionary<SpectralType, int> colorMap = new Dictionary<SpectralType, int>(){
    {SpectralType.O, 0},
    {SpectralType.B, 1},
    {SpectralType.A, 2},
    {SpectralType.F, 3},
    {SpectralType.G, 4},
    {SpectralType.K, 5},
    {SpectralType.M, 6},
  };


  //Table doesn't have anything for O & B types?
  public static List<StellarEvolutionData> stellarEvolutionTable = new List<StellarEvolutionData>(){
    new StellarEvolutionData{ mass = 0.10f, type = SpectralType.M, subType = 7, tempK = 3100, lMin = 0.0012f },
    new StellarEvolutionData{ mass = 0.15f, type = SpectralType.M, subType = 6, tempK = 3200, lMin = 0.0036f },
    new StellarEvolutionData{ mass = 0.20f, type = SpectralType.M, subType = 5, tempK = 3200, lMin = 0.0079f },
    new StellarEvolutionData{ mass = 0.25f, type = SpectralType.M, subType = 4, tempK = 3300, lMin = 0.015f },
    new StellarEvolutionData{ mass = 0.30f, type = SpectralType.M, subType = 4, tempK = 3300, lMin = 0.024f },
    new StellarEvolutionData{ mass = 0.35f, type = SpectralType.M, subType = 3, tempK = 3400, lMin = 0.037f },
    new StellarEvolutionData{ mass = 0.40f, type = SpectralType.M, subType = 2, tempK = 3500, lMin = 0.054f },
    new StellarEvolutionData{ mass = 0.45f, type = SpectralType.M, subType = 1, tempK = 3600, lMin = 0.07f, lMax = 0.08f, mSpan = 70 },
    new StellarEvolutionData{ mass = 0.50f, type = SpectralType.M, subType = 0, tempK = 3800, lMin = 0.09f, lMax = 0.11f, mSpan = 59 },

    new StellarEvolutionData{ mass = 0.55f, type = SpectralType.K, subType = 8, tempK = 4000, lMin = 0.11f, lMax = 0.15f, mSpan = 50 },
    new StellarEvolutionData{ mass = 0.60f, type = SpectralType.K, subType = 6, tempK = 4200, lMin = 0.13f, lMax = 0.20f, mSpan = 42 },
    new StellarEvolutionData{ mass = 0.65f, type = SpectralType.K, subType = 5, tempK = 4400, lMin = 0.15f, lMax = 0.25f, mSpan = 37 },
    new StellarEvolutionData{ mass = 0.70f, type = SpectralType.K, subType = 4, tempK = 4600, lMin = 0.19f, lMax = 0.35f, mSpan = 30 },
    new StellarEvolutionData{ mass = 0.75f, type = SpectralType.K, subType = 2, tempK = 4900, lMin = 0.23f, lMax = 0.48f, mSpan = 24 },
    new StellarEvolutionData{ mass = 0.80f, type = SpectralType.K, subType = 0, tempK = 5200, lMin = 0.28f, lMax = 0.65f, mSpan = 20 },

    new StellarEvolutionData{ mass = 0.85f, type = SpectralType.G, subType = 8, tempK = 5400, lMin = 0.36f, lMax = 0.84f, mSpan = 17 },
    new StellarEvolutionData{ mass = 0.90f, type = SpectralType.G, subType = 6, tempK = 5500, lMin = 0.45f, lMax = 1.00f, mSpan = 14 },
    new StellarEvolutionData{ mass = 0.95f, type = SpectralType.G, subType = 4, tempK = 5700, lMin = 0.56f, lMax = 1.30f, mSpan = 12,   sSpan = 1.8f, gSpan = 1.1f },
    new StellarEvolutionData{ mass = 1.00f, type = SpectralType.G, subType = 2, tempK = 5800, lMin = 0.68f, lMax = 1.60f, mSpan = 10,   sSpan = 1.6f, gSpan = 1.0f },
    new StellarEvolutionData{ mass = 1.05f, type = SpectralType.G, subType = 1, tempK = 5900, lMin = 0.87f, lMax = 1.90f, mSpan = 8.8f, sSpan = 1.4f, gSpan = 0.8f },
    new StellarEvolutionData{ mass = 1.10f, type = SpectralType.G, subType = 0, tempK = 6000, lMin = 1.10f, lMax = 2.20f, mSpan = 7.7f, sSpan = 1.2f, gSpan = 0.7f },

    new StellarEvolutionData{ mass = 1.15f, type = SpectralType.F, subType = 9, tempK = 6100, lMin = 1.40f, lMax = 2.60f, mSpan = 6.7f, sSpan = 1.0f, gSpan = 0.6f },
    new StellarEvolutionData{ mass = 1.20f, type = SpectralType.F, subType = 8, tempK = 6300, lMin = 1.70f, lMax = 3.00f, mSpan = 5.9f, sSpan = 0.9f, gSpan = 0.6f },
    new StellarEvolutionData{ mass = 1.25f, type = SpectralType.F, subType = 7, tempK = 6400, lMin = 2.10f, lMax = 3.50f, mSpan = 5.2f, sSpan = 0.8f, gSpan = 0.5f },
    new StellarEvolutionData{ mass = 1.30f, type = SpectralType.F, subType = 6, tempK = 6500, lMin = 2.50f, lMax = 3.90f, mSpan = 4.6f, sSpan = 0.7f, gSpan = 0.4f },
    new StellarEvolutionData{ mass = 1.35f, type = SpectralType.F, subType = 5, tempK = 6600, lMin = 3.10f, lMax = 4.50f, mSpan = 4.1f, sSpan = 0.6f, gSpan = 0.4f },
    new StellarEvolutionData{ mass = 1.40f, type = SpectralType.F, subType = 4, tempK = 6700, lMin = 3.70f, lMax = 5.10f, mSpan = 3.7f, sSpan = 0.6f, gSpan = 0.4f },
    new StellarEvolutionData{ mass = 1.45f, type = SpectralType.F, subType = 3, tempK = 6900, lMin = 4.30f, lMax = 5.70f, mSpan = 3.3f, sSpan = 0.5f, gSpan = 0.3f },
    new StellarEvolutionData{ mass = 1.50f, type = SpectralType.F, subType = 2, tempK = 7000, lMin = 5.10f, lMax = 6.50f, mSpan = 3.0f, sSpan = 0.5f, gSpan = 0.3f },
    new StellarEvolutionData{ mass = 1.60f, type = SpectralType.F, subType = 0, tempK = 7300, lMin = 6.70f, lMax = 8.20f, mSpan = 2.5f, sSpan = 0.4f, gSpan = 0.2f },

    new StellarEvolutionData{ mass = 1.70f, type = SpectralType.A, subType = 9, tempK = 7500, lMin = 8.60f, lMax = 10.0f, mSpan = 2.1f, sSpan = 0.3f, gSpan = 0.2f },
    new StellarEvolutionData{ mass = 1.80f, type = SpectralType.A, subType = 7, tempK = 7800, lMin = 11.0f, lMax = 13.0f, mSpan = 1.8f, sSpan = 0.3f, gSpan = 0.2f },
    new StellarEvolutionData{ mass = 1.90f, type = SpectralType.A, subType = 6, tempK = 8000, lMin = 13.0f, lMax = 16.0f, mSpan = 1.5f, sSpan = 0.2f, gSpan = 0.1f },
    new StellarEvolutionData{ mass = 2.00f, type = SpectralType.A, subType = 5, tempK = 8200, lMin = 16.0f, lMax = 20.0f, mSpan = 1.3f, sSpan = 0.2f, gSpan = 0.1f },
  };

  public static Dictionary<LuminosityClass, string> luminosityClassDescrip = new Dictionary<LuminosityClass, string>(){
    {LuminosityClass.I,   "Supergiant"},
    {LuminosityClass.II,  "Supergiant"},
    {LuminosityClass.III, "Giant"},
    {LuminosityClass.IV,  "Subgiant"},
    {LuminosityClass.V,   "Main Sequence Star"},
    {LuminosityClass.VI,  "Subdwarf"},
    {LuminosityClass.D,   "White Dwarf"},
  };
}

public enum SpectralType{
  O,
  B,
  A,
  F,
  G,
  K,
  M,
}

public enum LuminosityClass {
  I, //supergiant
  II,
  III, //giant
  IV, //subgiant
  V, //Main sequence
  VI, //subdwarf
  D //warf
}


public enum StarExploreStatus{
  Seen,
  LooksGood,
  LooksBad
}

public struct StellarEvolutionData {
  public float mass;
  public SpectralType type;
  public ushort subType;
  public int tempK;
  public float lMin; //initial luminosity
  public float lMax; // maximum luminosity
  public float mSpan; // main sequence span in billions of years
  public float sSpan; // ditto but subgiant span
  public float gSpan; // giant span
}
