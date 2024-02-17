using UnityEngine;
using strange.extensions.mediation.impl;


public class AudioService: View
{
  public AudioSource musicSource;

  public AudioSource[] sfxSources;

  [Inject] SettingsDataModel settings { get; set; }
  [Inject] ResourceLoaderService loader { get; set; }

  int currentSfxIndex = 0;
  int reservedSfxIndex = 0;

  protected override void Awake(){
    base.Awake();
  }

  void Update(){
    musicSource.volume = settings.musicVolume;

    //Only set the volume of the unreserved sources
    for(var i = reservedSfxIndex; i < sfxSources.Length; i++){
      sfxSources[i].volume = settings.sfxVolume;
    }
  }

  AudioSource AvailableSfxSource {
    get{
      if(sfxSources == null || sfxSources.Length == 0){
        Debug.LogWarning("No available sfx sources");
        return null;
      }

      //make sure not to loop back over a reserved source
      currentSfxIndex = currentSfxIndex + 1;
      if(currentSfxIndex >= sfxSources.Length){
        currentSfxIndex = reservedSfxIndex;
      }

      if(currentSfxIndex >= sfxSources.Length){
        Debug.LogWarning("Too many reserved sfx sources");
        return null;
      }

      var source = sfxSources[currentSfxIndex];

      //reset some props to default
      source.loop = false;
      source.volume = settings.sfxVolume;

      return source;
    }
  }

  //Expose a way for other scripts to get an sfx source they can hold onto and use exclusively
  //The source given out could still be playing other sfx I think but should be fine
  //This effectively reserves the first reservedSfxIndex number of sfx sources in the array
  public AudioSource GetReservedSfxSource(){
    //don't allow reserving all the instances
    if(reservedSfxIndex > sfxSources.Length - 2){
      return null;
    }

    var source = sfxSources[reservedSfxIndex];
    reservedSfxIndex++;
    return source;
  }

  public void ReleaseReservedSfxSource(){
    if(reservedSfxIndex <= 0){
      Debug.LogWarning("No reserved SFX source to release");
      return;
    }
    reservedSfxIndex--;
  }

  public void PlayMusic(AudioClip clip, bool loop = true, bool restart = false){
    if(!restart && musicSource.clip == clip && musicSource.isPlaying){
      return;
    }
    musicSource.clip = clip;
    musicSource.loop = loop;
    if(musicSource.clip != null){
      musicSource.Play();
    }
  }

  public void PlayMusicScheduled(AudioClip clip, double timeFromNow, bool loop = false){
    musicSource.clip = clip;
    musicSource.loop = loop;
    if(musicSource.clip != null){
      musicSource.PlayScheduled(timeFromNow);
    }
  }

  public void StopMusic(){
    if(musicSource.clip != null){
      musicSource.Stop();
    }
  }

  public void SetMusicTime(int timeSamples){
    if(musicSource.clip != null){
      musicSource.timeSamples = timeSamples;
    }
  }

  public void PauseMusic(){
    if(musicSource.clip != null){
      musicSource.Pause();
    }
  }

  public void UnPauseMusic(){
    if(musicSource.clip != null){
      musicSource.UnPause();
    }
  }

  public double MusicTimeElapsed{
    get{
      return (double)musicSource.timeSamples / musicSource.clip.frequency;
    }
  }

  public int MusicTimeSamples{
    get{
      return musicSource.timeSamples;
    }
  }

  public void PlaySfx(AudioClip clip, float pitch = 1, AudioSource reservedSource = null){
    var currentSfx = reservedSource;
    if(currentSfx == null){
      currentSfx = AvailableSfxSource;
    }
    if(clip != null && currentSfx != null){
      currentSfx.pitch = pitch;
      currentSfx.PlayOneShot(clip);
    }
  }

  public void PlaySfxScheduled(AudioClip clip, double timeFromNow){
    var currentSfx = AvailableSfxSource;
    if(clip != null && currentSfx != null){
      currentSfx.clip = clip;
      currentSfx.PlayScheduled(timeFromNow);
    }
  }

}

