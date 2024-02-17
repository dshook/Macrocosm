using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(GridCreator))]
public class ColliderCreatorEditor : Editor {

    override public void  OnInspectorGUI () {
        GridCreator colliderCreator = (GridCreator)target;
        if(GUILayout.Button("Create")) {
          colliderCreator.Create();
        }
        if(GUILayout.Button("Clear")) {
          colliderCreator.Clear();
        }
        DrawDefaultInspector();
    }
}