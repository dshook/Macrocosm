using System;

[System.Serializable]
[Singleton]
public class SettingsDataModel
{
  public bool tutorialsDisabled {get; set;}
  public bool hapticsDisabled {get; set;}
  public bool lowQuality {get; set;}

  public float musicVolume = 0.5f;
  public float sfxVolume = 0.5f;
}