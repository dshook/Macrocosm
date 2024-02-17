using System;
using Shapes;
using TMPro;
using UnityEngine;

public class ColorFader : MonoBehaviour {

  public float timeToLive = 1.5f;
  public float delay = 0f;

  public bool useChildComponents = false;

  [Tooltip("If specified, only control these components instead of just this object or its children")]
  public Transform[] componentsToFade = null;

  [Tooltip("If specified, also control these components instead of just this object or its children")]
  public Transform[] componentsWithChildrenToFade = null;

  public bool fadingOut = true;
  public bool started = true;
  public bool toggleGameObjectActive = false;
  [Tooltip("Run even when the game is paused")]
  public bool useRealtime = false;
  public Action onFinished = null;

  [Tooltip("What the faded in alpha value should be")]
  [Range(0f, 1f)]
  public float fadeToAlpha = 1f;

  public int runEveryXFrames = 0;
  public int runEveryXFramesOffset = 0;

  SpriteRenderer xRenderer;
  LineRenderer lineRenderer;
  ShapeRenderer shapeRenderer;
  TMP_Text textRenderer;
  FilledBar filledBar;
  Unity.VectorGraphics.SVGImage svgImage;

  SpriteRenderer[] xRendererArray;
  LineRenderer[] lineRendererArray;
  ShapeRenderer[] shapeRendererArray;
  TMP_Text[] textRendererArray;
  FilledBar[] filledBarArray;
  Unity.VectorGraphics.SVGImage[] svgImageArray;

  float timer = 0f;

  public bool finished {
    get{ return !started; }
  }

  void Awake () {
    UpdateComponents();
  }

  public void UpdateComponents(){
    if(xRendererArray != null){ Array.Clear(xRendererArray, 0, xRendererArray.Length); }
    if(lineRendererArray != null){ Array.Clear(lineRendererArray, 0, lineRendererArray.Length); }
    if(shapeRendererArray != null){ Array.Clear(shapeRendererArray, 0, shapeRendererArray.Length); }
    if(textRendererArray != null){ Array.Clear(textRendererArray, 0, textRendererArray.Length); }
    if(filledBarArray != null){ Array.Clear(filledBarArray, 0, filledBarArray.Length); }
    if(svgImageArray != null){ Array.Clear(svgImageArray, 0, svgImageArray.Length); }

    if((componentsToFade != null && componentsToFade.Length > 0) ||
       (componentsWithChildrenToFade != null && componentsWithChildrenToFade.Length > 0)
    ){
      if(componentsToFade != null && componentsToFade.Length > 0){
        foreach(var component in componentsToFade){
          AddToArray(ref xRendererArray, component.GetComponent<SpriteRenderer>());
          AddToArray(ref lineRendererArray, component.GetComponent<LineRenderer>());
          AddToArray(ref shapeRendererArray, component.GetComponent<ShapeRenderer>());
          AddToArray(ref textRendererArray, component.GetComponent<TMP_Text>());
          AddToArray(ref filledBarArray, component.GetComponent<FilledBar>());
          AddToArray(ref svgImageArray, component.GetComponent<Unity.VectorGraphics.SVGImage>());
        }
      }

      if(componentsWithChildrenToFade != null && componentsWithChildrenToFade.Length > 0){
        foreach(var component in componentsWithChildrenToFade){
          AddArrayToArray(ref xRendererArray, component.GetComponentsInChildren<SpriteRenderer>());
          AddArrayToArray(ref lineRendererArray, component.GetComponentsInChildren<LineRenderer>());
          AddArrayToArray(ref shapeRendererArray, component.GetComponentsInChildren<ShapeRenderer>());
          AddArrayToArray(ref textRendererArray, component.GetComponentsInChildren<TMP_Text>());
          AddArrayToArray(ref filledBarArray, component.GetComponentsInChildren<FilledBar>());
          AddArrayToArray(ref svgImageArray, component.GetComponentsInChildren<Unity.VectorGraphics.SVGImage>());
        }
      }
    }
    else if(useChildComponents){
      xRendererArray = GetComponentsInChildren<SpriteRenderer>(true);
      lineRendererArray = GetComponentsInChildren<LineRenderer>(true);
      shapeRendererArray = GetComponentsInChildren<ShapeRenderer>(true);
      textRendererArray = GetComponentsInChildren<TMP_Text>(true);
      filledBarArray = GetComponentsInChildren<FilledBar>(true);
      svgImageArray = GetComponentsInChildren<Unity.VectorGraphics.SVGImage>(true);
    }else{
      xRenderer = GetComponent<SpriteRenderer>();
      lineRenderer = GetComponent<LineRenderer>();
      shapeRenderer = GetComponent<ShapeRenderer>();
      textRenderer = GetComponent<TMP_Text>();
      filledBar = GetComponent<FilledBar>();
      svgImage = GetComponent<Unity.VectorGraphics.SVGImage>();
    }
  }

  void AddArrayToArray<T>(ref T[] arr, T[] items){
    foreach(var item in items){
      AddToArray(ref arr, item);
    }
  }

