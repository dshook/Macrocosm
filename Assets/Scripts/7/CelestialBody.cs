using UnityEngine;
using System.Collections.Generic;
using PygmyMonkey.ColorPalette;
using Shapes;
using TMPro;

public class CelestialBody : GalaxyCelestial {
  [Inject] GalaxyTransitionSignal galaxyTransitionSignal {get; set;}
  [Inject] GalaxyBuildingFinishedSignal buildingFinishedSignal {get; set;}
  [Inject] PaletteService palettes {get; set;}

  public float baseScale = 0.1f;
  public Disc atmosphereRing;

  public ColorFader settlementIndicatorFader;
  public SpriteRenderer settlementIndicatorRenderer;

  public CelestialBodyData data;
  public Star star;
  public CelestialBody parentBody;

  public CircleCollider2D clickCollider;
  public CircleLine asteroidBeltCircleLine;

  public Transform resourceDisplayHolder;
  public ColorFader resourceDisplayFader;
  public GameObject celestialBodyResourceDisplayIconPrefab;
  public Transform ringHolder;
  public GameObject ringPrefab;

  //For all the stacking layers of the actual planet features
  public Transform cbLayersHolder;

  public SpriteRenderer habitabilityDisplayRenderer;
  public ColorFader habitabilityDisplayFader;

  public List<CelestialBody> childCelestialBodies = new List<CelestialBody>(); //only filled in when they're instanciated

  GalaxyViewMode currentViewMode;

  bool isSettled{
    get{
      return stageSevenData.settlements.ContainsKey(data.id);
    }
  }

  bool isExplored;
  public bool IsExplored{
    get{ return isExplored; }
  }

  protected override void OnEnable(){
    base.OnEnable();

    galaxyTransitionSignal.AddListener(OnTransition);
    buildingFinishedSignal.AddListener(OnBuildingFinishedSignal);
  }

  protected override void OnDisable(){
    galaxyTransitionSignal.RemoveListener(OnTransition);
    buildingFinishedSignal.RemoveListener(OnBuildingFinishedSignal);
    childCelestialBodies.Clear();

    base.OnDisable();
  }

  //Track state for the planet's color palette based on its subtype and id, and have a way to cycle through the colors
  ColorPalette _planetColorPalette;
  int _colorPaletteIndex = 0;

  void SetPlanetColorPalette(){
    _colorPaletteIndex = 0;
    _planetColorPalette = palettes.PlanetPalette(data.subType, data.id);
  }
  Color GetNextPlanetColor(){
    var c = _planetColorPalette.getColorAtIndex(_colorPaletteIndex);
    _colorPaletteIndex = (_colorPaletteIndex + 1) % _planetColorPalette.colorInfoList.Count;
    return c;
  }

  // protected override void Awake() {
  //   base.Awake();
  // }

  // protected override void OnDestroy(){
  // }

  void Update(){
    UpdateSettlementIndicator();
  }

