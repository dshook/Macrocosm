using UnityEngine;

public class TimeService
{
  float lastTs = 1f;

  public static float normalTimeScale = 1f;
  public static float fastTimeScale = 2f;
  public static float extraFastTimeScale = 3f;

  public bool Paused {
    get{ return Time.timeScale == 0; }
  }

  int pauseCount = 0;

  public void Pause()
  {
    if(Time.timeScale > 0){
      lastTs = Time.timeScale;
    }
    Time.timeScale = 0;
    pauseCount++;
  }

  public void Resume(){
    pauseCount--;
    if(pauseCount <= 0){
      Time.timeScale = lastTs;
      pauseCount = 0;
    }
  }

  public void SetTimescale(float newTs){
    Time.timeScale = newTs;
  }

  public void ChangeTimescale(float delta){
    Time.timeScale += delta;
  }
}

