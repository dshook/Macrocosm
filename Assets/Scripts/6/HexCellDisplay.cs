using UnityEngine;

using TMPro;
using System.Collections.Generic;
using System;
using strange.extensions.mediation.impl;
using Shapes;

[SelectionBase]
public class HexCellDisplay : View {
  public HexCell hexCell;
  [Inject] public ResourceLoaderService loader {get; set;}
  [Inject] public StageSixDataModel stageSixData {get; set;}
  [Inject] HexCellExploredSignal hexCellExploredSignal {get; set;}
  [Inject] ObjectPool objectPool {get; set;}


  public TMP_Text label;
  public GameObject resourceHolder;
  public Transform foodIconHolder;
  public Transform productionIconHolder;
  public Transform scienceIconHolder;
  public SpriteRenderer cellRenderer;
  public RegularPolygon overlayRenderer;
  public SpriteRenderer featureRenderer;

  public GameObject bonusResourceHolder;
  public SpriteRenderer bonusResourceRenderer;

  public Transform buildingHolder;

  public GameObject resourceIconPrefab;

  public Vector3 Position {
    get { return transform.localPosition; }
  }

  public void UpdateTerrainDisplay(CellDrawMode drawMode = CellDrawMode.None, bool showFeatures = true){

#if UNITY_EDITOR
    //Hax for generating map in editor
    if(loader == null){
      loader = new ResourceLoaderService();
    }
#endif

    Color newColor = GetCellDisplayColor(hexCell, drawMode);
    Color overlayColor = Colors.transparent;

    if(hexCell.data.exploreStatus == HexExploreStatus.Partial && drawMode == CellDrawMode.None){
      overlayColor = Colors.transparentBlack50;
    }
    if(hexCell.data.exploreStatus == HexExploreStatus.Unexplored && drawMode == CellDrawMode.None){
      overlayColor = Colors.nearBlack;
      showFeatures = false;
    }
    if(hexCell.Selected){
      // overlayColor = Colors.transparentGreen;
    }
    if(hexCell.Selectable){
      overlayColor = Colors.darkPurple.SetA(hexCell.data.exploreStatus == HexExploreStatus.Unexplored ? 1 : 0.7f);
    }

    if(showFeatures && hexCell.HexFeature != HexFeature.None){
      featureRenderer.gameObject.SetActive(true);
      featureRenderer.sprite = loader.Load<Sprite>(featureSprites[hexCell.HexFeature]);
    }else{
      featureRenderer.gameObject.SetActive(false);
    }

    //Only show bonus resources for fully explored tiles and not where cities are
    if(hexCell.HexBonusResource != HexBonusResource.None &&
      (
        drawMode == CellDrawMode.Revealed ||
        (
          hexCell.data.exploreStatus == HexExploreStatus.Explored &&
          stageSixData.RevealedResource(hexCell.HexBonusResource)
        )
      )
      && hexCell.city == null
    ){
      bonusResourceHolder.SetActive(true);
      bonusResourceRenderer.sprite = loader.Load<Sprite>(bonusResourceSprites[hexCell.HexBonusResource]);

      //check to see if we need to flip the bonus resource display because a city would overlap
      var neighbor = hexCell.GetNeighbor(HexDirection.NW);
      if(neighbor != null && neighbor.city != null){
        bonusResourceHolder.transform.localScale = bonusResourceHolder.transform.localScale.SetX(-1);

        //then also have to check to see if any of the cells to the east need flipping too so they don't overlap
        var eastNeighbor = hexCell.GetNeighbor(HexDirection.E);
        while(
          eastNeighbor != null
          && eastNeighbor.HexBonusResource != HexBonusResource.None
          && eastNeighbor.display != null
        ){
          eastNeighbor.display.bonusResourceHolder.transform.localScale = eastNeighbor.display.bonusResourceHolder.transform.localScale.SetX(-1);
          eastNeighbor = eastNeighbor.GetNeighbor(HexDirection.E);
        }
      }

      //Also have to check if our westward neighbor has flipped the icon so we don't overlap
      var westNeighbor = hexCell.GetNeighbor(HexDirection.W);
      if(
        westNeighbor != null
        && westNeighbor.HexBonusResource != HexBonusResource.None
        && westNeighbor.display != null
        && westNeighbor.display.bonusResourceHolder.transform.localScale.x == -1
      ){
        bonusResourceHolder.transform.localScale = bonusResourceHolder.transform.localScale.SetX(-1);
      }
    }else{
      bonusResourceHolder.SetActive(false);
    }

    if(drawMode == CellDrawMode.Coordinates){
      label.gameObject.SetActive(true);
      label.text = hexCell.coordinates.ToStringOnSeparateLines();
    }else if(drawMode == CellDrawMode.MoveCost){
      label.gameObject.SetActive(true);
      label.text = hexCell.MoveCost(debugPathfindOptions).ToString();
    }else{
      label.gameObject.SetActive(false);
    }

    cellRenderer.color = newColor;

    if(overlayColor == Colors.transparent){
      overlayRenderer.gameObject.SetActive(false);
    }else{
      overlayRenderer.gameObject.SetActive(true);
      overlayRenderer.Color = overlayColor;
    }

    //Add buildings to the display.
    //This is mostly for the check of the load happens and the building is set on the cell
    //before the display is created
    if(hexCell.building.HasValue && buildingHolder.childCount == 0){
      var mapImagePath = CityBuilding.allBuildings[hexCell.building.Value].mapImagePath;
      if(!string.IsNullOrEmpty(mapImagePath)){
        AddBuildingDisplay(mapImagePath);
      }
    }
  }

