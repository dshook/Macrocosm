using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StringChanger {

  Dictionary<string, int> prevIntValues = new Dictionary<string, int>();
  Dictionary<string, float> prevFloatValues = new Dictionary<string, float>();

  const int desyncCheckFrames = 40;

  public void UpdateString(TMP_Text tmp, string name, int value, string stringFormat = null, string valueFormat = null){
    if(!prevIntValues.ContainsKey(name) || prevIntValues[name] != value){
      prevIntValues[name] = value;
      if(!string.IsNullOrEmpty(stringFormat)){
        tmp.text = string.Format(stringFormat, value);
      }else{
        tmp.text = value.ToString(valueFormat);
      }
    }
    #if UNITY_EDITOR
    if(Time.frameCount % desyncCheckFrames == 0){
      UnityEngine.Profiling.Profiler.BeginSample("Editor check, don't worry");
      //Check for cache desync
      if(!string.IsNullOrEmpty(stringFormat)){
        if(tmp.text != string.Format(stringFormat, value)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {string.Format(stringFormat, value)}");
        }
      }else{
        if(tmp.text != value.ToString(valueFormat)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {value.ToString()}");
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    #endif
  }

  public void UpdateString(TMP_Text tmp, string name, int value, string stringFormat, int obj1){
    if(!prevIntValues.ContainsKey(name) || prevIntValues[name] != value){
      prevIntValues[name] = value;
      if(!string.IsNullOrEmpty(stringFormat)){
        tmp.text = string.Format(stringFormat, obj1);
      }
    }
    #if UNITY_EDITOR
    if(Time.frameCount % desyncCheckFrames == 0){
      UnityEngine.Profiling.Profiler.BeginSample("Editor check, don't worry");
      //Check for cache desync
      if(!string.IsNullOrEmpty(stringFormat)){
        if(tmp.text != string.Format(stringFormat, obj1)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {string.Format(stringFormat, obj1)}");
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    #endif
  }

  public void UpdateString(TMP_Text tmp, string name, int value, string stringFormat, int obj1, int obj2){
    if(!prevIntValues.ContainsKey(name) || prevIntValues[name] != value){
      prevIntValues[name] = value;
      if(!string.IsNullOrEmpty(stringFormat)){
        tmp.text = string.Format(stringFormat, obj1, obj2);
      }
    }
    #if UNITY_EDITOR
    if(Time.frameCount % desyncCheckFrames == 0){
      UnityEngine.Profiling.Profiler.BeginSample("Editor check, don't worry");
      //Check for cache desync
      if(!string.IsNullOrEmpty(stringFormat)){
        if(tmp.text != string.Format(stringFormat, obj1, obj2)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {string.Format(stringFormat, obj1, obj2)}");
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    #endif
  }

  public void UpdateStringShortFormat(TMP_Text tmp, string name, int value, string prefix = null){
    if(!prevIntValues.ContainsKey(name) || prevIntValues[name] != value){
      prevIntValues[name] = value;
      if(string.IsNullOrEmpty(prefix)){
        tmp.text = value.ToShortFormat();
      }else{
        tmp.text = prefix + value.ToShortFormat();
      }
    }
    #if UNITY_EDITOR
    if(Time.frameCount % desyncCheckFrames == 0){
      UnityEngine.Profiling.Profiler.BeginSample("Editor check, don't worry");
      //Check for cache desync
      if(string.IsNullOrEmpty(prefix)){
        if(tmp.text != value.ToShortFormat()){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {value.ToShortFormat()}");
        }
      }else{
        if(tmp.text != prefix + value.ToShortFormat()){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {prefix + value.ToShortFormat()}");
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    #endif
  }

  public void UpdateString(TMP_Text tmp, string name, float value, string stringFormat = null, string valueFormat = null){
    if(FloatNeedsChanging(name, value)){
      prevFloatValues[name] = value;
      if(!string.IsNullOrEmpty(stringFormat)){
        tmp.text = string.Format(stringFormat, value);
      }else{
        tmp.text = value.ToString(valueFormat);
      }
    }
    #if UNITY_EDITOR
    if(Time.frameCount % desyncCheckFrames == 0){
      UnityEngine.Profiling.Profiler.BeginSample("Editor check, don't worry");
      if(!string.IsNullOrEmpty(stringFormat)){
        if(tmp.text != string.Format(stringFormat, value)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {string.Format(stringFormat, value)}");
        }
      }else{
        if(tmp.text != value.ToString(valueFormat)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {value.ToString(valueFormat)}");
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    #endif
  }

  public void UpdateString(TMP_Text tmp, string name, float value, string stringFormat, float obj1, float obj2){
    if(FloatNeedsChanging(name, value)){
      prevFloatValues[name] = value;
      if(!string.IsNullOrEmpty(stringFormat)){
        tmp.text = string.Format(stringFormat, obj1, obj2);
      }
    }
    #if UNITY_EDITOR
    if(Time.frameCount % desyncCheckFrames == 0){
      UnityEngine.Profiling.Profiler.BeginSample("Editor check, don't worry");
      if(!string.IsNullOrEmpty(stringFormat)){
        if(tmp.text != string.Format(stringFormat, obj1, obj2)){
          Debug.LogWarning($"String changer desync. Name {name} Text {tmp.text} value {string.Format(stringFormat, obj1, obj2)}");
        }
      }
      UnityEngine.Profiling.Profiler.EndSample();
    }
    #endif
  }

  bool FloatNeedsChanging(string name, float value){
    return !prevFloatValues.ContainsKey(name) ||
      (prevFloatValues[name] != value && !Mathf.Approximately(prevFloatValues[name], value));
  }

  public void ClearValue(string name){
    if(name != null){
      prevIntValues.Remove(name);
      prevFloatValues.Remove(name);
    }
  }

  public void ClearAll(){
    prevIntValues.Clear();
    prevFloatValues.Clear();
  }
}
