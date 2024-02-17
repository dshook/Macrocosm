using UnityEngine;

[ExecuteInEditMode]
public class StageOneBigText : MonoBehaviour
{
  public string lineOne;
  public string lineTwo;

  public MultiText lineOneMulti;
  public MultiText lineTwoMulti;

  public ColorPulser warningPulse;

  public ParticleSystem particles;
  ParticleSystem.EmissionModule particleEmission;

  bool showing = false;
  float showTime = 3f;
  float showingTime = 0f;

  Vector3 originalScale;

  void Awake(){
    originalScale = transform.localScale;
  }

  void Update()
  {
    if(!showing){
      return;
    }

    showingTime += Time.unscaledDeltaTime;

    if(particles != null && showingTime > (showTime * 0.75f) && particleEmission.enabled){
      particleEmission.enabled = false;
    }

    if(showingTime > showTime){
      Hide();

      if(particles != null){
        particles.Stop();
      }
    }
  }

  public void Show(string lineOne, string lineTwo, float showTime = 3f, bool sparkles = false){
    showing = true;
    showingTime = 0f;
    this.showTime = showTime;

    this.lineOne = lineOne;
    this.lineTwo = lineTwo;

    lineOneMulti.text = lineOne;
    lineTwoMulti.text = lineTwo;

    lineOneMulti.gameObject.SetActive(true);
    lineTwoMulti.gameObject.SetActive(true);

    transform.localScale = Vector3.zero;
    LeanTween.scale(this.gameObject, originalScale, showTime / 4)
      .setEase(LeanTweenType.easeOutBack)
      .setIgnoreTimeScale(true);

    if(warningPulse != null){
      warningPulse.pulseDuration = showTime;
      warningPulse.Play();
    }

    if(sparkles && particles != null){
      particleEmission = particles.emission;
      particles.Clear();
      particles.Play();
      particleEmission.enabled = true;
    }
  }

  public void Hide(){
    lineOneMulti.gameObject.SetActive(false);
    lineTwoMulti.gameObject.SetActive(false);

    if(warningPulse != null){
      warningPulse.Pause();
    }
    if(particles != null){
      particles.Stop();
    }

    showing = false;
  }

  public bool visible {
    get{ return lineOneMulti.gameObject.activeInHierarchy; }
  }
}