  public void UpdateDisplay(bool isExplored){
    this.isExplored = isExplored;

    if(data.type == CelestialBodyType.AsteroidBelt){
      InitAsteroidBelt();
    }else{
      display.transform.localScale = Vector3.one * baseScale * data.radius;
      var unexploredColor = palettes.stage7.getColorFromName("Millbrook").color;
      SetPlanetColorPalette();

      //Update planet body color
      if(!isExplored){
        //No info state when not explored
        baseShapeRenderer.Color = unexploredColor;
      }else{
        baseShapeRenderer.Color = GetNextPlanetColor();
      }

      //Gas giants use their primary palette color for the atmosphere color because they're made of gas...
      var atmosphereColor = data.type == CelestialBodyType.GasGiant ? baseShapeRenderer.Color :
        palettes.stage7Atmospheres.getColorFromName(data.atmosphereType.ToString()).color;

      //Update atmosphere display
      if(!isExplored ||
        data.atmosphereType == CelestialBodyAtmosphereType.None ||
        data.atmospherePressure == CelestialBodyAtmospherePressure.None
      ){
        atmosphereRing.gameObject.SetActive(false);
      }else{
        atmosphereRing.gameObject.SetActive(true);

        atmosphereRing.ColorInner = atmosphereColor;
        atmosphereRing.ColorOuter = atmosphereColor.SetA(0);
        atmosphereRing.Thickness = atmosphereThicknesses[(int)data.atmospherePressure];

        var baseDisc = baseShapeRenderer as Disc;
        if(baseDisc != null){
          atmosphereRing.Radius = baseDisc.Radius + (atmosphereRing.Thickness / 2f);
        }
      }

      //Other layers
      if(isExplored){
        BuildPlanetLayers(atmosphereColor);
      }else{
        //remove any previous recycled layers
        cbLayersHolder.DestroyChildren();
      }

      //rings
      //Currently the rings with the opaque setting to make them have the correct depth
      //settings don't work with fading out (since they're always opaque!)
      if(data.rings != null && data.rings.Length > 0){
        ringHolder.rotation = Quaternion.Euler(data.ringRotation);

        if(ringHolder.childCount != data.rings.Length && ringHolder.childCount > 0){
          ringHolder.DestroyChildren(!Application.isPlaying);
        }

        if(ringHolder.childCount != data.rings.Length){
          for(var i = 0; i < data.rings.Length; i++){
            var newRing = GameObject.Instantiate<GameObject>(ringPrefab, ringHolder);
          }
        }

        for(var i = 0; i < data.rings.Length; i++){
          var torus = ringHolder.GetChild(i).GetComponent<Torus>();
          var ring = data.rings[i];

          torus.Radius = ring.radius;
          torus.Thickness = ring.thickness;
          torus.Color = isExplored ? GetNextPlanetColor() : unexploredColor;
        }
      }else{
        //clean up any residual rings
        ringHolder.DestroyChildren(!Application.isPlaying);
      }


    }

    //spawn or update resource icons. Spawn them incrementally as needed and disable extra ones, so as we're recycling the CB's they can be reused
    if(isExplored && data.resourceDeposits != null){

      //disable extra ones
      if(resourceDisplayHolder.childCount > data.resourceDeposits.Length){
        for(var i = data.resourceDeposits.Length; i < resourceDisplayHolder.childCount; i++){
          resourceDisplayHolder.GetChild(i).gameObject.SetActive(false);
        }
      }

      //spawn needed ones
      for(var i = resourceDisplayHolder.childCount; i < data.resourceDeposits.Length; i++){
        GameObject.Instantiate(celestialBodyResourceDisplayIconPrefab, Vector3.zero, Quaternion.identity, resourceDisplayHolder);
      }

      //And update displays
      for(var r = 0; r < data.resourceDeposits.Length; r++){
        var rd = data.resourceDeposits[r];
        var resourceDisplayTransform = resourceDisplayHolder.GetChild(r);
        resourceDisplayTransform.gameObject.SetActive(true);
        var resourceDisplay = resourceDisplayHolder.GetChild(r).GetComponent<CelestialBodyResourceDisplay>();
        resourceDisplay.data = data;
        resourceDisplay.stageSevenData = stageSevenData;
        resourceDisplay.resourceType = rd.type;
        resourceDisplay.galaxyTransitionSignal = galaxyTransitionSignal;

        resourceDisplay.Init();
      }
    }else{
      //disable all the resource icons if we shouldn't be showing them
      for(var i = 0; i < resourceDisplayHolder.childCount; i++){
        resourceDisplayHolder.GetChild(i).gameObject.SetActive(false);
      }
    }

    //Update habitability icon
    if(isExplored && !isSettled){
      habitabilityDisplayRenderer.gameObject.SetActive(true);
      habitabilityDisplayRenderer.sprite = loader.Load<Sprite>(cbHabitabilityIcons[data.habitability]);
    }else{
      habitabilityDisplayRenderer.gameObject.SetActive(false);
    }

    resourceDisplayFader.UpdateComponents();
    colorFader.UpdateComponents();
    habitabilityDisplayFader.UpdateComponents();
  }

