using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HexCellDisplay))]
public class HexCellDisplayEditor : Editor {

  override public void  OnInspectorGUI () {
    HexCellDisplay creator = (HexCellDisplay)target;
    DrawDefaultInspector();

    if(creator.hexCell != null){
      EditorGUILayout.LabelField("Coords", creator.hexCell.coordinates.ToString());
    }
  }
}