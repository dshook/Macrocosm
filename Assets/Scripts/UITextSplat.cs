using UnityEngine;
using TMPro;
using System;

[RequireComponent(typeof(TMP_Text))]
public class UITextSplat : MonoBehaviour
{
  private TMP_Text damageSplatText;

  public float ttl = 1f;
  public float ttd = 1f; //TIME TO DIE
  public float animateTime = 0.5f;

  public float animateRotationAmount;
  float timeAccum = 0f;

  void Start()
  {
    damageSplatText = GetComponent<TMP_Text>();

    if(damageSplatText != null){
    }

  }

  void Update()
  {
    if(!damageSplatText.enabled){
      return;
    }


    timeAccum += Time.deltaTime;
    if (timeAccum > (animateTime + ttl + ttd))
    {
      damageSplatText.enabled = false;
    }
  }

  public void Show(string text, Color color, bool animate, float ttl){
    timeAccum = 0f;
    this.ttl = ttl;

    LeanTween.cancel(gameObject);
    damageSplatText.color = color;
    damageSplatText.text = text;
    damageSplatText.enabled = true;

    LeanTween.textColor(gameObject, color.SetA(0), ttl + animateTime)
      // .setDelay(animateTime + ttl)
      .setIgnoreTimeScale(true)
      .setEase(LeanTweenType.easeInQuart);

    if (animate)
    {
      transform.localScale = Vector3.zero;

      LeanTween.scale(gameObject, Vector3.one, animateTime).setEase(LeanTweenType.easeOutBack);
      LeanTween.rotate(gameObject, new Vector3(0, 0, 724f), animateTime).setEase(LeanTweenType.easeOutBack);
    }else{
      transform.localScale = Vector3.one;
    }
  }

}