  public static Color GetCellDisplayColor(HexCell hexCell, CellDrawMode drawMode = CellDrawMode.None){
    Color newColor;

    if(hexCell.TerrainType == HexTerrainType.Water){
      newColor = Colors.water;
    }else{
      newColor = terrainColors[hexCell.TerrainType];
    }
    newColor = newColor.ToHSV().ChangeV(0.1f * hexCell.Elevation).ToColor();

    //color by various things for debugging
    if(drawMode == CellDrawMode.Elevation && hexCell.Elevation > 0){
      newColor = Colors.land.ToHSV().ChangeV(0.1f * hexCell.Elevation).ToColor();
    }
    if(drawMode == CellDrawMode.Clouds && hexCell.Elevation > 0){
      newColor = Color.black.ToHSV().ChangeV(hexCell.climateData.clouds).ToColor();
    }
    if(drawMode == CellDrawMode.Moisture){
      if(!hexCell.IsUnderwater){
        newColor = Color.black.ToHSV().ChangeV(hexCell.climateData.moisture).ToColor();
      }
    }
    if(drawMode == CellDrawMode.Temperature){
      newColor = Color.black.ToHSV().ChangeV(hexCell.climateData.temperature).ToColor();
    }
    if(drawMode == CellDrawMode.Freshwater){
      if(hexCell.IsUnderwater){
        newColor = hexCell.Freshwater ? Colors.blue : Colors.red;
      }
    }
    if(drawMode == CellDrawMode.MoveCost){
      var moveCost = hexCell.MoveCost(debugPathfindOptions);
      if(moveCost < 0){
        newColor = Color.black;
      }else{
        newColor = Color.gray.ToHSV().ChangeV(0.1f * (HexCell.BaseMoveCost - moveCost)).ToColor();
      }
    }
    return newColor;
  }

  public void ShowResourcesDisplay(StageSixDataModel stageSixData){
    if(resourceHolder.activeInHierarchy){
      //Skip showing resources when we already are to avoid duplicating the icons
      return;
    }
    var hasResources = false;
    var food = hexCell.Food(stageSixData);
    if(food > 0){
      UpdateResourcesIcon(GameResourceType.Food, food, foodIconHolder);
      hasResources = true;
    }else{
      foodIconHolder.gameObject.SetActive(false);
    }
    var production = hexCell.Production(stageSixData);
    if(production > 0){
      UpdateResourcesIcon(GameResourceType.Production, production, productionIconHolder);
      hasResources = true;
    }else{
      productionIconHolder.gameObject.SetActive(false);
    }
    var science = hexCell.Science(stageSixData);
    if(science > 0){
      UpdateResourcesIcon(GameResourceType.Science, science, scienceIconHolder);
      hasResources = true;
    }else{
      scienceIconHolder.gameObject.SetActive(false);
    }

    resourceHolder.SetActive(hasResources);
  }

  public void UpdateResourcesIcon(GameResourceType type, int iconsNeeded, Transform iconHolder){
    const float normalIconScale = 0.17f;
    const float bigIconScale = 0.21f;

    iconHolder.gameObject.SetActive(true);
    var iconsPresent = iconHolder.childCount;
    if(iconsPresent > 0){
      Logger.LogWarning("Hex cell has leftover resource icons");
    }

    //Stacking icons for less than 5
    if(iconsNeeded <= 5){

      for(int i = 0; i < iconsNeeded; i++){
        SpawnResourceIcon(type, iconHolder, normalIconScale);
      }

    }else{
      //otherwise show the text label and a single icon
      var firstIcon = SpawnResourceIcon(type, iconHolder, bigIconScale);

      TMP_Text textTmp;
      //spawn label and set to value
      var labelPrefab = loader.Load<Transform>("Prefabs/6/CellResourceLabel");
      var newLabel = GameObject.Instantiate(labelPrefab, Vector3.zero, Quaternion.identity);
      newLabel.SetParent(firstIcon.transform, false);
      newLabel.transform.localPosition = new Vector2(-1.35f, 0);
      textTmp = newLabel.GetComponent<TMP_Text>();
      textTmp.text = iconsNeeded.ToString();
    }
  }

  GameObject SpawnResourceIcon(GameResourceType resourceType, Transform parent, float scale){
    var newIcon = objectPool.Spawn(resourceIconPrefab, parent, Vector3.zero);
    newIcon.transform.localScale = scale * Vector3.one;

    var iconImage = newIcon.GetComponent<SpriteRenderer>();

    iconImage.sprite = loader.Load<Sprite>(GameResource.resourceIconPaths[resourceType]);
    return newIcon;
  }

