using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

class MarkovTestTool : EditorWindow
{
  [MenuItem("Window/Macrocosm/Markov Test Tool")]
  static void Init()
  {
    var window = GetWindow(typeof(MarkovTestTool));
    window.Show();
  }

  void OnGUI()
  {
    if (GUILayout.Button("Test"))
    {
      Test();
    }
  }

  void Test()
  {
    foreach(var name in StarName.Generate(30)){
        Debug.Log(name);
    }
  }

}