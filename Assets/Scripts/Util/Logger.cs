using System;
using System.Diagnostics;
using UnityEngine;

public static class Logger{

  [Conditional("UNITY_EDITOR")]
  public static void Log(object message, UnityEngine.Object context){
    UnityEngine.Debug.Log(message, context);
  }

  [Conditional("UNITY_EDITOR")]
  public static void Log(object message){
    UnityEngine.Debug.Log(message);
  }

  [Conditional("UNITY_EDITOR")]
  public static void LogWithFrame(object message, UnityEngine.Object context){
    UnityEngine.Debug.Log(string.Format("{0} {1}", Time.frameCount, message), context);
  }

  [Conditional("UNITY_EDITOR")]
  public static void LogWithFrame(object message){
    UnityEngine.Debug.Log(string.Format("{0} {1}", Time.frameCount, message));
  }

  [Conditional("UNITY_EDITOR")]
  public static void LogWarning(object message, UnityEngine.Object context){
    UnityEngine.Debug.LogWarning(message, context);
  }

  [Conditional("UNITY_EDITOR")]
  public static void LogWarning(object message){
    UnityEngine.Debug.LogWarning(message);
  }
}