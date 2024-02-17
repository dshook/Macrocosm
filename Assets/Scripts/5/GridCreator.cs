using UnityEngine;

public class GridCreator : MonoBehaviour {

  public int width;
  public int height;

  public float tileSpacing;
  public GameObject tilePrefab;
  public GameObject linePrefab;

  public void Create(){

    var tdGrid = new TdGrid();
    tdGrid.Create(width, height, tileSpacing, tilePrefab, linePrefab, this.transform, 1);

  }

  public void Clear(){
    transform.DestroyChildren(true);
  }
}