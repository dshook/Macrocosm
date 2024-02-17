using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetRingGenerator))]
public class PlanetRingGeneratorEditor : Editor {

  override public void  OnInspectorGUI () {
    PlanetRingGenerator creator = (PlanetRingGenerator)target;
    DrawDefaultInspector();

    if(GUILayout.Button("Update")) {
      creator.UpdateDisplay();
    }
  }
}