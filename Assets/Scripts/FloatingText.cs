using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections;

public class FloatingText : View {

  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] ObjectPool objectPool {get; set;}

  public GameObject holder;
  public UITextSplat uITextSplat;

  public GameObject textSplatPrefab;

  protected override void Awake () {
    base.Awake();

    objectPool.CreatePool(textSplatPrefab, 0);
  }

  void Update () {
  }

  public void Create(
    RectTransform position,
    Color? color = null,
    string text = null,
    float? fontSize = null,
    float? moveUpPct = null,
    float? punchSize = null,
    float? ttl = null,
    float? delay = null,
    string prefabPath = null
  ){
    MakeIt(
      position: Vector3.zero,
      color,
      parent: null,
      text,
      fontSize,
      moveUpPct,
      punchSize,
      ttl,
      delay,
      prefabPath,
      rectTransform: position
    );
  }

  public void Create(
    Vector3 position,
    Color? color = null,
    Transform parent = null,
    string text = null,
    float? fontSize = null,
    float? moveUpPct = null,
    float? punchSize = null,
    float? ttl = null,
    float? delay = null,
    string prefabPath = null
  ){
    MakeIt(
      position,
      color,
      parent,
      text,
      fontSize,
      moveUpPct,
      punchSize,
      ttl,
      delay,
      prefabPath
    );
  }

  void MakeIt(
    Vector3 position,
    Color? color = null,
    Transform parent = null,
    string text = null,
    float? fontSize = null,
    float? moveUpPct = null,
    float? punchSize = null,
    float? ttl = null,
    float? delay = null,
    string prefabPath = null,
    RectTransform rectTransform = null
  ){

    var prefab = string.IsNullOrEmpty(prefabPath) ? textSplatPrefab : loader.Load<GameObject>(prefabPath);

    var newSplat = objectPool.Spawn(
      prefab,
      position,
      Quaternion.identity
    ) as GameObject;

    if(rectTransform != null){
      newSplat.transform.SetParent(rectTransform.transform);
      newSplat.transform.localPosition = position;
      newSplat.transform.localScale = Vector3.one;
    }else{
      newSplat.transform.SetParent(parent ?? holder.transform, false);
    }

    var textSplat = newSplat.GetComponent<TextSplat>();

    if(textSplat != null){
      textSplat.text = text;
      textSplat.animate = true;
      textSplat.delay = delay;
      textSplat.objectPool = objectPool;
      if(color.HasValue){
        textSplat.color = color.Value;
      }
      if(fontSize.HasValue){
        textSplat.fontSize = fontSize.Value;
      }

      textSplat.moveUpPct = moveUpPct;
      textSplat.punchSize = punchSize;

      if(ttl.HasValue){
        textSplat.ttl = ttl.Value;
      }

      textSplat.Init();
    }
  }

  //For the persistent UI text splat
  public void CreateUI(string text, Color color, bool animate, float ttl = 1.5f){
    uITextSplat.Show(text, color, animate, ttl);
  }
}
