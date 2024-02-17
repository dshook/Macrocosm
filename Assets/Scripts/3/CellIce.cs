using UnityEngine;

public class CellIce : MonoBehaviour {
  public SpriteRenderer display;
  public Sprite[] sprites;

  int spriteIdx = 0;

  public ObjectPool objectPool;

  protected void OnEnable(){
    spriteIdx = 0;
  }

  //Returns if the ice just broke
  public bool TapTheIce(){
    spriteIdx++;

    //Must be done if we don't have anything more to display
    if(spriteIdx >= sprites.Length){
      objectPool.Recycle(this.gameObject);
      return true;
    }
    return false;
  }

  void Update(){
    if(spriteIdx < sprites.Length){
      display.sprite = sprites[spriteIdx];
    }
  }
}