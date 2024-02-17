using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;

public class TowerSelection : View {
  public GameObject display;

  public ShinyButton cancelButton;
  public ShinyButton hideButton;
  public ShinyButton sellButton;
  public ShinyButton upgradeButton;

  public GameObject hidePanel;
  public GameObject newTowerPanel;
  public GameObject existingTowerPanel;
  public UpgradePanelView upgradePanelView;
  public GameObject hqTowerPanel;
  public HqPanelView hqPanelView;
  public GameObject towerRadius;

  public TowerSelectView[] towerSelections;
  public TowerSelectView existingTowerSelectView;

  [Inject] TimeService time {get; set;}
  [Inject] TowerBuiltSignal towerBuilt {get; set;}
  [Inject] TowerSellSignal towerSell {get; set;}
  [Inject] TowerUpgradeSignal towerUpgrade {get; set;}
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}

  TdGrid grid;
  TdTile tdTile;
  Color originalTileColor;

  bool hiding = false;

  protected override void Awake() {
    base.Awake();

    cancelButton.onClick.AddListener(EndSelect);
    sellButton.onClick.AddListener(SellTower);
    upgradeButton.onClick.AddListener(UpgradeTower);

    hideButton.onPointerDown.AddListener(StartHide);
    hideButton.onPointerUp.AddListener(EndHide);

    foreach(var towerSelection in towerSelections){
      towerSelection.button.onClick.AddListener(() => SelectTower(towerSelection.towerType));
    }
  }

  public void StartHide(){
    hiding = true;
  }

  public void EndHide(){
    hiding = false;
  }

  void Update () {

    if(hiding){
      hidePanel.SetActive(false);
    }else{
      hidePanel.SetActive(true);
    }
  }

  public void SetGrid(TdGrid g){
    grid = g;
  }

  public void StartSelect(TdTile tile){
    if(grid == null){
      Debug.LogError("Grid not set up in tower selection");
      return;
    }
    originalTileColor = tile.svgRenderer.color;

    //Upgrade a tower
    if(tile.tower != null && !tile.tower.isGhost){
      existingTowerPanel.SetActive(true);
      newTowerPanel.SetActive(false);
      hqTowerPanel.SetActive(false);

      existingTowerSelectView.towerType = tile.tower.stats.type;
      existingTowerSelectView.UpdateDescrip();
      existingTowerSelectView.UpdateCosts(tile.tower.MoneySellValue, tile.tower.WoodSellValue, tile.tower.OreSellValue);
      UpdateTowerRadius(tile);
      upgradePanelView.UpdateText(tile.tower.stats.type, tile.tower.towerLevel, stageFiveData);
    }else{
      //build a new tower
      var towerLevel = 0;
      var nextToWater = false;
      var nextToOre = false;
      var nextToWood = false;
      //check to see if tile is next to any resources
      foreach(var neighborOffet in TdGrid.neighborOffsets){
        var neighborTile = grid.GetTile(tile.pos + neighborOffet);
        if(neighborTile == null) continue;

        if(neighborTile.terrain == TileTerrain.Water){  nextToWater = true; }
        if(neighborTile.terrain == TileTerrain.Forest){ nextToWood = true; }
        if(neighborTile.terrain == TileTerrain.Ore){    nextToOre = true; }
      }

      //then deactivate any of the towers needed
      for(int t = 0; t < towerSelections.Length; t++){
        var ts = towerSelections[t];
        ts.button.interactable = true;

        if(!Tower.CanAffordTower(loader, ts.towerType, towerLevel, stageFiveData)){
          ts.button.interactable = false;
        }

        if(ts.towerType == TdTowerType.Farm){
          ts.button.interactable = ts.button.interactable && nextToWater;
        }
        if(ts.towerType == TdTowerType.Quarry){
          ts.button.interactable = ts.button.interactable && nextToOre;
        }
        if(ts.towerType == TdTowerType.Lumber){
          ts.button.interactable = ts.button.interactable && nextToWood;
        }
      }

      tile.svgRenderer.color = Colors.yellow;

      existingTowerPanel.SetActive(false);
      newTowerPanel.SetActive(true);
      hqTowerPanel.SetActive(false);
    }

    display.SetActive(true);
    tdTile = tile;
    time.Pause();
  }

  public void SelectHq(TdTile tile){
    existingTowerPanel.SetActive(false);
    newTowerPanel.SetActive(false);
    hqTowerPanel.SetActive(true);

    hqPanelView.UpdateText(grid);

    display.SetActive(true);
    time.Pause();
  }

  void EndSelect(){
    towerRadius.SetActive(false);
    display.SetActive(false);
    if(tdTile != null){
      tdTile.svgRenderer.color = originalTileColor;
    }
    // tdTile = null;
    time.Resume();
  }

  void SelectTower(TdTowerType type){
    EndSelect();
    towerBuilt.Dispatch(new TowerBuiltData(){ tile = tdTile, towerType = type});
  }

  void SellTower(){
    EndSelect();
    towerSell.Dispatch(new TowerBuiltData(){ tile = tdTile, towerType = tdTile.tower.stats.type});
  }

  void UpdateTowerRadius(TdTile tile){
    if(tile.tower != null && tile.tower.stats.hasRadius){
      towerRadius.SetActive(true);
      towerRadius.transform.position = tile.go.transform.position;
      towerRadius.transform.localScale = tile.tower.stats.radius[tile.tower.towerLevel] * 2f * Vector3.one;
    }
  }

  void UpgradeTower(){
    towerUpgrade.Dispatch(new TowerBuiltData(){ tile = tdTile, towerType = tdTile.tower.stats.type});
    upgradePanelView.UpdateText(tdTile.tower.stats.type, tdTile.tower.towerLevel, stageFiveData);
    existingTowerSelectView.UpdateCosts(tdTile.tower.MoneySellValue, tdTile.tower.WoodSellValue, tdTile.tower.OreSellValue);
  }
}