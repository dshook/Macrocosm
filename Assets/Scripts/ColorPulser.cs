using TMPro;
using UnityEngine;

public class ColorPulser : MonoBehaviour {

  public float pulseDuration = 1.5f;
  public float delay = 0f;
  public Gradient colorGradient;

  public bool started = false;
  public bool useChildComponents = false;

  public bool useUnscaledTime = false;

  SpriteRenderer xRenderer;
  LineRenderer lineRenderer;
  TMP_Text textRenderer;

  SpriteRenderer[] xRendererArray;
  LineRenderer[] lineRendererArray;
  TMP_Text[] textRendererArray;

  float timer = 0f;

  void Awake () {
    UpdateComponents();
  }

  public void UpdateComponents(){
    if(useChildComponents){
      xRendererArray = GetComponentsInChildren<SpriteRenderer>(true);
      lineRendererArray = GetComponentsInChildren<LineRenderer>(true);
      textRendererArray = GetComponentsInChildren<TMP_Text>(true);
    }else{
      xRenderer = GetComponent<SpriteRenderer>();
      lineRenderer = GetComponent<LineRenderer>();
      textRenderer = GetComponent<TMP_Text>();
    }
  }

  void Update () {
    timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    if(!started || timer < delay){
      return;
    }

    var realTime = timer - delay;
    float pct = Mathf.Clamp01((realTime % pulseDuration) / pulseDuration );
    var destColor = colorGradient.Evaluate(pct);

    if(useChildComponents){
      if(xRendererArray != null){
        foreach(var xRend in xRendererArray){
          if(xRend == null){ continue; }
          xRend.color = destColor;
        }
      }

      if(lineRendererArray != null){
        foreach(var lineRend in lineRendererArray){
          if(lineRend == null){ continue; }
          lineRend.startColor = destColor;
          lineRend.endColor = destColor;
        }
      }

      if(textRendererArray != null){
        foreach(var textRend in textRendererArray){
          if(textRend == null){ continue; }
          textRend.color = destColor;
        }
      }
    }else{
      if(xRenderer != null){
        xRenderer.color = destColor;
      }

      if(lineRenderer != null){
        lineRenderer.startColor = destColor;
        lineRenderer.endColor = destColor;
      }

      if(textRenderer != null){
        textRenderer.color = destColor;
      }
    }

  }

  public void Play(){
    if(!started){
      timer = 0f;
      started = true;
      Update();
    }
  }

  public void Pause(){
    started = false;
  }

}