  void AddToArray<T>(ref T[] arr, T item){
    if(item != null){
      if(arr == null){
        arr = new T[componentsToFade.Length];
      }
      if(arr[arr.Length - 1] != null){
        Array.Resize<T>(ref arr, arr.Length * 2);
      }

      var freeIdx = 0;
      for(var i = 0; i < arr.Length; i++){
        if(arr[i] == null){
          break;
        }
        freeIdx++;
      }

      arr[freeIdx] = item;
    }
  }

  void Update () {
    if(!started){ return; }

    timer += useRealtime ? Time.unscaledDeltaTime : Time.deltaTime;
    if(timer < delay){
      return;
    }

    var pctFaded = Mathf.Clamp01((timer - delay) / timeToLive);
    if(fadingOut){
      pctFaded = 1 - pctFaded;
    }

    if(runEveryXFrames == 0 || (Time.frameCount + runEveryXFramesOffset) % runEveryXFrames == 0){
      DoFade(pctFaded);
    }

    if(timer > timeToLive + delay){
      started = false;
      if(toggleGameObjectActive && fadingOut){
        this.gameObject.SetActive(false);
      }
      if(onFinished != null){
        onFinished();
      }
      return;
    }
  }

  //pctFaded is how much faded in we are, 1 for fully in, 0 for fully out
  void DoFade(float pctFaded){
    UnityEngine.Profiling.Profiler.BeginSample("Do Fade");
    var alpha = pctFaded * fadeToAlpha;

    if(useChildComponents || (componentsToFade != null && componentsToFade.Length > 0)){
      UnityEngine.Profiling.Profiler.BeginSample("xRendererArray");
      if(xRendererArray != null && xRendererArray.Length > 0){
        foreach(var xRend in xRendererArray){
          if(xRend == null){ continue; }
          xRend.color = xRend.color.SetA(alpha);
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("svgImgArray");
      if(svgImageArray != null && svgImageArray.Length > 0){
        foreach(var svgImg in svgImageArray){
          if(svgImg == null){ continue; }
          svgImg.color = svgImg.color.SetA(alpha);
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("lineRendererArray");
      if(lineRendererArray != null && lineRendererArray.Length > 0){
        foreach(var lineRend in lineRendererArray){
          if(lineRend == null){ continue; }
          lineRend.startColor = lineRend.startColor.SetA(alpha);
          lineRend.endColor = lineRend.endColor.SetA(alpha);
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("shapeRendererArray");
      if(shapeRendererArray != null && shapeRendererArray.Length > 0){
        foreach(var rend in shapeRendererArray){
          if(rend == null){ continue; }
          rend.Color = rend.Color.SetA(alpha);
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("textRendererArray");
      if(textRendererArray != null && textRendererArray.Length > 0){
        foreach(var textRend in textRendererArray){
          if(textRend == null){ continue; }
          textRend.color = textRend.color.SetA(alpha);
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("filledBarArray");
      if(filledBarArray != null && filledBarArray.Length > 0){
        foreach(var filledBar in filledBarArray){
          if(filledBar == null){ continue; }
          filledBar.color = filledBar.color.SetA(alpha);
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();

    }else{
      UnityEngine.Profiling.Profiler.BeginSample("xRenderer");
      if(xRenderer != null){
        xRenderer.color = xRenderer.color.SetA(alpha);
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("svgImage");
      if(svgImage != null){
        svgImage.color = svgImage.color.SetA(alpha);
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("lineRenderer");
      if(lineRenderer != null){
        lineRenderer.startColor = lineRenderer.startColor.SetA(alpha);
        lineRenderer.endColor = lineRenderer.endColor.SetA(alpha);
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("shapeRenderer");
      if(shapeRenderer != null){
        shapeRenderer.Color = shapeRenderer.Color.SetA(alpha);
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("textRenderer");
      if(textRenderer != null){
        textRenderer.color = textRenderer.color.SetA(alpha);
      }
      UnityEngine.Profiling.Profiler.EndSample();

      UnityEngine.Profiling.Profiler.BeginSample("filledBar");
      if(filledBar != null){
        filledBar.color = textRenderer.color.SetA(alpha);
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    UnityEngine.Profiling.Profiler.EndSample();
  }

  public void FadeIn(float? ttl = null, bool immediate = false, bool updateComponents = false){
    if(updateComponents){ UpdateComponents(); }
    if(ttl.HasValue){ timeToLive = ttl.Value; }
    fadingOut = false;
    timer = 0f;
    started = true;
    if(toggleGameObjectActive){
      this.gameObject.SetActive(true);
    }
    if(immediate){
      DoFade(1f);
      started = false;
    }
  }

  public void FadeOut(float? ttl = null, bool immediate = false, bool updateComponents = false){
    if(updateComponents){ UpdateComponents(); }
    if(ttl.HasValue){ timeToLive = ttl.Value; }
    fadingOut = true;
    timer = 0f;
    started = true;
    if(immediate){
      DoFade(0f);
      started = false;
      if(toggleGameObjectActive){
        this.gameObject.SetActive(false);
      }
    }
  }

}