  public void InitAsteroidBelt(){

    var radius = Galaxy.GetSystemViewScale(data.parentDistanceAU);
    var width = 0.2f;
    switch(data.sizeClass){
      case CelestialBodySizeClass.Tiny: width = 0.01f; break;
      case CelestialBodySizeClass.Small: width = 0.015f; break;
      case CelestialBodySizeClass.Standard: width = 0.02f; break;
      case CelestialBodySizeClass.Large: width = 0.025f; break;
    }
    asteroidBeltCircleLine.width = width;
    asteroidBeltCircleLine.radius = radius;

    clickCollider.radius = width;
    clickCollider.offset = new Vector2(0, radius);

    display.transform.localPosition = new Vector3(0, -radius, 0);
    // settlementIndicatorFader.gameObject.transform.localPosition = new Vector3(0, radius, 0);

    //set rotation speed based on radius
    var rotater = asteroidBeltCircleLine.transform.GetComponent<Rotater>();
    rotater.speed = new Vector3(0, 0, (1f / data.parentDistanceAU) * 0.55f);
  }

  //For terrestrial and gas giants
  void BuildPlanetLayers(Color atmosphereColor){
    cbLayersHolder.DestroyChildren();

    /*
    TODO:
      Color palettes based on climate
      Special things for some subtypes? (ice, hadean, chthonian?)

      Diff atmosphere layers for gas giants
    */

    if(data.type == CelestialBodyType.Terrestrial){

      var layerRenderer = AddLayer();

      if(data.hydrographicCoverage > 0){
        var landLevel = 1;
        if(data.hydrographicCoverage < 0.75f){
          landLevel = 2;
        }else if(data.hydrographicCoverage < 0.25f){
          landLevel = 3;
        }
        //For worlds with liquid surfaces choose one of the terrestrial arts based on how much liquid
        var layerArt = loader.Load<Sprite>("Art/stage7/planet/terrestrial/terrestrial_land_" + landLevel);
        layerRenderer.sprite = layerArt;
        layerRenderer.color = GetNextPlanetColor();

        if(data.subType == CelestialBodySubType.Garden){
          var overlayRenderer = AddLayer();
          var overlayArt = loader.Load<Sprite>("Art/stage7/planet/terrestrial/terrestrial_land_overlay");
          overlayRenderer.sprite = overlayArt;
          overlayRenderer.color = GetNextPlanetColor();
        }

      }else{
        //for worlds without liquid, show craters based on their atmosphere and tectonic levels
        var craterLevel = 3;
        if(data.atmospherePressure == CelestialBodyAtmospherePressure.Standard){
          craterLevel = 2;
        }else if(
          data.atmospherePressure == CelestialBodyAtmospherePressure.Dense ||
          data.atmospherePressure == CelestialBodyAtmospherePressure.Superdense
        ){
          craterLevel = 1;
        }

        if(
          data.tectonicActivity == CelestialBodyPropertyActivity.Extreme ||
          data.tectonicActivity == CelestialBodyPropertyActivity.Heavy
        ){
          craterLevel -= 1;
        }

        craterLevel = Mathf.Clamp(craterLevel, 1, 3);

        //For worlds with liquid surfaces choose one of the terrestrial arts based on how much liquid
        var layerArt = loader.Load<Sprite>("Art/stage7/planet/terrestrial/terrestrial_craters_" + craterLevel);
        layerRenderer.sprite = layerArt;
        layerRenderer.color = GetNextPlanetColor();

      }

      //Special Ice layer
      if(data.subType == CelestialBodySubType.Ice){
        var iceRenderer = AddLayer();
        var layerArt = loader.Load<Sprite>("Art/stage7/planet/terrestrial/terrestrial_ice");
        iceRenderer.sprite = layerArt;
        iceRenderer.color = GetNextPlanetColor();
      }

      //atmosphere
      if(
        data.atmosphereType != CelestialBodyAtmosphereType.None &&
        data.atmospherePressure != CelestialBodyAtmospherePressure.None
      ){
        var cloudLevel = 1;
        if(data.atmospherePressure == CelestialBodyAtmospherePressure.Standard){
          cloudLevel = 2;
        }else if(
          data.atmospherePressure == CelestialBodyAtmospherePressure.Dense ||
          data.atmospherePressure == CelestialBodyAtmospherePressure.Superdense
        ){
          cloudLevel = 3;
        }

        var cloudArt = loader.Load<Sprite>("Art/stage7/planet/atmosphere/terrestrial_clouds_" + cloudLevel);

        var atmoRenderer = AddLayer("Prefabs/7/planet/Clouds");
        atmoRenderer.color = atmosphereColor;
        atmoRenderer.sprite = cloudArt;
      }

    }else if(data.type == CelestialBodyType.GasGiant && data.subType != CelestialBodySubType.Cloudless){

      string cloudPrefix = GasGiantCloudPrefixes[data.id % GasGiantCloudPrefixes.Length];

      if(!string.IsNullOrEmpty(cloudPrefix)){
        for(var i = 1; i <= 3; i++){
          var cloudArt = loader.Load<Sprite>(cloudPrefix + i);

          var atmoRenderer = AddLayer("Prefabs/7/planet/Clouds");
          atmoRenderer.color = GetNextPlanetColor();
          atmoRenderer.sprite = cloudArt;

          var scrollMult = 1;
          if(data.id % 3 == 0){
            atmoRenderer.transform.localScale = atmoRenderer.transform.localScale.SetX(-1);
            scrollMult = -1;
          }
          if(data.id % 2 == 0){
            atmoRenderer.transform.localScale = atmoRenderer.transform.localScale.SetY(-1);
          }
          atmoRenderer.material.SetVector("_TextureScroll", new Vector4(scrollMult * (0.05f + (i * 0.1f)), 0, 0, 0));

        }

      }
    }
  }

