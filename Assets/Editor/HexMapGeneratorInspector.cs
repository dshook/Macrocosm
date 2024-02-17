using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(HexMapGenerator))]
public class HexMapGeneratorEditor : Editor {

  override public void  OnInspectorGUI () {
    HexMapGenerator creator = (HexMapGenerator)target;
    DrawDefaultInspector();

    if(GUILayout.Button("Update Cell Display")) {
      EditorGUI.BeginChangeCheck();

      creator.UpdateCellDisplay();

      if (EditorGUI.EndChangeCheck())
      {
        Undo.RecordObject(target, "Changed Map");
      }
      UnityEditor.SceneView.RepaintAll();
    }

    if(GUILayout.Button("Generate")) {

      EditorGUI.BeginChangeCheck();

      creator.grid.ClearGrid();
      creator.grid.CreateGrid();
      creator.GenerateMap();
      creator.grid.StartCreatingCells();

      if (EditorGUI.EndChangeCheck())
      {
        Undo.RecordObject(target, "Changed Map");
      }
      UnityEditor.SceneView.RepaintAll();
    }
  }
}