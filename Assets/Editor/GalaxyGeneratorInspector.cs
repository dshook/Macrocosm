using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(GalaxyGenerator))]
public class GalaxyGeneratorEditor : Editor {

  override public void  OnInspectorGUI () {
    GalaxyGenerator creator = (GalaxyGenerator)target;
    DrawDefaultInspector();

    if(GUILayout.Button("Generate")) {
      //TODO: Broken till I set up the spawner to work with the inspector
      creator.GenerateMap();
      EditorUtility.SetDirty(target);
    }
    if(GUILayout.Button("Clear")) {
      EditorUtility.SetDirty(target);
    }
  }
}