  static string[] GasGiantCloudPrefixes = new string[]{
    "Art/stage7/planet/atmosphere/gasgiant_clouds_",
    "Art/stage7/planet/atmosphere/gasgiant_swoops_",
    "Art/stage7/planet/atmosphere/gasgiant_stripes_",
    "Art/stage7/planet/atmosphere/gasgiant_waves_",
  };

  SpriteRenderer AddLayer(string prefabPath = null){
    var landlayer = loader.Load<GameObject>(prefabPath ?? "Prefabs/7/planet/LandLayer");

    var layer = GameObject.Instantiate<GameObject>(landlayer, cbLayersHolder);
    var layerRenderer = layer.GetComponent<SpriteRenderer>();
    layerRenderer.sortingOrder = 5 + cbLayersHolder.childCount;

    return layerRenderer;
  }

  bool showingSettlementIndicator = true; //initially true synced up with the prefab initial state

  void UpdateSettlementIndicator(bool skipAnimation = false){

    var shouldShowSettlementIndicator = isSettled && currentViewMode == GalaxyViewMode.System;

    if(shouldShowSettlementIndicator && !showingSettlementIndicator){
      settlementIndicatorFader.FadeIn(GalaxyTransitioner.fadeTime, skipAnimation);
      showingSettlementIndicator = true;

      //really should always try to update this but there's a bug when transitioning between system and the CB is being destroyed
      var settlementBuilding = stageSevenData.settlements[data.id].GetSettlementBuilding();
      settlementIndicatorRenderer.sprite = loader.Load<Sprite>(cbSettlementIcons[settlementBuilding.tier]);
    }

    if(!shouldShowSettlementIndicator && showingSettlementIndicator){
      //Skip animation for unsettled worlds to never show the indicator
      settlementIndicatorFader.FadeOut(GalaxyTransitioner.fadeTime, !isSettled || skipAnimation);
      showingSettlementIndicator = false;
    }
  }

  void TransitionIn(float animTime, bool skipAnimation){
    //skip if already faded in little hacky but prevents fading in the currently selected cb when going to system view
    if(colorFader.finished && colorFader.fadingOut){
      colorFader.FadeOut(0f, true);
      colorFader.FadeIn(animTime, skipAnimation);
      resourceDisplayFader.FadeIn(animTime, skipAnimation);
      habitabilityDisplayFader.FadeIn(animTime, skipAnimation);
    }

    //atmosphere ring fade in
    if(atmosphereRing.gameObject.activeSelf){
      LeanTween.value(atmosphereRing.gameObject, 0, 1f, animTime).setOnUpdate(SetAtmosphereInnerColorAlpha);
    }
  }

  void SetAtmosphereInnerColorAlpha(float newAlpha){
    atmosphereRing.ColorInner = atmosphereRing.ColorInner.SetA(newAlpha);
  }

  void TransitionOut(float animTime, bool skipAnimation){
    colorFader.FadeOut(animTime, skipAnimation);
    resourceDisplayFader.FadeOut(animTime, skipAnimation);
    habitabilityDisplayFader.FadeOut(animTime, skipAnimation);

    //atmosphere ring fade out
    if(atmosphereRing.gameObject.activeSelf){
      LeanTween.value(atmosphereRing.gameObject, 1f, 0f, animTime).setOnUpdate(SetAtmosphereInnerColorAlpha);
    }
  }

