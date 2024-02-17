using System.Collections.Generic;
using UnityEngine;

public class SongTemplate : MonoBehaviour
{
  public AudioClip musicClip;

  public int bpm;

  public BeatTemplate[] templates;

  //Stuff Only for tracking in game
  [System.NonSerialized]
  public int templateIndex = 0;

  public float length{
    get{
      if(musicClip != null){
        return musicClip.length;
      }
      return 0;
    }
  }

  public HashSet<BeatType> beatTypesUsed {
    get{
      if(templates == null){
        return null;
      }

      HashSet<BeatType> typesUsed = new HashSet<BeatType>();
      foreach(var t in templates){
        if(t != null){
          foreach(var item in t.items){
            typesUsed.Add(item.type);
          }
        }
      }
      return typesUsed;
    }
  }
}