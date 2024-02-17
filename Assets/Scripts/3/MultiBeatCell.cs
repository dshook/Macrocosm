using UnityEngine;

public class MultiBeatCell : MonoBehaviour {

  public Collider2D col;
  public SpriteRenderer[] renderers;

  public Sprite[] unhitSprites;
  public Sprite[] hitSprites;

  public SpriteRenderer bonusRenderer;

  public bool hit = false;
  public bool bonus;

  protected void OnEnable(){
    if(renderers.Length != unhitSprites.Length || renderers.Length != hitSprites.Length){
      Debug.LogError("Multi cell renderer sprite mismatch");
      return;
    }

    hit = false;
    for(var i = 0; i < renderers.Length; i++){
      //Reset sorting order since clone's mess with it
      renderers[i].sortingOrder = i + 1;
      renderers[i].sprite = unhitSprites[i];
      renderers[i].color = Color.white;
      renderers[i].material.SetFloat("_Saturation", 1f);
    }

    bonusRenderer.gameObject.SetActive(bonus);
    bonusRenderer.color = Color.white;
    bonusRenderer.sortingOrder = 0;

  }

  public void GetHit(){
    hit = true;

    for(var i = 0; i < renderers.Length; i++){
      renderers[i].sprite = hitSprites[i];
      renderers[i].material.SetFloat("_Saturation", 1f);
    }
  }

  public void DoCloneThings(){
    for(var i = 0; i < renderers.Length; i++){
      renderers[i].sortingOrder--;
      renderers[i].material.SetFloat("_Saturation", 1f);
    }
    bonusRenderer.sortingOrder--;
    GetHit();
  }

  public void SetRendSaturation(float sat){
    foreach(var rend in renderers){
      rend.material.SetFloat("_Saturation", sat);
    }
  }

  public void FadeDisplay(float time, float delay){
    // Logger.LogWithFrame($"Fading display. Clone {isClone} color {rend.color}");

    foreach(var rend in renderers){
      LeanTween.color(rend.gameObject, rend.color.SetA(0), time).setDelay(delay);
    }
    LeanTween.color(bonusRenderer.gameObject, bonusRenderer.color.SetA(0), time).setDelay(delay);
  }
}