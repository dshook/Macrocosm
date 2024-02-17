using UnityEngine;
using UnityEditor;
using System.Collections;
using PygmyMonkey.ColorPalette;

[CustomEditor(typeof(ShinyButton))]
public class ShinyButtonEditor : Editor {

  override public void  OnInspectorGUI () {
    ShinyButton creator = (ShinyButton)target;
    DrawDefaultInspector();

    EditorGUI.BeginChangeCheck();

    //Make sure the setters are being set to update in editor
    creator.isSelected = creator.isSelected;
    creator.color = creator.color;

    if (EditorGUI.EndChangeCheck())
    {
      Undo.RecordObject(target, "ShinyButton");
    }

  }
}