  //Helper to do the annoying logic to remove the cell resource label if it exists before recycling
  void RecycleResourceIcon(Transform icon){
    if(icon.childCount > 0){
      icon.DestroyChildren();
    }
    objectPool.Recycle(icon.gameObject);
  }

  public void AddBuildingDisplay(string mapImagePath){
    var buildingPrefab = loader.Load<GameObject>("Prefabs/6/CityBuilding");
    var newBuilding = GameObject.Instantiate(buildingPrefab, Vector3.zero, Quaternion.identity);

    newBuilding.transform.SetParent(buildingHolder, false);
    // newBuilding.transform.localPosition = cell.localPosition;

    var renderer = newBuilding.GetComponentInChildren<SpriteRenderer>();
    var buildingSprite = loader.Load<Sprite>(mapImagePath);
    renderer.sprite = buildingSprite;
  }

  //Assumes only one building with a display
  public void RemoveBuildingDisplay(){
    buildingHolder.DestroyChildren();
  }


  public void HideResourcesDisplay(){
    while(foodIconHolder.childCount > 0){
      RecycleResourceIcon(foodIconHolder.GetChild(0));
    }

    while(productionIconHolder.childCount > 0){
      RecycleResourceIcon(productionIconHolder.GetChild(0));
    }

    while(scienceIconHolder.childCount > 0){
      RecycleResourceIcon(scienceIconHolder.GetChild(0));
    }
    resourceHolder.SetActive(false);
  }

  public void ExploreUpdate(){
    hexCellExploredSignal.Dispatch();
    UpdateTerrainDisplay();
  }

  public static Dictionary<HexTerrainType, Color> terrainColors = new Dictionary<HexTerrainType, Color>(HexCell.terrainComparer) {
    {HexTerrainType.Sand, Colors.sand},
    {HexTerrainType.Grass, Colors.greenGrass},
    {HexTerrainType.Mud, Colors.land},
    {HexTerrainType.Stone, Colors.stone},
    {HexTerrainType.Snow, Colors.snow},
    {HexTerrainType.Ice, Colors.snow},
    {HexTerrainType.Shallows, Colors.shallows},
  };

  public static Dictionary<HexFeature, string> featureSprites = new Dictionary<HexFeature, string>(HexCell.featureComparer) {
    {HexFeature.Hills, "Art/stage6/cellFeatures/hex_hills"},
    {HexFeature.Mountains, "Art/stage6/cellFeatures/hex_mountains"},
    {HexFeature.Peak, "Art/stage6/cellFeatures/hex_peak"},
    {HexFeature.TreesDense, "Art/stage6/cellFeatures/hex_trees_dense"},
    {HexFeature.TreesMedium, "Art/stage6/cellFeatures/hex_trees_medium"},
    {HexFeature.TreesSparse, "Art/stage6/cellFeatures/hex_trees_sparse"},
    {HexFeature.Lake, "Art/stage6/cellFeatures/hex_lake"},
  };

  public static Dictionary<HexBonusResource, string> bonusResourceSprites = new Dictionary<HexBonusResource, string>(HexCell.bonusResourceComparer) {
    {HexBonusResource.Livestock, "Art/stage6/bonusResources/livestock"},
    {HexBonusResource.Grains, "Art/stage6/bonusResources/grains"},
    {HexBonusResource.Fish, "Art/stage6/bonusResources/fish"},
    {HexBonusResource.Stone, "Art/stage6/bonusResources/stone"},
    {HexBonusResource.Cotton, "Art/stage6/bonusResources/cotton"},
    {HexBonusResource.Iron, "Art/stage6/bonusResources/iron"},
    {HexBonusResource.Coal, "Art/stage6/bonusResources/coal"},
    {HexBonusResource.Horses, "Art/stage6/bonusResources/horses"},
    {HexBonusResource.Sugar, "Art/stage6/bonusResources/sugar"},
    {HexBonusResource.Salt, "Art/stage6/bonusResources/salt"},
    {HexBonusResource.Grapes, "Art/stage6/bonusResources/wine"},
    {HexBonusResource.Gold, "Art/stage6/bonusResources/gold"},
    {HexBonusResource.Reef, "Art/stage6/bonusResources/pearls"},
    {HexBonusResource.Aluminum, "Art/stage6/bonusResources/aluminum"},
    {HexBonusResource.Oil, "Art/stage6/bonusResources/oil"},
    {HexBonusResource.Uranium, "Art/stage6/bonusResources/uranium"},
  };

  public static HexGrid.PathfindOptions debugPathfindOptions = new HexGrid.PathfindOptions(){
    canMoveOnLand = true,
    canMoveOnWater = true
  };
}

public enum CellDrawMode {
  None, Revealed, Moisture, Clouds, Temperature, Elevation, Freshwater, Coordinates, MoveCost
}