  void OnTransition(GalaxyTransitionInfo transitionInfo){
    currentViewMode = transitionInfo.to;
    var skipAnimation = transitionInfo.skipAnimation;
    var animTime = skipAnimation ? 0f : GalaxyTransitioner.fadeTime;

    if(transitionInfo.from == GalaxyViewMode.Planet || transitionInfo.from == GalaxyViewMode.Galaxy){
      TransitionIn(animTime, skipAnimation);
    }

    if(transitionInfo.to == GalaxyViewMode.Planet){
      //Only transition out if this isn't the selected CB or a parent of the selected CB
      if(transitionInfo.transitioner.SelectedCb != this &&
         (transitionInfo.transitioner.SelectedCb.parentBody == null || transitionInfo.transitioner.SelectedCb.parentBody != this)
      ){
        TransitionOut(animTime, skipAnimation);
      }

      //remove resource indicator if we're going to the selected cb
      if(transitionInfo.transitioner.SelectedCb == this){
        resourceDisplayFader.FadeOut(animTime, skipAnimation);
        habitabilityDisplayFader.FadeOut(animTime, skipAnimation);
      }
    }

    if(transitionInfo.to == GalaxyViewMode.System){
      //If going from a selected cb fade the resources back in
      if(resourceDisplayFader.finished && resourceDisplayFader.fadingOut){
        resourceDisplayFader.FadeIn(animTime, skipAnimation);
        habitabilityDisplayFader.FadeIn(animTime, skipAnimation);
      }
    }

    if(transitionInfo.to == GalaxyViewMode.Galaxy){
      TransitionOut(animTime, skipAnimation);
    }

    UpdateSettlementIndicator(skipAnimation);
  }

  //indexed by CelestialBodyAtmospherePressure
  static float[] atmosphereThicknesses = new float[]{
    0,
    0.04f,
    0.06f,
    0.1f,
    0.2f,
    0.3f
  };

  public bool isMoon{
    get{
      return data.parentId != data.parentStarId;
    }
  }

  public GalaxyBuildingId GetColonyShipLevelNeeded(){
    if(data.type == CelestialBodyType.Terrestrial && data.habitability >= CelestialBodyHabitability.Moderate ){
      return GalaxyBuildingId.Colony1;
    }

    if(
      (data.type == CelestialBodyType.Terrestrial || data.type == CelestialBodyType.GasGiant) &&
      data.habitability >= CelestialBodyHabitability.Poor ){
      return GalaxyBuildingId.Colony2;
    }

    if( data.habitability >= CelestialBodyHabitability.Poor ){
      return GalaxyBuildingId.Colony3;
    }

    if( data.habitability >= CelestialBodyHabitability.Terrible ){
      return GalaxyBuildingId.Colony4;
    }

    return GalaxyBuildingId.Colony5;
  }

  void OnBuildingFinishedSignal(GalaxyBuildingData buildingData, uint starId, uint? celestialId){
    if(star.generatedData.id == starId && celestialId.HasValue && celestialId.Value == data.id){
      UpdateDisplay(true);
    }
  }

  public static Dictionary<CelestialBodyHabitability, string> cbHabitabilityIcons = new Dictionary<CelestialBodyHabitability, string>(){
    {CelestialBodyHabitability.Atrocious, "Art/stage7/habitability/atrocious" },
    {CelestialBodyHabitability.Terrible, "Art/stage7/habitability/pain" },
    {CelestialBodyHabitability.Poor, "Art/stage7/habitability/sad" },
    {CelestialBodyHabitability.Moderate, "Art/stage7/habitability/meh" },
    {CelestialBodyHabitability.Wonderful, "Art/stage7/habitability/smile" },
    {CelestialBodyHabitability.Excellent, "Art/stage7/habitability/happy" },
  };

  public static Dictionary<ushort, string> cbSettlementIcons = new Dictionary<ushort, string>(){
    {1, "Art/stage7/settlement_1" },
    {2, "Art/stage7/settlement_2" },
    {3, "Art/stage7/settlement_3" },
    {4, "Art/stage7/settlement_4" },
    {5, "Art/stage7/settlement_5" },
  };
}