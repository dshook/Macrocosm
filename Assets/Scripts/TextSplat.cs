using UnityEngine;
using TMPro;
using System;

public class TextSplat : MonoBehaviour
{
  public string text { get; set; }
  public Color color { get;set; }
  public bool animate = true;
  public float? delay = null;
  public float ttl = 1f;
  public float fontSize = 5f;
  public float? moveUpPct = 1f;
  public float? punchSize = 1.2f;

  public GameObject display;

  public ObjectPool objectPool;

  float timeAccum = 0f;
  TextMeshPro damageSplatText;

  void OnEnable(){
    timeAccum = 0f;
    ttl = 1f;
    fontSize = 5f;
    moveUpPct = 1f;
    punchSize = 1.2f;
  }

  public void Init()
  {
    damageSplatText = display.GetComponent<TextMeshPro>();

    if(damageSplatText != null){
      damageSplatText.color = color;
      damageSplatText.text = text;
      damageSplatText.fontSize = fontSize;
    }

    if(delay.HasValue){
      display.SetActive(false);
    }

    if (animate)
    {
      gameObject.transform.localScale = Vector3.one;
      var animateDelay = delay ?? 0;

      if(punchSize.HasValue){
        gameObject.transform.localScale = Vector3.zero;
        LeanTween.scale(gameObject, Vector3.one * punchSize.Value, 0.8f)
          .setEase(LeanTweenType.easeOutElastic)
          .setDelay(animateDelay);
      }

      if(damageSplatText != null){
        LeanTween.textColor(display, color.SetA(0), ttl)
          .setDelay(animateDelay);
        ;
      }else{
        LeanTween.color(display, color.SetA(0), ttl)
          .setDelay(animateDelay);
      }

      if(moveUpPct.HasValue){
        LeanTween.moveLocal(gameObject, transform.position + (Vector3.up * 1.0f * moveUpPct.Value), 3.5f)
          .setDelay(animateDelay);
      }
    }
  }

  void Update()
  {
    timeAccum += Time.deltaTime;
    if(delay.HasValue && timeAccum > delay.Value){
      display.SetActive(true);
    }
    if (timeAccum > ttl + (delay ?? 0))
    {
      objectPool.Recycle(this.gameObject);
    }
  }

}