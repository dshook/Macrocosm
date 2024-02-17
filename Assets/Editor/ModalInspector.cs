using UnityEngine;
using UnityEditor;
using System.Collections;
using PygmyMonkey.ColorPalette;

[CustomEditor(typeof(Modal))]
public class ModalEditor : Editor {

  override public void  OnInspectorGUI () {
    Modal creator = (Modal)target;
    DrawDefaultInspector();

    EditorGUI.BeginChangeCheck();

    creator.color = creator.color;

    if (EditorGUI.EndChangeCheck())
    {
      Undo.RecordObject(target, "Modal");
    }

  }
}