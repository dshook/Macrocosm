using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(HexGrid))]
public class HexGridEditor : Editor {

  override public void  OnInspectorGUI () {
    HexGrid creator = (HexGrid)target;
    DrawDefaultInspector();

    if(GUILayout.Button("Create")) {
      creator.ClearGrid();
      creator.CreateGrid();
    }
    if(GUILayout.Button("Clear")) {
      creator.ClearGrid();
    }
  }
}